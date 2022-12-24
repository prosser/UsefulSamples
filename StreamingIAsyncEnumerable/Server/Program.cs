using Rosser.Contracts;

using System.Text.Json;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
JsonSerializerOptionsFactory jsof = new();

builder.Services.AddControllers();
builder.Services.AddSingleton(jsof);
builder.Services.AddSingleton(jsof.Default);
builder.Services.ConfigureHttpJsonOptions(options =>
{
    // apply the default serializer options to ASP.NET Core request and response formatting.
    jsof.Apply(options.SerializerOptions);
});

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
