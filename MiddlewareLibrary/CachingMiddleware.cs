using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace MiddlewareLibrary.Middlewares
{
    /// <summary>
    /// Middleware responsável por gerenciar cache de respostas HTTP em memória para requisições idempotentes seguras (GET e HEAD).
    /// </summary>
    public class CachingMiddleware
    {
        private const int DefaultBufferSize = 4096;
        private readonly RequestDelegate _next;
        private readonly IMemoryCache _cache;
        private readonly ILogger<CachingMiddleware> _logger;
        private readonly TimeSpan _cacheDuration;

        /// <summary>
        /// Inicializa uma nova instância de <see cref="CachingMiddleware"/>.
        /// </summary>
        /// <param name="next">Próximo delegado no pipeline HTTP.</param>
        /// <param name="cache">Mecanismo de cache em memória.</param>
        /// <param name="logger">Instância de logging.</param>
        /// <param name="cacheDuration">Tempo de retenção das entradas em cache (padrão: 1 minuto).</param>
        public CachingMiddleware(
            RequestDelegate next,
            IMemoryCache cache,
            ILogger<CachingMiddleware> logger,
            TimeSpan? cacheDuration = null)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cacheDuration = cacheDuration ?? TimeSpan.FromMinutes(1);
        }

        /// <summary>
        /// Executa o fluxo de verificação e armazenamento de cache HTTP com pooling de buffers de memória.
        /// </summary>
        /// <param name="context">Contexto HTTP da requisição.</param>
        public async Task InvokeAsync(HttpContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            if (!HttpMethods.IsGet(context.Request.Method) && !HttpMethods.IsHead(context.Request.Method))
            {
                await _next(context);
                return;
            }

            var cacheKey = GenerateCacheKeyFromRequest(context.Request);

            if (_cache.TryGetValue(cacheKey, out var cachedResponse) && cachedResponse is string responseString)
            {
                _logger.LogInformation("Cache hit para a chave: {CacheKey}", cacheKey);
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(responseString);
                return;
            }

            var originalBodyStream = context.Response.Body;
            using var responseBodyStream = new MemoryStream();
            context.Response.Body = responseBodyStream;

            try
            {
                await _next(context);

                if (context.Response.StatusCode == StatusCodes.Status200OK)
                {
                    await CacheResponseBodyAsync(cacheKey, responseBodyStream);
                }

                await CopyStreamWithBufferPoolAsync(responseBodyStream, originalBodyStream);
            }
            finally
            {
                context.Response.Body = originalBodyStream;
            }
        }

        private async Task CacheResponseBodyAsync(string cacheKey, MemoryStream stream)
        {
            stream.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: DefaultBufferSize, leaveOpen: true);
            var responseText = await reader.ReadToEndAsync();
            stream.Seek(0, SeekOrigin.Begin);

            if (!string.IsNullOrEmpty(responseText))
            {
                _cache.Set(cacheKey, responseText, _cacheDuration);
                _logger.LogInformation("Resposta armazenada em cache para a chave: {CacheKey} com TTL de {TTL}", cacheKey, _cacheDuration);
            }
        }

        private static async Task CopyStreamWithBufferPoolAsync(Stream source, Stream destination)
        {
            source.Seek(0, SeekOrigin.Begin);
            var pool = ArrayPool<byte>.Shared;
            byte[] buffer = pool.Rent(DefaultBufferSize);
            try
            {
                int bytesRead;
                while ((bytesRead = await source.ReadAsync(buffer.AsMemory(0, buffer.Length))) > 0)
                {
                    await destination.WriteAsync(buffer.AsMemory(0, bytesRead));
                }
            }
            finally
            {
                pool.Return(buffer);
            }
        }

        private static string GenerateCacheKeyFromRequest(HttpRequest request)
        {
            var method = request.Method.ToUpperInvariant();
            var path = request.Path.Value?.ToLowerInvariant() ?? string.Empty;
            var queryString = request.QueryString.HasValue ? request.QueryString.Value : string.Empty;

            return $"{method}:{path}{queryString}";
        }
    }
}
