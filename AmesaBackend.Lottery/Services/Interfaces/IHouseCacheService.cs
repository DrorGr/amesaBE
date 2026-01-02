namespace AmesaBackend.Lottery.Services.Interfaces;

public interface IHouseCacheService
{
    Task InvalidateHouseCachesAsync();
}
