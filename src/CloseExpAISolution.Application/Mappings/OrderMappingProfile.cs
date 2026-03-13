using AutoMapper;
using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Domain.Entities;

namespace CloseExpAISolution.Application.Mappings;

public class OrderMappingProfile : Profile
{
    public OrderMappingProfile()
    {
        CreateMap<OrderItem, OrderItemResponseDto>()
            .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src =>
                src.StockLot != null && src.StockLot.Product != null ? src.StockLot.Product.Name : null))
            .ForMember(dest => dest.ExpiryDate, opt => opt.MapFrom(src =>
                src.StockLot != null ? src.StockLot.ExpiryDate : (DateTime?)null));

        CreateMap<Order, OrderResponseDto>()
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User != null ? src.User.FullName ?? src.User.Email : null))
            .ForMember(dest => dest.TimeSlotDisplay, opt => opt.MapFrom(src =>
                src.TimeSlot != null ? $"{src.TimeSlot.StartTime:hh\\:mm} - {src.TimeSlot.EndTime:hh\\:mm}" : null))
            .ForMember(dest => dest.PickupPointName, opt => opt.MapFrom(src => src.PickupPoint != null ? src.PickupPoint.Name : null))
            .ForMember(dest => dest.OrderItems, opt => opt.MapFrom(src => src.OrderItems));
    }
}
