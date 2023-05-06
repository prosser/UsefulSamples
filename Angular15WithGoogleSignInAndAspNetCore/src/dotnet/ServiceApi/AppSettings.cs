namespace ServiceApi;

public class AppSettings
{
    public AuthSettings Authentication { get; set; } = new();
    public string Storage { get; set; } = "<storage account connection string>";
}

public class AuthSettings
{
    public GoogleAuthSettings Google { get; set; } = new();
}

public class GoogleAuthSettings
{
    public string ClientId { get; set; } = "<google_client_id>";
    public string ClientSecret { get; set; } = "<google_client_secret>";
}