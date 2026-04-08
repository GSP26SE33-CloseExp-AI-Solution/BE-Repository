using System.Text.Json;
using AutoMapper;
using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Domain.Entities;

namespace CloseExpAISolution.Application.Mappings;

public class RefundMappingProfile : Profile
{
    public RefundMappingProfile()
    {
        CreateMap<Refund, RefundResponseDto>()
            .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.RefundedOrderItemIds, opt => opt.MapFrom(s => ParseRefundItemIds(s.RefundedOrderItemIdsJson)));
    }

    private static IReadOnlyList<Guid>? ParseRefundItemIds(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;
        try
        {
            return JsonSerializer.Deserialize<List<Guid>>(json);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
