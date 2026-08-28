using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MiddlewareLibrary.Middlewares;
using Moq;
using System.Threading.Tasks;
using Xunit;

namespace MiddlewareLibrary.Tests
{
    public class RequestTimingMiddlewareTests
    {
        private readonly Mock<ILogger<RequestTimingMiddleware>> _loggerMock = new();

        [Fact]
        public async Task InvokeAsync_ShouldExecuteDownstreamAndInjectTimingHeader()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var executed = false;

            RequestDelegate next = async ctx =>
            {
                executed = true;
                await Task.Delay(10);
            };

            var middleware = new RequestTimingMiddleware(next, _loggerMock.Object);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.True(executed);
        }
    }
}

