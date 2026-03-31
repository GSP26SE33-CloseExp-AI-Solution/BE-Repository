using System.ComponentModel.DataAnnotations;

namespace CloseExpAISolution.Application.DTOs.Request;

public class CreateCustomerAddressDto
{
    [Required(ErrorMessage = "Số điện thoại là bắt buộc")]
    [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tên người nhận là bắt buộc")]
    [StringLength(100, ErrorMessage = "Tên người nhận không được vượt quá 100 ký tự")]
    public string RecipientName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Địa chỉ là bắt buộc")]
    [StringLength(500, ErrorMessage = "Địa chỉ không được vượt quá 500 ký tự")]
    public string AddressLine { get; set; } = string.Empty;

    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public bool IsDefault { get; set; }
}

public class UpdateCustomerAddressDto
{
    [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    public string? Phone { get; set; }

    [StringLength(100, ErrorMessage = "Tên người nhận không được vượt quá 100 ký tự")]
    public string? RecipientName { get; set; }

    [StringLength(500, ErrorMessage = "Địa chỉ không được vượt quá 500 ký tự")]
    public string? AddressLine { get; set; }

    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
}
