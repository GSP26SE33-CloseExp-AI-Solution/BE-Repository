namespace CloseExpAISolution.API.Extensions;

public static class StartupValidationExtensions
{
    public static void ValidateCriticalConfiguration(this WebApplicationBuilder builder)
    {
        var config = builder.Configuration;

        var jwtKey = config["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(jwtKey))
            throw new InvalidOperationException("Missing required configuration: Jwt:Key");

        var defaultConnection = config.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(defaultConnection))
            throw new InvalidOperationException("Missing required connection string: DefaultConnection");

        var loggerFactory = LoggerFactory.Create(lb => lb.AddConsole());
        var logger = loggerFactory.CreateLogger("StartupValidation");
        if (string.IsNullOrWhiteSpace(config["R2Storage:BucketName"]))
        {
            logger.LogWarning(
                "Optional configuration R2Storage:BucketName is missing. OCR/image-upload endpoints may return 503.");
        }
    }
}

