using CloseExpAISolution.Domain.Enums;

namespace CloseExpAISolution.Application.Auth;

public static class InternalStaffRegistrationHelper
{
    public const string FallbackDefaultPassword = "CloseExp@.123";

    public static string ResolveTemporaryPassword(string? requestPassword, string? configuredPassword)
    {
        if (!string.IsNullOrWhiteSpace(requestPassword))
            return requestPassword.Trim();

        if (!string.IsNullOrWhiteSpace(configuredPassword))
            return configuredPassword.Trim();

        return FallbackDefaultPassword;
    }

    public static string GetRoleLabel(int roleId)
    {
        if (!Enum.IsDefined(typeof(RoleUser), roleId))
            return $"Role-{roleId}";

        return ((RoleUser)roleId).ToString();
    }
}
