using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Lumio.Platform.App;

public static class OpenApiExport
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

    public static async Task ExportAsync(string path, CancellationToken cancellationToken = default)
    {
        var app = PlatformHost.Build([], new PlatformOptions { ListenUrl = "http://127.0.0.1:0" }, requireDatabase: false);
        await app.StartAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var addresses = app.Services.GetRequiredService<Microsoft.AspNetCore.Hosting.Server.IServer>()
                .Features.Get<Microsoft.AspNetCore.Hosting.Server.Features.IServerAddressesFeature>()?.Addresses;
            var address = addresses?.SingleOrDefault() ?? throw new InvalidOperationException("server address unavailable");
            using var client = new HttpClient { BaseAddress = new Uri(address) };
            using var response = await client.GetAsync("/openapi/v1.json", cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var document = JsonNode.Parse(json)?.AsObject() ?? throw new InvalidDataException("OpenAPI document was not an object");
            document.Remove("servers");
            var formatted = JsonSerializer.Serialize(document, SerializerOptions).Replace("\r\n", "\n", StringComparison.Ordinal) + "\n";
            var root = FindRepositoryRoot();
            var outputPath = Path.IsPathRooted(path) ? path : Path.Combine(root, path);
            var directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            await File.WriteAllTextAsync(outputPath, formatted, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            await app.StopAsync(cancellationToken).ConfigureAwait(false);
            await app.DisposeAsync().ConfigureAwait(false);
        }
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Directory.Build.props"))) return directory.FullName;
            directory = directory.Parent;
        }

        return Directory.GetCurrentDirectory();
    }
}
