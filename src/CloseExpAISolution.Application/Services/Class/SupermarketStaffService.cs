using System.Linq.Expressions;
using AutoMapper;
using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.Services.Interface;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Infrastructure.UnitOfWork;

namespace CloseExpAISolution.Application.Services.Class;

public class SupermarketStaffService : ISupermarketStaffService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public SupermarketStaffService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public Task<SupermarketStaff?> GetByIdAsync(int id) => _unitOfWork.SupermarketStaffRepository.GetByIdAsync(id);
    public Task<IEnumerable<SupermarketStaff>> GetAllAsync() => _unitOfWork.SupermarketStaffRepository.GetAllAsync();
    public Task<IEnumerable<SupermarketStaff>> FindAsync(Expression<Func<SupermarketStaff, bool>> predicate) => _unitOfWork.SupermarketStaffRepository.FindAsync(predicate);
    public Task<SupermarketStaff?> FirstOrDefaultAsync(Expression<Func<SupermarketStaff, bool>> predicate) => _unitOfWork.SupermarketStaffRepository.FirstOrDefaultAsync(predicate);
    public Task<int> CountAsync(Expression<Func<SupermarketStaff, bool>>? predicate = null) => _unitOfWork.SupermarketStaffRepository.CountAsync(predicate);
    public Task<bool> ExistsAsync(Expression<Func<SupermarketStaff, bool>> predicate) => _unitOfWork.SupermarketStaffRepository.ExistsAsync(predicate);

    public async Task<SupermarketStaff> AddAsync(SupermarketStaff entity, CancellationToken cancellationToken = default)
    {
        var added = await _unitOfWork.SupermarketStaffRepository.AddAsync(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return added;
    }

    public async Task<IEnumerable<SupermarketStaff>> AddRangeAsync(IEnumerable<SupermarketStaff> entities, CancellationToken cancellationToken = default)
    {
        var added = await _unitOfWork.SupermarketStaffRepository.AddRangeAsync(entities);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return added;
    }

    public async Task UpdateAsync(SupermarketStaff entity, CancellationToken cancellationToken = default)
    {
        _unitOfWork.SupermarketStaffRepository.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateRangeAsync(IEnumerable<SupermarketStaff> entities, CancellationToken cancellationToken = default)
    {
        _unitOfWork.SupermarketStaffRepository.UpdateRange(entities);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(SupermarketStaff entity, CancellationToken cancellationToken = default)
    {
        _unitOfWork.SupermarketStaffRepository.Delete(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteRangeAsync(IEnumerable<SupermarketStaff> entities, CancellationToken cancellationToken = default)
    {
        _unitOfWork.SupermarketStaffRepository.DeleteRange(entities);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<MarketStaffResponseDto?> GetByIdWithDtoAsync(Guid id)
    {
        var marketStaff = await _unitOfWork.SupermarketStaffRepository.FirstOrDefaultAsync(x => x.SupermarketStaffId == id);
        if (marketStaff == null) return null;

        return _mapper.Map<MarketStaffResponseDto>(marketStaff);
    }

    public async Task<IEnumerable<MarketStaffResponseDto>> GetAllWithDtoAsync()
    {
        var items = await _unitOfWork.SupermarketStaffRepository.GetAllAsync();
        return _mapper.Map<IEnumerable<MarketStaffResponseDto>>(items);
    }

    public async Task<MarketStaffResponseDto> CreateMarketStaffAsync(CreateMarketStaffRequestDto request, CancellationToken cancellationToken = default)
    {
        var marketStaff = _mapper.Map<SupermarketStaff>(request);

        var added = await _unitOfWork.SupermarketStaffRepository.AddAsync(marketStaff);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<MarketStaffResponseDto>(added);
    }

    public async Task UpdateMarketStaffAsync(Guid id, UpdateMarketStaffRequestDto request, CancellationToken cancellationToken = default)
    {
        var marketStaff = await _unitOfWork.SupermarketStaffRepository.FirstOrDefaultAsync(x => x.SupermarketStaffId == id);
        if (marketStaff == null) throw new KeyNotFoundException($"Không tìm thấy nhân viên siêu thị với id {id}");

        _mapper.Map(request, marketStaff);

        _unitOfWork.SupermarketStaffRepository.Update(marketStaff);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteMarketStaffAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var marketStaff = await _unitOfWork.SupermarketStaffRepository.FirstOrDefaultAsync(x => x.SupermarketStaffId == id);
        if (marketStaff == null) throw new KeyNotFoundException($"Không tìm thấy nhân viên siêu thị với id {id}");

        await DeleteAsync(marketStaff, cancellationToken);
    }

    public async Task<Guid?> GetSupermarketIdByUserIdAsync(Guid userId)
    {
        var marketStaff = await _unitOfWork.SupermarketStaffRepository.FirstOrDefaultAsync(ms => ms.UserId == userId);
        return marketStaff?.SupermarketId;
    }
}

