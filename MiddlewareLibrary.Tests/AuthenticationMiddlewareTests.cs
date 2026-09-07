using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MiddlewareLibrary.Middlewares;
using MiddlewareLibrary.Models;
using Moq;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace MiddlewareLibrary.Tests
{
    public class AuthenticationMiddlewareTests
    {
        private readonly Mock<ILogger<AuthenticationMiddleware>> _loggerMock = new();

        [Fact]
        public async Task InvokeAsync_WhenAuthorizationHeaderIsMissing_ShouldReturn401ProblemDetails()
        {
            var context = new DefaultHttpContext();
            context.TraceIdentifier = "auth-trace-1";
            context.Request.Path = "/api/seguro";
            var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            RequestDelegate next = _ => Task.CompletedTask;
            var middleware = new AuthenticationMiddleware(next, _loggerMock.Object);

            await middleware.InvokeAsync(context);

            Assert.Equal(401, context.Response.StatusCode);
            Assert.Equal("application/problem+json", context.Response.ContentType);

            responseBody.Seek(0, SeekOrigin.Begin);
            var json = await new StreamReader(responseBody, Encoding.UTF8).ReadToEndAsync();
            var problemDetails = JsonSerializer.Deserialize<ProblemDetailsResponse>(json);

            Assert.NotNull(problemDetails);
            Assert.Equal(401, problemDetails.Status);
            Assert.Equal("Unauthorized", problemDetails.Title);
            Assert.Contains("obrigatório", problemDetails.Detail);
        }

        [Fact]
        public async Task InvokeAsync_WhenValidBearerTokenProvided_ShouldPassToNextMiddleware()
        {
            var context = new DefaultHttpContext();
            context.Request.Headers["Authorization"] = "Bearer valid_jwt_token_sample";

            var executed = false;
            RequestDelegate next = _ =>
            {
                executed = true;
                return Task.CompletedTask;
            };

            var middleware = new AuthenticationMiddleware(next, _loggerMock.Object);

            await middleware.InvokeAsync(context);

            Assert.True(executed);
        }
    }
}

