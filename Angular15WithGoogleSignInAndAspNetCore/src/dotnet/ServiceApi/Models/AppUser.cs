namespace ServiceApi.Models;
public record AppUser(string Email, string Name, string Role, byte[] PasswordSalt, byte[] PasswordHash);
