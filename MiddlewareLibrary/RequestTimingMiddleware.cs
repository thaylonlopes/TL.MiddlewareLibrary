using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace MiddlewareLibrary.Middlewares
{
    /// <summary>
    /// Middleware responsável por mensurar o tempo total de execução da requisição HTTP e injetar métricas de diagnóstico com zero alocação de heap.
    /// </summary>
    public partial class RequestTimingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestTimingMiddleware> _logger;

        /// <summary>
        /// Inicializa uma nova instância de <see cref="RequestTimingMiddleware"/>.
        /// </summary>
        /// <param name="next">Próximo delegado no pipeline HTTP.</param>
        /// <param name="logger">Instância de logging.</param>
        public RequestTimingMiddleware(RequestDelegate next, ILogger<RequestTimingMiddleware> logger)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Executa a medição de tempo de resposta da requisição com precisão de timestamp de alta resolução.
        /// </summary>
        /// <param name="context">Contexto HTTP da requisição.</param>
        public async Task InvokeAsync(HttpContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            long startTimestamp = Stopwatch.GetTimestamp();

            context.Response.OnStarting(() =>
            {
                if (!context.Response.Headers.ContainsKey("X-Response-Time-Ms"))
                {
                    long elapsedMilliseconds = GetElapsedMilliseconds(startTimestamp);
                    context.Response.Headers.Append("X-Response-Time-Ms", elapsedMilliseconds.ToString());
                }
                return Task.CompletedTask;
            });

            try
            {
                await _next(context);
            }
            finally
            {
                long elapsedMilliseconds = GetElapsedMilliseconds(startTimestamp);
                _logger.LogInformation(
                    "Requisição [{Method}] em {Path} concluída com status {StatusCode} em {ElapsedMilliseconds} ms",
                    context.Request.Method,
                    context.Request.Path,
                    context.Response.StatusCode,
                    elapsedMilliseconds);
            }
        }
    }
}
