using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace BarberBooking.Api.Services;

public sealed class SchedulingCache(IDistributedCache cache)
{
    private static readonly DistributedCacheEntryOptions AvailabilityLifetime = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30)
    };

    public async Task<IReadOnlyList<DateTime>?> GetAvailability(Guid tenantId, Guid barberId, IReadOnlyCollection<Guid> serviceIds, DateOnly date, CancellationToken cancellationToken = default)
    {
        var version = await GetVersion(tenantId, cancellationToken);
        var services = string.Join('-', serviceIds.OrderBy(x => x));
        var key = $"availability:{tenantId:N}:{version}:{barberId:N}:{services}:{date:yyyyMMdd}";
        var json = await cache.GetStringAsync(key, cancellationToken);
        return json is null ? null : JsonSerializer.Deserialize<DateTime[]>(json);
    }

    public async Task SetAvailability(Guid tenantId, Guid barberId, IReadOnlyCollection<Guid> serviceIds, DateOnly date, IReadOnlyList<DateTime> slots, CancellationToken cancellationToken = default)
    {
        var version = await GetVersion(tenantId, cancellationToken);
        var services = string.Join('-', serviceIds.OrderBy(x => x));
        var key = $"availability:{tenantId:N}:{version}:{barberId:N}:{services}:{date:yyyyMMdd}";
        await cache.SetStringAsync(key, JsonSerializer.Serialize(slots), AvailabilityLifetime, cancellationToken);
    }

    public Task InvalidateTenant(Guid tenantId, CancellationToken cancellationToken = default) =>
        cache.SetStringAsync($"availability-version:{tenantId:N}", Guid.NewGuid().ToString("N"), cancellationToken);

    private async Task<string> GetVersion(Guid tenantId, CancellationToken cancellationToken)
    {
        var key = $"availability-version:{tenantId:N}";
        var version = await cache.GetStringAsync(key, cancellationToken);
        if (version is not null) return version;

        version = "1";
        await cache.SetStringAsync(key, version, new DistributedCacheEntryOptions(), cancellationToken);
        return version;
    }
}
