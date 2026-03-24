using AutoMapper;
using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Application.Services.Interface;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Infrastructure.UnitOfWork;

namespace CloseExpAISolution.Application.Services.Class;

public class CategoryService : ICategoryService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CategoryService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<IEnumerable<CategoryResponseDto>> GetAllAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var all = (await _unitOfWork.Repository<Category>().GetAllAsync()).ToList();
        var filtered = includeInactive ? all : all.Where(c => c.IsActive).ToList();
        return MapWithParentNames(filtered, all);
    }

    public async Task<CategoryResponseDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var all = (await _unitOfWork.Repository<Category>().GetAllAsync()).ToList();
        var entity = all.FirstOrDefault(c => c.CategoryId == id);
        if (entity == null)
            return null;

        return MapWithParentNames(new List<Category> { entity }, all).First();
    }

    public async Task<CategoryResponseDto> CreateAsync(CreateCategoryRequestDto request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Name is required.", nameof(request));

        await EnsureNameUniqueAsync(request.Name, null, cancellationToken);

        if (request.ParentCatId.HasValue)
        {
            var parentExists = await _unitOfWork.Repository<Category>()
                .ExistsAsync(c => c.CategoryId == request.ParentCatId.Value);
            if (!parentExists)
                throw new KeyNotFoundException($"Parent category not found: {request.ParentCatId}");
        }

        var entity = _mapper.Map<Category>(request);
        entity.CategoryId = Guid.NewGuid();

        await _unitOfWork.Repository<Category>().AddAsync(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return (await GetByIdAsync(entity.CategoryId, cancellationToken))!;
    }

    public async Task UpdateAsync(Guid id, UpdateCategoryRequestDto request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Name is required.", nameof(request));

        var entity = await _unitOfWork.Repository<Category>().FirstOrDefaultAsync(c => c.CategoryId == id);
        if (entity == null)
            throw new KeyNotFoundException($"Category not found: {id}");

        await EnsureNameUniqueAsync(request.Name, id, cancellationToken);

        if (request.ParentCatId == id)
            throw new InvalidOperationException("A category cannot be its own parent.");

        if (request.ParentCatId.HasValue)
        {
            var parentExists = await _unitOfWork.Repository<Category>()
                .ExistsAsync(c => c.CategoryId == request.ParentCatId.Value);
            if (!parentExists)
                throw new KeyNotFoundException($"Parent category not found: {request.ParentCatId}");

            if (await WouldCreateCycleAsync(id, request.ParentCatId.Value, cancellationToken))
                throw new InvalidOperationException("Invalid parent: would create a circular reference.");
        }

        _mapper.Map(request, entity);
        _unitOfWork.Repository<Category>().Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Repository<Category>().FirstOrDefaultAsync(c => c.CategoryId == id);
        if (entity == null)
            throw new KeyNotFoundException($"Category not found: {id}");

        var hasChildren = await _unitOfWork.Repository<Category>().ExistsAsync(c => c.ParentCatId == id);
        if (hasChildren)
            throw new InvalidOperationException("Cannot delete a category that has child categories. Remove or reassign children first.");

        var hasProducts = await _unitOfWork.Repository<Product>().ExistsAsync(p => p.CategoryId == id);
        if (hasProducts)
            throw new InvalidOperationException("Cannot delete a category that is assigned to products.");

        var hasPromotions = await _unitOfWork.Repository<Promotion>().ExistsAsync(p => p.CategoryId == id);
        if (hasPromotions)
            throw new InvalidOperationException("Cannot delete a category that is used by promotions.");

        _unitOfWork.Repository<Category>().Delete(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureNameUniqueAsync(string name, Guid? excludeCategoryId, CancellationToken cancellationToken)
    {
        var normalized = name.Trim().ToLowerInvariant();
        var all = await _unitOfWork.Repository<Category>().GetAllAsync();
        var conflict = all.FirstOrDefault(c =>
            c.Name.Trim().ToLowerInvariant() == normalized
            && (!excludeCategoryId.HasValue || c.CategoryId != excludeCategoryId.Value));
        if (conflict != null)
            throw new InvalidOperationException($"A category with name '{name.Trim()}' already exists.");
    }

    /// <summary>
    /// True if assigning <paramref name="categoryId"/>.Parent = <paramref name="newParentId"/> would create a cycle
    /// (i.e. <paramref name="newParentId"/> is <paramref name="categoryId"/> or a descendant of it).
    /// </summary>
    private async Task<bool> WouldCreateCycleAsync(Guid categoryId, Guid newParentId, CancellationToken cancellationToken)
    {
        if (newParentId == categoryId)
            return true;

        var byId = (await _unitOfWork.Repository<Category>().GetAllAsync()).ToDictionary(c => c.CategoryId);
        if (!byId.ContainsKey(newParentId))
            return true;

        var current = (Guid?)newParentId;
        for (var guard = 0; guard < 256 && current.HasValue; guard++)
        {
            if (current == categoryId)
                return true;
            if (!byId.TryGetValue(current.Value, out var node))
                break;
            current = node.ParentCatId;
        }

        return false;
    }

    private List<CategoryResponseDto> MapWithParentNames(IReadOnlyList<Category> entities, IReadOnlyList<Category> all)
    {
        var byId = all.ToDictionary(c => c.CategoryId);
        var dtos = _mapper.Map<List<CategoryResponseDto>>(entities);
        foreach (var dto in dtos)
        {
            if (dto.ParentCatId.HasValue && byId.TryGetValue(dto.ParentCatId.Value, out var p))
                dto.ParentName = p.Name;
        }

        return dtos;
    }
}
