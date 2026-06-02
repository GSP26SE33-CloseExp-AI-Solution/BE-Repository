namespace CloseExpAISolution.Application.Services.Interface;

public interface IStalePaidUnclaimedPackagingProcessor
{
    Task<int> ProcessAsync(CancellationToken cancellationToken = default);
}
