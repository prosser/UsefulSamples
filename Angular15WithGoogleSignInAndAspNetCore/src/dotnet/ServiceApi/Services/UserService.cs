namespace ServiceApi.Services;

using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

using ServiceApi.Models;

using System.Text.Json;

public class UserService : IHostedService
{
    private readonly BlobServiceClient blobServiceClient;
    private readonly BlobContainerClient containerClient;
    private readonly JsonSerializerOptions serializerOptions;

    public UserService(
        AppSettings appSettings,
        JsonSerializerOptions serializerOptions)
    {
        this.blobServiceClient = new(appSettings.Storage);
        this.containerClient = this.blobServiceClient.GetBlobContainerClient("users");
        this.serializerOptions = serializerOptions;
    }

    public async Task<AppUser?> GetUserByUserNameAsync(string userName, CancellationToken ct = default)
    {
        BlobClient client = this.containerClient.GetBlobClient(userName + ".json");
        try
        {
            using MemoryStream stream = new();
            Response response = await client.DownloadToAsync(stream, ct);
            if (response.IsError)
            {
                throw new KeyNotFoundException();
            }

            stream.Position = 0;
            return await JsonSerializer.DeserializeAsync<AppUser>(stream, serializerOptions, ct);
        }
        catch (RequestFailedException ex)
        {
            if (ex.ErrorCode == "BlobNotFound")
            {
                return null;
            }

            throw;
        }
    }

    public async Task RegisterUserAsync(AppUser user, CancellationToken ct = default)
    {
        BlobClient client = this.containerClient.GetBlobClient(user.Email + ".json");
        if (await client.ExistsAsync(ct))
        {
            throw new InvalidOperationException("User already registered");
        }

        using MemoryStream stream = new();
        await JsonSerializer.SerializeAsync(stream, user, this.serializerOptions, ct);
        stream.Position = 0;
        _ = await client.UploadAsync(stream, ct);
    }

    public async Task StartAsync(CancellationToken ct)
    {
        _ = await this.containerClient.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: ct);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
