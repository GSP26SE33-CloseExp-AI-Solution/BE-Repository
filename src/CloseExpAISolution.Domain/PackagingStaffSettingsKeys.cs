namespace CloseExpAISolution.Domain;

public static class PackagingStaffSettingsKeys
{
    public const string DocumentKey = "PACKAGING_STAFF_SETTINGS";

    public const string LegacyStaffSupermarketPrefix = "PACKAGING_STAFF_SUPERMARKET:";

    public const string LegacyUnassignedActorUserId = "PACKAGING_UNASSIGNED_ACTOR_USER_ID";

    public static bool IsReservedSystemConfigKey(string configKey)
    {
        if (string.IsNullOrWhiteSpace(configKey))
            return false;

        var k = configKey.Trim();
        return k == DocumentKey
            || k == LegacyUnassignedActorUserId
            || k.StartsWith(LegacyStaffSupermarketPrefix, StringComparison.Ordinal);
    }
}
