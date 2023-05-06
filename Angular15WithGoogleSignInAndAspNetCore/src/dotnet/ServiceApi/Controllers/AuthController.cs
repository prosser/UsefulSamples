namespace ServiceApi.Controllers;

using Google.Apis.Auth;

using ServiceApi.Models;
using ServiceApi.Services;

using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly AppSettings appSettings;
    private readonly UserService userService;

    public AuthController(
        AppSettings appSettings,
        UserService userService)
    {
        this.appSettings = appSettings;
        this.userService = userService;
    }

    [HttpPost("Login")]
    public async Task<IActionResult> Login([FromBody] Login model, CancellationToken ct)
    {
        if (model.UserName is null)
        {
            return this.Unauthorized(model);
        }

        AppUser? user = await this.userService.GetUserByUserNameAsync(model.UserName, ct);

        if (user is null || !this.CheckPassword(model.Password, user))
        {
            return this.Unauthorized(model);
        }

        JwtResult result = this.CreateJwt(user);
        return this.Ok(result);
    }

    [HttpPost("LoginWithGoogle")]
    public async Task<IActionResult> LoginWithGoogleAsync([FromBody] GoogleAuth auth, CancellationToken ct)
    {
        try
        {
            GoogleJsonWebSignature.ValidationSettings settings = new()
            {
                Audience = new[] { this.appSettings.Authentication.Google.ClientId },
            };

            GoogleJsonWebSignature.Payload payload = await GoogleJsonWebSignature.ValidateAsync(auth.Credentials, settings);

            AppUser? user = await this.userService.GetUserByUserNameAsync(payload.Email, ct);
            if (user is null)
            {
                string randomPassword = Guid.NewGuid().ToString();
                user = new(payload.Email, payload.Name, "User", Array.Empty<byte>(), Array.Empty<byte>());
                await this.userService.RegisterUserAsync(user, ct);
            }

            return this.Ok(this.CreateJwt(user));
        }
        catch (InvalidJwtException)
        {
            return this.Unauthorized();
        }
    }

    public record GoogleAuth(string Credentials);

    private JwtResult CreateJwt(AppUser user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        byte[] key = Encoding.ASCII.GetBytes(this.appSettings.Authentication.Google.ClientSecret);

        SecurityTokenDescriptor tokenDescriptor = new()
        {
            Subject = new ClaimsIdentity(new Claim[] { new(ClaimTypes.Name, user.Name), new(ClaimTypes.Email, user.Email), new(ClaimTypes.Role, user.Role) }),
            Expires = DateTime.UtcNow.AddDays(1),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        SecurityToken token = tokenHandler.CreateToken(tokenDescriptor);
        string encryptedToken = tokenHandler.WriteToken(token);
        JwtResult result = new(encryptedToken, user.Email);
        return result;
    }

    private record JwtResult(string Token, string Username);

    [HttpPost("Register")]
    public async Task<IActionResult> RegisterAsync([FromBody] Register model, CancellationToken ct)
    {
        AppUser user = new(model.Email, model.Name, model.Role, Array.Empty<byte>(), Array.Empty<byte>());

        if (model.ConfirmPassword != model.Password)
        {
            return this.BadRequest("Passwords do not match");
        }
        else
        {
            using var hmac = new HMACSHA512();
            user = user with
            {
                PasswordSalt = hmac.Key,
                PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(model.Password)),
            };
        }

        await this.userService.RegisterUserAsync(user, ct);
        return this.Ok(user);
    }

    private bool CheckPassword(string password, AppUser user)
    {
        using HMACSHA512 hmac = new(user.PasswordSalt);
        byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
        return hash.SequenceEqual(user.PasswordHash);
    }
}
