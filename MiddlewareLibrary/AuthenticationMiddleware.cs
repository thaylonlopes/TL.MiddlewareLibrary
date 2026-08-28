using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MiddlewareLibrary.Models;
using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace MiddlewareLibrary.Middlewares
{
    /// <summary>
    /// Middleware responsável por validar a presença e consistência prévia do cabeçalho de Authorization.
    /// </summary>
    public class AuthenticationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AuthenticationMiddleware> _logger;

        /// <summary>
        /// Inicializa uma nova instância de <see cref="AuthenticationMiddleware"/>.
        /// </summary>
        /// <param name="next">Próximo delegado no pipeline HTTP.</param>
        /// <param name="logger">Instância de logging.</param>
        public AuthenticationMiddleware(RequestDelegate next, ILogger<AuthenticationMiddleware> logger)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Executa a inspeção do cabeçalho Authorization na requisição.
        /// </summary>
        /// <param name="context">Contexto HTTP da requisição.</param>
        public async Task InvokeAsync(HttpContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            if (!context.Request.Headers.TryGetValue("Authorization", out var authHeader) || string.IsNullOrWhiteSpace(authHeader))
            {
                await WriteUnauthorizedResponseAsync(context, "O cabeçalho 'Authorization' é obrigatório.");
                return;
            }

            var token = authHeader.ToString();
            if (!IsValidToken(token))
            {
                await WriteUnauthorizedResponseAsync(context, "O token de autorização fornecido é inválido.");
                return;
            }

            await _next(context);
        }

        private async Task WriteUnauthorizedResponseAsync(HttpContext context, string detail)
        {
            _logger.LogWarning("Tentativa de acesso não autorizado: {Detail} no caminho {Path}", detail, context.Request.Path);

            if (context.Response.HasStarted)
            {
                _logger.LogWarning("A resposta HTTP já foi iniciada; não é possível reescrever os cabeçalhos para erro 401.");
                return;
            }

            context.Response.Clear();
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            context.Response.ContentType = "application/problem+json";

            var traceId = context.TraceIdentifier ?? Guid.NewGuid().ToString();
            var problemDetails = ProblemDetailsResponse.Create(
                StatusCodes.Status401Unauthorized,
                "Unauthorized",
                detail,
                context.Request.Path.Value,
                traceId
            );

            var json = JsonSerializer.Serialize(problemDetails);
            await context.Response.WriteAsync(json);
        }

        private static bool IsValidToken(string token)
        {
            return !string.IsNullOrWhiteSpace(token) && token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase);
        }
    }
}

