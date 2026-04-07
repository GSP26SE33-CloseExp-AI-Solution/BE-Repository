using System.ComponentModel.DataAnnotations;
using CloseExpAISolution.Application.DTOs.Request;
using Xunit;

namespace CloseExpAISolution.Application.Tests;

public class PackagingRequestDtoValidationTests
{
    [Fact]
    public void FailPackagingRequest_RejectsWhitespaceFailureReason()
    {
        var dto = new FailPackagingOrderRequestDto
        {
            FailureReason = "   ",
            Notes = "test"
        };

        var context = new ValidationContext(dto);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(dto, context, results, validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(FailPackagingOrderRequestDto.FailureReason)));
    }

    [Fact]
    public void FailPackagingRequest_AcceptsNonWhitespaceFailureReason()
    {
        var dto = new FailPackagingOrderRequestDto
        {
            FailureReason = "Hết hàng thực tế"
        };

        var context = new ValidationContext(dto);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(dto, context, results, validateAllProperties: true);

        Assert.True(isValid);
    }
}
