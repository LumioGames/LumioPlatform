using System;
using System.IO;
using Microsoft.Extensions.Options;

namespace Lumio.Platform.App;

public static class Program
{
    public static int Main(string[] args)
    {
        try
        {
            if (args.Length > 0 && string.Equals(args[0], "openapi-export", StringComparison.Ordinal))
            {
                if (args.Length != 2 || string.IsNullOrWhiteSpace(args[1])) return PlatformExitCodes.InvalidArguments;
                OpenApiExport.ExportAsync(args[1]).GetAwaiter().GetResult();
                return PlatformExitCodes.Success;
            }
            if (args.Length > 0) return PlatformExitCodes.InvalidArguments;
            var app = PlatformHost.Build(args);
            app.Run();
            return PlatformExitCodes.Success;
        }
        catch (OptionsValidationException ex)
        {
            Console.Error.WriteLine($"PLATFORM_INIT_FAILED {ex.Message}");
            return PlatformExitCodes.InitializationFailed;
        }
        catch (Exception ex) when (ex is InvalidDataException or IOException or UnauthorizedAccessException or Microsoft.EntityFrameworkCore.DbUpdateException or Npgsql.NpgsqlException)
        {
            Console.Error.WriteLine($"PLATFORM_INIT_FAILED {ex.Message}");
            return PlatformExitCodes.InitializationFailed;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"PLATFORM_FATAL {ex.Message}");
            return PlatformExitCodes.Fatal;
        }
    }
}
