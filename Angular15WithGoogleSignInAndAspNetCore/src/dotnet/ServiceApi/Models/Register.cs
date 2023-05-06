namespace ServiceApi.Models;

public record Register(string Email, string Name, string Role, string Password, string ConfirmPassword);
