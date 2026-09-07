using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MiddlewareLibrary.Middlewares;
using MiddlewareLibrary.Models;
using Moq;
using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace MiddlewareLibrary.Tests
{
    public class ExceptionHandlingMiddlewareTests
    {
        private readonly Mock<ILogger<ExceptionHandlingMiddleware>> _loggerMock = new();

        [Fact]
        public async Task InvokeAsync_WhenUnhandledExceptionOccurs_ShouldCatchAndReturn500ProblemDetails()
        {
            var context = new DefaultHttpContext();
            context.TraceIdentifier = "trace-catch-all";
            context.Request.Path = "/api/crash";
            var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            RequestDelegate next = _ => throw new ApplicationException("Erro catastrófico não tratado.");
            var middleware = new ExceptionHandlingMiddleware(next, _loggerMock.Object);

            await middleware.InvokeAsync(context);

            Assert.Equal(500, context.Response.StatusCode);
            Assert.Equal("application/problem+json", context.Response.ContentType);

            responseBody.Seek(0, SeekOrigin.Begin);
            var json = await new StreamReader(responseBody, Encoding.UTF8).ReadToEndAsync();
            var problemDetails = JsonSerializer.Deserialize<ProblemDetailsResponse>(json);

            Assert.NotNull(problemDetails);
            Assert.Equal(500, problemDetails.Status);
            Assert.Equal("Internal Server Error", problemDetails.Title);
            Assert.Equal("Ocorreu um erro interno inesperado ao processar a solicitação.", problemDetails.Detail);
            Assert.Equal("trace-catch-all", problemDetails.TraceId);
        }

        [Fact]
        public async Task InvokeAsync_WhenNoExceptionOccurs_ShouldExecuteDownstreamDelegateSuccessfully()
        {
            var context = new DefaultHttpContext();
            var executed = false;

            RequestDelegate next = _ =>
            {
                executed = true;
                return Task.CompletedTask;
            };

            var middleware = new ExceptionHandlingMiddleware(next, _loggerMock.Object);

            await middleware.InvokeAsync(context);

            Assert.True(executed);
        }
    }
}

