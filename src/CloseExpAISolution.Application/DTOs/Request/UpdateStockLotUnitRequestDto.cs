using System.ComponentModel.DataAnnotations;

namespace CloseExpAISolution.Application.DTOs.Request;

public class UpdateStockLotUnitRequestDto
{
    [Required]
    public Guid UnitId { get; set; }
}
