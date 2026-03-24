using AutoMapper;
using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Domain.Entities;

namespace CloseExpAISolution.Application.Mappings;

public class CategoryMappingProfile : Profile
{
    public CategoryMappingProfile()
    {
        CreateMap<Category, CategoryResponseDto>()
            .ForMember(d => d.ParentName, opt => opt.Ignore());

        CreateMap<CreateCategoryRequestDto, Category>()
            .ForMember(d => d.CategoryId, opt => opt.Ignore())
            .ForMember(d => d.ParentCategory, opt => opt.Ignore())
            .ForMember(d => d.ChildCategories, opt => opt.Ignore())
            .ForMember(d => d.Products, opt => opt.Ignore());

        CreateMap<UpdateCategoryRequestDto, Category>()
            .ForMember(d => d.CategoryId, opt => opt.Ignore())
            .ForMember(d => d.ParentCategory, opt => opt.Ignore())
            .ForMember(d => d.ChildCategories, opt => opt.Ignore())
            .ForMember(d => d.Products, opt => opt.Ignore());
    }
}
