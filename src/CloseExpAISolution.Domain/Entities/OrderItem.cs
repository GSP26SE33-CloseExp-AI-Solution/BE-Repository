namespace CloseExpAISolution.Domain.Entities;

public class OrderItem
{
    public Guid OrderItemId { get; set; }
    public Guid OrderId { get; set; }
    public Guid LotId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }


    public Order? Order { get; set; }
    public ProductLot? ProductLot { get; set; }
}

