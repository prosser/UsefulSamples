namespace ServiceApi;

using Azure.Identity;

using ServiceApi.Services;

using System.Text.Json;

public class Program
{
    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        JsonSerializerOptions jsonSerializerOptions = JsonSerializerOptionsFactory.CreateSerializerOptions();
        var keyVaultEndpoint = new Uri(Environment.GetEnvironmentVariable("VaultUri") ?? "https://your-vault-name.vault.azure.net/");
        _ = builder.Configuration.AddAzureKeyVault(keyVaultEndpoint, new DefaultAzureCredential());

        ConfigurationManager config = builder.Configuration;

        IServiceCollection services = builder.Services;

        _ = services.AddAuthentication();

        _ = services.AddControllers()
            .AddJsonOptions(options => options.JsonSerializerOptions.PopulateOptions(jsonSerializerOptions));

        _ = services.AddCors(options =>
        {
            options.AddDefaultPolicy(builder =>
            {
                _ = builder.WithOrigins("http://localhost:4200", "https://localhost:4201", "https://your-app-name.azurewebsites.net")
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        _ = services.AddSingleton(_ =>
        {
            AppSettings appSettings = new();
            builder.Configuration.Bind(appSettings);
            return appSettings;
        })
            .AddSingleton(jsonSerializerOptions)
            .AddSingleton<UserService>()
            .AddHostedService(p => p.GetRequiredService<UserService>());

        WebApplication app = builder.Build();

        // Configure the HTTP request pipeline.

        app.UseCors();
        _ = app.UseHttpsRedirection()
            .UseAuthentication()
            .UseAuthorization();

        _ = app.MapControllers();

        app.Run();
    }
}