using System.ComponentModel.DataAnnotations;
using CloseExpAISolution.Application.Auth;
using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Domain.Enums;
using Xunit;

namespace CloseExpAISolution.Application.Tests;

public class InternalStaffRegistrationHelperTests
{
    [Fact]
    public void ResolveTemporaryPassword_prefers_request_password()
    {
        var actual = InternalStaffRegistrationHelper.ResolveTemporaryPassword(
            "Aa@12345",
            "CloseExp@.123");

        Assert.Equal("Aa@12345", actual);
    }

    [Fact]
    public void ResolveTemporaryPassword_uses_system_config_when_request_missing()
    {
        var actual = InternalStaffRegistrationHelper.ResolveTemporaryPassword(
            null,
            "Abc@12345");

        Assert.Equal("Abc@12345", actual);
    }

    [Fact]
    public void ResolveTemporaryPassword_uses_fallback_when_both_missing()
    {
        var actual = InternalStaffRegistrationHelper.ResolveTemporaryPassword(
            "   ",
            null);

        Assert.Equal(InternalStaffRegistrationHelper.FallbackDefaultPassword, actual);
    }

    [Theory]
    [InlineData((int)RoleUser.PackagingStaff, "PackagingStaff")]
    [InlineData((int)RoleUser.MarketingStaff, "MarketingStaff")]
    [InlineData((int)RoleUser.SupermarketStaff, "SupermarketStaff")]
    [InlineData((int)RoleUser.DeliveryStaff, "DeliveryStaff")]
    public void GetRoleLabel_returns_enum_name_for_valid_role(int roleId, string expected)
    {
        var actual = InternalStaffRegistrationHelper.GetRoleLabel(roleId);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GetRoleLabel_returns_fallback_for_unknown_role()
    {
        var actual = InternalStaffRegistrationHelper.GetRoleLabel(999);
        Assert.Equal("Role-999", actual);
    }

    [Fact]
    public void AdminRegisterInternalRequestDto_allows_missing_password()
    {
        var request = new AdminRegisterInternalRequestDto
        {
            FullName = "Internal Staff",
            Email = "internal.staff@example.com",
            Phone = "0909000009",
            Password = null,
            RoleId = (int)RoleUser.PackagingStaff
        };

        var results = new List<ValidationResult>();
        var context = new ValidationContext(request);
        var isValid = Validator.TryValidateObject(request, context, results, true);

        Assert.True(isValid);
    }
}
