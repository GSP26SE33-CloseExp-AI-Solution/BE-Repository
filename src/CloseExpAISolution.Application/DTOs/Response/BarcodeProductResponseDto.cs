namespace CloseExpAISolution.Application.DTOs.Response;

public class VietnameseBarcodeResponse
{
    public string Barcode { get; set; } = string.Empty;
    public bool IsVietnamese { get; set; }
    public string Message { get; set; } = string.Empty;
}
