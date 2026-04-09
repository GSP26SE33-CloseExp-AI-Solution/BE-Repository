using CloseExpAISolution.Domain;
using CloseExpAISolution.Domain.Enums;
using Xunit;

namespace CloseExpAISolution.Application.Tests;

public class InternalRolePolicyTests
{
    [Theory]
    [InlineData((int)RoleUser.PackagingStaff, true)]
    [InlineData((int)RoleUser.MarketingStaff, true)]
    [InlineData((int)RoleUser.SupermarketStaff, true)]
    [InlineData((int)RoleUser.DeliveryStaff, true)]
    [InlineData((int)RoleUser.Admin, false)]
    [InlineData((int)RoleUser.Vendor, false)]
    [InlineData(999, false)]
    public void IsAllowedForAdminInternalRegistration_matches_policy(int roleId, bool expected)
    {
        var actual = InternalRolePolicy.IsAllowedForAdminInternalRegistration(roleId);
        Assert.Equal(expected, actual);
    }
}
