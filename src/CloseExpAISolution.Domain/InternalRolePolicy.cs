using CloseExpAISolution.Domain.Enums;

namespace CloseExpAISolution.Domain;

public static class InternalRolePolicy
{
    public static bool IsAllowedForAdminInternalRegistration(int roleId)
    {
        return roleId is
            (int)RoleUser.PackagingStaff or
            (int)RoleUser.MarketingStaff or
            (int)RoleUser.SupermarketStaff or
            (int)RoleUser.DeliveryStaff;
    }
}
