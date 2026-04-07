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
                src.StockLot != null ? src.StockLot.ExpiryDate : (DateTime?)null))
            .ForMember(dest => dest.PackagingStatus, opt => opt.MapFrom(src => src.PackagingStatus.ToString()))
            .ForMember(dest => dest.DeliveryStatus, opt => opt.MapFrom(src =>
                src.DeliveryStatus.HasValue ? src.DeliveryStatus.Value.ToString() : null));

        CreateMap<Order, OrderResponseDto>()
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User != null ? src.User.FullName ?? src.User.Email : null))
            .ForMember(dest => dest.TimeSlotId, opt => opt.MapFrom(src => src.TimeSlotId))
            .ForMember(dest => dest.TimeSlotDisplay, opt => opt.MapFrom(src =>
                src.DeliveryTimeSlot != null ? $"{src.DeliveryTimeSlot.StartTime:hh\\:mm} - {src.DeliveryTimeSlot.EndTime:hh\\:mm}" : null))
            .ForMember(dest => dest.CollectionId, opt => opt.MapFrom(src => src.CollectionId))
            .ForMember(dest => dest.CollectionPointName, opt => opt.MapFrom(src => src.CollectionPoint != null ? src.CollectionPoint.Name : null))
            .ForMember(dest => dest.OrderItems, opt => opt.MapFrom(src => src.OrderItems));
    }
}
