using System.ComponentModel.DataAnnotations;

namespace CloseExpAISolution.Application.DTOs.Request;

public class UpdateProductUnitRequestDto
{
    [Required]
    public Guid UnitId { get; set; }
}
