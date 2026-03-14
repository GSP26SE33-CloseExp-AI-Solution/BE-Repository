using System.Linq.Expressions;
using AutoMapper;
using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Application.Services.Interface;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Domain.Enums;
using CloseExpAISolution.Infrastructure.UnitOfWork;

namespace CloseExpAISolution.Application.Services.Class;

public class SupermarketService : ISupermarketService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public SupermarketService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public Task<Supermarket?> GetByIdAsync(int id) => _unitOfWork.SupermarketRepository.GetByIdAsync(id);
    public Task<IEnumerable<Supermarket>> GetAllAsync() => _unitOfWork.SupermarketRepository.GetAllAsync();
    public Task<IEnumerable<Supermarket>> FindAsync(Expression<Func<Supermarket, bool>> predicate) => _unitOfWork.SupermarketRepository.FindAsync(predicate);
    public Task<Supermarket?> FirstOrDefaultAsync(Expression<Func<Supermarket, bool>> predicate) => _unitOfWork.SupermarketRepository.FirstOrDefaultAsync(predicate);
    public Task<int> CountAsync(Expression<Func<Supermarket, bool>>? predicate = null) => _unitOfWork.SupermarketRepository.CountAsync(predicate);
    public Task<bool> ExistsAsync(Expression<Func<Supermarket, bool>> predicate) => _unitOfWork.SupermarketRepository.ExistsAsync(predicate);

    public async Task<Supermarket> AddAsync(Supermarket entity, CancellationToken cancellationToken = default)
    {
        var added = await _unitOfWork.SupermarketRepository.AddAsync(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return added;
    }

    public async Task<IEnumerable<Supermarket>> AddRangeAsync(IEnumerable<Supermarket> entities, CancellationToken cancellationToken = default)
    {
        var added = await _unitOfWork.SupermarketRepository.AddRangeAsync(entities);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return added;
    }

    public async Task UpdateAsync(Supermarket entity, CancellationToken cancellationToken = default)
    {
        _unitOfWork.SupermarketRepository.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateRangeAsync(IEnumerable<Supermarket> entities, CancellationToken cancellationToken = default)
    {
        _unitOfWork.SupermarketRepository.UpdateRange(entities);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Supermarket entity, CancellationToken cancellationToken = default)
    {
        _unitOfWork.SupermarketRepository.Delete(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteRangeAsync(IEnumerable<Supermarket> entities, CancellationToken cancellationToken = default)
    {
        _unitOfWork.SupermarketRepository.DeleteRange(entities);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<SupermarketResponseDto?> GetByIdWithDtoAsync(Guid id)
    {
        var supermarket = await _unitOfWork.SupermarketRepository.FirstOrDefaultAsync(x => x.SupermarketId == id);
        if (supermarket == null) return null;

        return _mapper.Map<SupermarketResponseDto>(supermarket);
    }

    public async Task<IEnumerable<SupermarketResponseDto>> GetAllWithDtoAsync()
    {
        var items = await _unitOfWork.SupermarketRepository.GetAllAsync();
        return _mapper.Map<IEnumerable<SupermarketResponseDto>>(items);
    }

    public async Task<IEnumerable<SupermarketResponseDto>> GetAvailableWithDtoAsync()
    {
        var allSupermarkets = await _unitOfWork.SupermarketRepository.GetAllAsync();

        // Lấy danh sách SupermarketId đã có nhân viên đăng ký
        var registeredSupermarketIds = (await _unitOfWork.Repository<SupermarketStaff>()
            .GetAllAsync())
            .Select(ms => ms.SupermarketId)
            .ToHashSet();

        // Filter: chỉ trả về siêu thị chưa có nhân viên
        var availableSupermarkets = allSupermarkets
            .Where(s => !registeredSupermarketIds.Contains(s.SupermarketId));

        return _mapper.Map<IEnumerable<SupermarketResponseDto>>(availableSupermarkets);
    }

    public async Task<IEnumerable<SupermarketResponseDto>> SearchAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return await GetAllWithDtoAsync();

        var queryLower = query.Trim().ToLower();

        var allSupermarkets = await _unitOfWork.SupermarketRepository.GetAllAsync();

        // Lấy danh sách SupermarketId đã có nhân viên
        var registeredSupermarketIds = (await _unitOfWork.Repository<SupermarketStaff>()
            .GetAllAsync())
            .Select(ms => ms.SupermarketId)
            .ToHashSet();

        // Filter: tìm theo tên hoặc địa chỉ (case-insensitive)
        var searchResults = allSupermarkets
            .Where(s =>
                s.Name.ToLower().Contains(queryLower) ||
                s.Address.ToLower().Contains(queryLower))
            .Select(s => new
            {
                Supermarket = s,
                HasStaff = registeredSupermarketIds.Contains(s.SupermarketId)
            })
            .ToList();

        var dtos = _mapper.Map<List<SupermarketResponseDto>>(searchResults.Select(x => x.Supermarket));

        // Gắn thêm thông tin HasStaff vào response nếu cần
        // Hiện SupermarketResponseDto không có field HasStaff
        // Nếu cần, có thể extend DTO hoặc trả về trong metadata

        return dtos;
    }

    public async Task<SupermarketResponseDto> CreateSupermarketAsync(CreateSupermarketRequestDto request, CancellationToken cancellationToken = default)
    {
        var supermarket = _mapper.Map<Supermarket>(request);

        var added = await _unitOfWork.SupermarketRepository.AddAsync(supermarket);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<SupermarketResponseDto>(added);
    }

    public async Task UpdateSupermarketAsync(Guid id, UpdateSupermarketRequestDto request, CancellationToken cancellationToken = default)
    {
        var supermarket = await _unitOfWork.SupermarketRepository.FirstOrDefaultAsync(x => x.SupermarketId == id);
        if (supermarket == null) throw new KeyNotFoundException($"Không tìm thấy siêu thị với id {id}");

        _mapper.Map(request, supermarket);

        _unitOfWork.SupermarketRepository.Update(supermarket);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteSupermarketAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var supermarket = await _unitOfWork.SupermarketRepository.FirstOrDefaultAsync(x => x.SupermarketId == id);
        if (supermarket == null) throw new KeyNotFoundException($"Không tìm thấy siêu thị với id {id}");

        await DeleteAsync(supermarket, cancellationToken);
    }
}

