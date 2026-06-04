using AutoMapper;
using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Application.Services.Fulfillment;
using CloseExpAISolution.Domain.Entities;

namespace CloseExpAISolution.Application.Mappings;

public class RefundMappingProfile : Profile
{
    public RefundMappingProfile()
    {
        CreateMap<Refund, RefundResponseDto>()
            .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.RefundedOrderItemIds, opt => opt.MapFrom(s => RefundDtoEnricher.ParseRefundItemIds(s.RefundedOrderItemIdsJson)))
            .ForMember(d => d.IsFullOrderRefund, opt => opt.Ignore())
            .ForMember(d => d.Items, opt => opt.Ignore())
            .ForMember(d => d.Steps, opt => opt.Ignore());
    }
}
