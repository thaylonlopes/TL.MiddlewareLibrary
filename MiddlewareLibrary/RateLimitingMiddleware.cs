using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MiddlewareLibrary.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MiddlewareLibrary.Middlewares
{
    /// <summary>
    /// Middleware responsável por limitar a taxa de requisições por endereço IP em janelas fixas de tempo.
    /// </summary>
    public class RateLimitingMiddleware : IDisposable
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RateLimitingMiddleware> _logger;
        private readonly Dictionary<string, int> _requestCounts = new();
        private readonly int _limit;
        private readonly TimeSpan _period;
        private readonly Timer? _timer;
        private bool _disposed;

        /// <summary>
        /// Inicializa uma nova instância de <see cref="RateLimitingMiddleware"/>.
        /// </summary>
        /// <param name="next">Próximo delegado no pipeline HTTP.</param>
        /// <param name="logger">Instância de logging.</param>
        /// <param name="limit">Número máximo de requisições permitidas na janela.</param>
        /// <param name="period">Duração da janela de tempo.</param>
        public RateLimitingMiddleware(
            RequestDelegate next,
            ILogger<RateLimitingMiddleware> logger,
            int limit,
            TimeSpan period)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            if (limit <= 0)
                throw new ArgumentOutOfRangeException(nameof(limit), "O limite de requisições deve ser maior que zero.");

            if (period <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(period), "O período deve ser maior que zero.");

            _limit = limit;
            _period = period;

            _timer = new Timer(ResetRequestCounts, null, _period, _period);
        }

        private void ResetRequestCounts(object? state)
        {
            lock (_requestCounts)
            {
                _requestCounts.Clear();
            }
        }

        /// <summary>
        /// Executa a verificação e controle de taxa de requisições.
        /// </summary>
        /// <param name="context">Contexto HTTP da requisição.</param>
        public async Task InvokeAsync(HttpContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            var clientIp = context.Connection.RemoteIpAddress?.ToString()
                           ?? context.Request.Headers["X-Forwarded-For"].FirstOrDefault()
                           ?? "unknown_client";

            var allowRequest = true;

            lock (_requestCounts)
            {
                if (_requestCounts.TryGetValue(clientIp, out var count))
                {
                    if (count >= _limit)
                    {
                        allowRequest = false;
                    }
                    else
                    {
                        _requestCounts[clientIp] = count + 1;
                    }
                }
                else
                {
                    _requestCounts[clientIp] = 1;
                }
            }

            if (allowRequest)
            {
                await _next(context);
                return;
            }

            _logger.LogWarning("Limite de requisições excedido para o identificador/IP: {ClientIp} ({Limit} reqs / {Period})", clientIp, _limit, _period);

            if (context.Response.HasStarted)
            {
                _logger.LogWarning("A resposta HTTP já foi iniciada; não é possível reescrever os cabeçalhos para erro 429.");
                return;
            }

            context.Response.Clear();
            context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            context.Response.ContentType = "application/problem+json";

            var traceId = context.TraceIdentifier ?? Guid.NewGuid().ToString();
            var problemDetails = ProblemDetailsResponse.Create(
                StatusCodes.Status429TooManyRequests,
                "Too Many Requests",
                $"O limite de {_limit} requisições no período de {_period.TotalSeconds} segundos foi excedido. Tente novamente mais tarde.",
                context.Request.Path.Value,
                traceId
            );

            var result = JsonSerializer.Serialize(problemDetails);
            await context.Response.WriteAsync(result);
        }

        /// <summary>
        /// Libera os recursos gerenciados pelo middleware (Timer).
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Liberação protegida de recursos.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _timer?.Dispose();
                }

                _disposed = true;
            }
        }
    }
}
