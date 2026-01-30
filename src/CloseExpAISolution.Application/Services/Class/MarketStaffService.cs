using System.Linq.Expressions;
using AutoMapper;
using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.Services.Interface;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Infrastructure.UnitOfWork;

namespace CloseExpAISolution.Application.Services.Class;

public class MarketStaffService : IMarketStaffService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public MarketStaffService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public Task<MarketStaff?> GetByIdAsync(int id) => _unitOfWork.MarketStaffRepository.GetByIdAsync(id);
    public Task<IEnumerable<MarketStaff>> GetAllAsync() => _unitOfWork.MarketStaffRepository.GetAllAsync();
    public Task<IEnumerable<MarketStaff>> FindAsync(Expression<Func<MarketStaff, bool>> predicate) => _unitOfWork.MarketStaffRepository.FindAsync(predicate);
    public Task<MarketStaff?> FirstOrDefaultAsync(Expression<Func<MarketStaff, bool>> predicate) => _unitOfWork.MarketStaffRepository.FirstOrDefaultAsync(predicate);
    public Task<int> CountAsync(Expression<Func<MarketStaff, bool>>? predicate = null) => _unitOfWork.MarketStaffRepository.CountAsync(predicate);
    public Task<bool> ExistsAsync(Expression<Func<MarketStaff, bool>> predicate) => _unitOfWork.MarketStaffRepository.ExistsAsync(predicate);

    public async Task<MarketStaff> AddAsync(MarketStaff entity, CancellationToken cancellationToken = default)
    {
        var added = await _unitOfWork.MarketStaffRepository.AddAsync(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return added;
    }

    public async Task<IEnumerable<MarketStaff>> AddRangeAsync(IEnumerable<MarketStaff> entities, CancellationToken cancellationToken = default)
    {
        var added = await _unitOfWork.MarketStaffRepository.AddRangeAsync(entities);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return added;
    }

    public async Task UpdateAsync(MarketStaff entity, CancellationToken cancellationToken = default)
    {
        _unitOfWork.MarketStaffRepository.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateRangeAsync(IEnumerable<MarketStaff> entities, CancellationToken cancellationToken = default)
    {
        _unitOfWork.MarketStaffRepository.UpdateRange(entities);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(MarketStaff entity, CancellationToken cancellationToken = default)
    {
        _unitOfWork.MarketStaffRepository.Delete(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteRangeAsync(IEnumerable<MarketStaff> entities, CancellationToken cancellationToken = default)
    {
        _unitOfWork.MarketStaffRepository.DeleteRange(entities);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<MarketStaffResponseDto?> GetByIdWithDtoAsync(Guid id)
    {
        var marketStaff = await _unitOfWork.MarketStaffRepository.FirstOrDefaultAsync(x => x.MarketStaffId == id);
        if (marketStaff == null) return null;

        return _mapper.Map<MarketStaffResponseDto>(marketStaff);
    }

    public async Task<IEnumerable<MarketStaffResponseDto>> GetAllWithDtoAsync()
    {
        var items = await _unitOfWork.MarketStaffRepository.GetAllAsync();
        return _mapper.Map<IEnumerable<MarketStaffResponseDto>>(items);
    }

    public async Task<MarketStaffResponseDto> CreateMarketStaffAsync(CreateMarketStaffRequestDto request, CancellationToken cancellationToken = default)
    {
        var marketStaff = _mapper.Map<MarketStaff>(request);

        var added = await _unitOfWork.MarketStaffRepository.AddAsync(marketStaff);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<MarketStaffResponseDto>(added);
    }

    public async Task UpdateMarketStaffAsync(Guid id, UpdateMarketStaffRequestDto request, CancellationToken cancellationToken = default)
    {
        var marketStaff = await _unitOfWork.MarketStaffRepository.FirstOrDefaultAsync(x => x.MarketStaffId == id);
        if (marketStaff == null) throw new KeyNotFoundException($"Không tìm thấy nhân viên siêu thị với id {id}");

        _mapper.Map(request, marketStaff);

        _unitOfWork.MarketStaffRepository.Update(marketStaff);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteMarketStaffAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var marketStaff = await _unitOfWork.MarketStaffRepository.FirstOrDefaultAsync(x => x.MarketStaffId == id);
        if (marketStaff == null) throw new KeyNotFoundException($"Không tìm thấy nhân viên siêu thị với id {id}");

        await DeleteAsync(marketStaff, cancellationToken);
    }
}

