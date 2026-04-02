using System.ComponentModel.DataAnnotations;

namespace CloseExpAISolution.Application.DTOs.Request;

public class AddCartItemRequestDto
{
    [Required]
    public Guid LotId { get; set; }

    [Range(typeof(decimal), "0.0001", "999999999")]
    public decimal Quantity { get; set; }
}

public class UpdateCartItemRequestDto
{
    [Range(typeof(decimal), "0.0001", "999999999")]
    public decimal Quantity { get; set; }
}
