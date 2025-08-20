using Microsoft.Extensions.Caching.Memory;

namespace GastosHogarAPI.Services
{
    public class CacheService
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<CacheService> _logger;

        public CacheService(IMemoryCache cache, ILogger<CacheService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public T? Get<T>(string key)
        {
            if (_cache.TryGetValue(key, out T? value))
            {
                _logger.LogDebug("Cache HIT para clave: {Key}", key);
                return value;
            }

            _logger.LogDebug("Cache MISS para clave: {Key}", key);
            return default;
        }

        public async Task<T?> GetOrCreateAsync<T>(
            string key,
            Func<Task<T>> factory,
            TimeSpan? expiration = null)
        {
            if (_cache.TryGetValue(key, out T? cached))
            {
                _logger.LogDebug("Cache HIT para clave: {Key}", key);
                return cached;
            }

            _logger.LogDebug("Cache MISS para clave: {Key} - Ejecutando factory", key);

            var result = await factory();

            var options = new MemoryCacheEntryOptions();
            if (expiration.HasValue)
            {
                options.AbsoluteExpirationRelativeToNow = expiration;
            }
            else
            {
                // Tiempo por defecto: 30 minutos
                options.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);
            }

            options.SlidingExpiration = TimeSpan.FromMinutes(5); // Renovar si se accede

            _cache.Set(key, result, options);
            _logger.LogDebug("Guardado en cache con clave: {Key}", key);

            return result;
        }

        public void Remove(string key)
        {
            _cache.Remove(key);
            _logger.LogDebug("Eliminado del cache: {Key}", key);
        }

        public void RemoveByPattern(string pattern)
        {
            // Para MemoryCache, no hay una forma directa de eliminar por patrón
            // Esta es una implementación básica
            _logger.LogDebug("Solicitud de eliminación por patrón: {Pattern}", pattern);

            // En una implementación real con Redis, podrías usar SCAN
            // Por ahora, registramos la solicitud para debugging
        }

        public void Clear()
        {
            if (_cache is MemoryCache memoryCache)
            {
                memoryCache.Clear();
                _logger.LogInformation("Cache completamente limpiado");
            }
        }
    }
}