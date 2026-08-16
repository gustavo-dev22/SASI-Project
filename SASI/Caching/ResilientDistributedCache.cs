using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace SASI.Caching
{
    public class ResilientDistributedCache : IDistributedCache
    {
        private readonly IDistributedCache _primary;
        private readonly IDistributedCache _fallback;
        private readonly ILogger<ResilientDistributedCache> _logger;

        public ResilientDistributedCache(IDistributedCache primary, IDistributedCache fallback, ILogger<ResilientDistributedCache> logger)
        {
            _primary = primary;
            _fallback = fallback;
            _logger = logger;
        }

        public byte[]? Get(string key)
        {
            try { return _primary.Get(key); }
            catch (Exception ex) { return Fallback(key, () => _fallback.Get(key), ex); }
        }

        public async Task<byte[]?> GetAsync(string key, CancellationToken token = default)
        {
            try { return await _primary.GetAsync(key, token); }
            catch (Exception ex) { return await FallbackAsync(key, () => _fallback.GetAsync(key, token), ex); }
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            try { _primary.Set(key, value, options); }
            catch (Exception ex) { Fallback(key, () => { _fallback.Set(key, value, options); return null; }, ex); }
        }

        public async Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
        {
            try { await _primary.SetAsync(key, value, options, token); }
            catch (Exception ex) { await FallbackAsync(key, async () => { await _fallback.SetAsync(key, value, options, token); return null; }, ex); }
        }

        public void Refresh(string key)
        {
            try { _primary.Refresh(key); }
            catch (Exception ex) { Fallback(key, () => { _fallback.Refresh(key); return null; }, ex); }
        }

        public async Task RefreshAsync(string key, CancellationToken token = default)
        {
            try { await _primary.RefreshAsync(key, token); }
            catch (Exception ex) { await FallbackAsync(key, async () => { await _fallback.RefreshAsync(key, token); return null; }, ex); }
        }

        public void Remove(string key)
        {
            try { _primary.Remove(key); }
            catch (Exception ex) { Fallback(key, () => { _fallback.Remove(key); return null; }, ex); }
        }

        public async Task RemoveAsync(string key, CancellationToken token = default)
        {
            try { await _primary.RemoveAsync(key, token); }
            catch (Exception ex) { await FallbackAsync(key, async () => { await _fallback.RemoveAsync(key, token); return null; }, ex); }
        }

        private byte[]? Fallback(string key, Func<byte[]?> fallback, Exception ex)
        {
            _logger.LogWarning(ex, "Cache principal no disponible, se usa memoria para {Key}", key);
            return fallback();
        }

        private async Task<byte[]?> FallbackAsync(string key, Func<Task<byte[]?>> fallback, Exception ex)
        {
            _logger.LogWarning(ex, "Cache principal no disponible, se usa memoria para {Key}", key);
            return await fallback();
        }
    }
}
