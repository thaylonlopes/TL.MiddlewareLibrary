using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MiddlewareLibrary.Exceptions;
using MiddlewareLibrary.Models;
using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace MiddlewareLibrary.Middlewares
{
    /// <summary>
    /// Middleware responsável por interceptar exceções tipadas de negócio e traduzi-las em respostas de erro padronizadas (ProblemDetails).
    /// </summary>
    public class StatusCodeMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<StatusCodeMiddleware> _logger;

        /// <summary>
        /// Inicializa uma nova instância de <see cref="StatusCodeMiddleware"/>.
        /// </summary>
        /// <param name="next">Próximo delegado no pipeline HTTP.</param>
        /// <param name="logger">Instância de logging.</param>
        public StatusCodeMiddleware(RequestDelegate next, ILogger<StatusCodeMiddleware> logger)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Executa a interceptação assíncrona do pipeline HTTP.
        /// </summary>
        /// <param name="context">Contexto HTTP da requisição.</param>
        public async Task InvokeAsync(HttpContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var (statusCode, title) = exception switch
            {
                BadRequestException _ or ArgumentNullException _ or ArgumentException _ => (HttpStatusCode.BadRequest, "Bad Request"),
                UnauthorizedAccessException _ or UnauthorizedException _ => (HttpStatusCode.Unauthorized, "Unauthorized"),
                ForbiddenException _ => (HttpStatusCode.Forbidden, "Forbidden"),
                TimeoutException _ => (HttpStatusCode.RequestTimeout, "Request Timeout"),
                NotFoundException _ => (HttpStatusCode.NotFound, "Not Found"),
                NotAcceptableException _ => (HttpStatusCode.NotAcceptable, "Not Acceptable"),
                ConflictException _ => (HttpStatusCode.Conflict, "Conflict"),
                UnsupportedMediaTypeException _ => (HttpStatusCode.UnsupportedMediaType, "Unsupported Media Type"),
                LockedException _ => (HttpStatusCode.Locked, "Locked"),
                TooManyRequestsException _ => (HttpStatusCode.TooManyRequests, "Too Many Requests"),
                BadGatewayException _ => (HttpStatusCode.BadGateway, "Bad Gateway"),
                GatewayTimeoutException _ => (HttpStatusCode.GatewayTimeout, "Gateway Timeout"),
                InsufficientStorageException _ => (HttpStatusCode.InsufficientStorage, "Insufficient Storage"),
                _ => (HttpStatusCode.InternalServerError, "Internal Server Error")
            };

            _logger.LogError(exception, "Exceção capturada pelo StatusCodeMiddleware: {ExceptionMessage}", exception.Message);

            if (context.Response.HasStarted)
            {
                _logger.LogWarning("A resposta HTTP já foi iniciada; não é possível reescrever os cabeçalhos para o ProblemDetails.");
                return;
            }

            context.Response.Clear();
            context.Response.StatusCode = (int)statusCode;
            context.Response.ContentType = "application/problem+json";

            var traceId = context.TraceIdentifier ?? Guid.NewGuid().ToString();
            var detail = statusCode == HttpStatusCode.InternalServerError
                ? "Ocorreu um erro interno ao processar a solicitação."
                : exception.Message;

            var problemDetails = ProblemDetailsResponse.Create(
                (int)statusCode,
                title,
                detail,
                context.Request.Path.Value,
                traceId
            );

            var json = JsonSerializer.Serialize(problemDetails);
            await context.Response.WriteAsync(json);
        }
    }
}