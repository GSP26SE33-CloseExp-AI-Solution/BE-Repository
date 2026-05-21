namespace CloseExpAISolution.API.Extensions;

public static class StartupValidationExtensions
{
    public static void ValidateCriticalConfiguration(this WebApplicationBuilder builder)
    {
        var config = builder.Configuration;
        var env = builder.Environment;

        var jwtKey = config["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(jwtKey))
            throw new InvalidOperationException("Missing required configuration: Jwt:Key");

        var defaultConnection = config.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(defaultConnection))
            throw new InvalidOperationException("Missing required connection string: DefaultConnection");

        if (env.IsProduction())
        {
            ValidateProductionValue("Jwt:Key", jwtKey);
            ValidateProductionValue("ConnectionStrings:DefaultConnection", defaultConnection);
            ValidateProductionValue("GoogleAuth:ClientId", config["GoogleAuth:ClientId"]);
            ValidateProductionValue("GoogleAuth:ClientSecret", config["GoogleAuth:ClientSecret"]);
            ValidateProductionValue("Mapbox:AccessToken", config["Mapbox:AccessToken"]);
            ValidateProductionValue("EmailSettings:SmtpUsername", config["EmailSettings:SmtpUsername"]);
            ValidateProductionValue("EmailSettings:SmtpPassword", config["EmailSettings:SmtpPassword"]);
            ValidateProductionValue("R2Storage:AccessKeyId", config["R2Storage:AccessKeyId"]);
            ValidateProductionValue("R2Storage:SecretAccessKey", config["R2Storage:SecretAccessKey"]);
        }

        var loggerFactory = LoggerFactory.Create(lb => lb.AddConsole());
        var logger = loggerFactory.CreateLogger("StartupValidation");
        if (string.IsNullOrWhiteSpace(config["R2Storage:BucketName"]))
        {
            logger.LogWarning(
                "Optional configuration R2Storage:BucketName is missing. OCR/image-upload endpoints may return 503.");
        }
    }

    private static void ValidateProductionValue(string key, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"Missing required production configuration: {key}");

        var normalized = value.Trim().ToLowerInvariant();
        if (normalized.Contains("your-") || normalized.Contains("change-me") || normalized.Contains("example.com"))
            throw new InvalidOperationException(
                $"Invalid production configuration for {key}. Placeholder value detected.");
    }
}

