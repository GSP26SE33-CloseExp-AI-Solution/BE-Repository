using CloseExpAISolution.Application.Services.Fulfillment;
using Xunit;

namespace CloseExpAISolution.Application.Tests;

public class FulfillmentFailureRefundHelperTests
{
    [Fact]
    public void BuildFailureNote_WithNotes_IncludesBothParts()
    {
        var note = FulfillmentFailureRefundHelper.BuildFailureNote(
            "Giao hàng thất bại",
            "Khách không nghe máy",
            "Gọi 3 lần");

        Assert.Equal("Giao hàng thất bại: Khách không nghe máy | Gọi 3 lần", note);
    }

    [Fact]
    public void BuildFailureNote_WithoutNotes_OmitsExtra()
    {
        var note = FulfillmentFailureRefundHelper.BuildFailureNote(
            "Đóng gói thất bại",
            "Hết hàng",
            null);

        Assert.Equal("Đóng gói thất bại: Hết hàng", note);
    }
}
