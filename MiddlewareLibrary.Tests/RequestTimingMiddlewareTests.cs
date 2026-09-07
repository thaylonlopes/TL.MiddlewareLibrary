using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MiddlewareLibrary.Middlewares;
using Moq;
using System.Threading.Tasks;
using Xunit;

using Microsoft.AspNetCore.Http.Features;

namespace MiddlewareLibrary.Tests
{
    public class RequestTimingMiddlewareTests
    {
        private readonly Mock<ILogger<RequestTimingMiddleware>> _loggerMock = new();

        [Fact]
        public async Task InvokeAsync_ShouldExecuteDownstreamAndInjectTimingHeader()
        {
            var context = new DefaultHttpContext();
            var responseFeature = new FakeResponseFeature();
            context.Features.Set<IHttpResponseFeature>(responseFeature);

            var executed = false;

            RequestDelegate next = async ctx =>
            {
                executed = true;
                await Task.Delay(10);
                await responseFeature.FireOnStartingAsync();
            };

            var middleware = new RequestTimingMiddleware(next, _loggerMock.Object);

            await middleware.InvokeAsync(context);

            Assert.True(executed);
            Assert.True(context.Response.Headers.ContainsKey("X-Response-Time-Ms"));
            var headerValue = context.Response.Headers["X-Response-Time-Ms"].ToString();
            Assert.True(long.TryParse(headerValue, out var elapsedMs));
            Assert.True(elapsedMs >= 0);
        }

        [Fact]
        public async Task InvokeAsync_WhenContextIsNull_ShouldThrowArgumentNullException()
        {
            var middleware = new RequestTimingMiddleware(_ => Task.CompletedTask, _loggerMock.Object);

            await Assert.ThrowsAsync<ArgumentNullException>(() => middleware.InvokeAsync(null!));
        }

        private sealed class FakeResponseFeature : IHttpResponseFeature
        {
            private readonly List<(Func<object, Task> Callback, object State)> _onStartingCallbacks = new();

            public int StatusCode { get; set; } = 200;
            public string? ReasonPhrase { get; set; }
            public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();
            public Stream Body { get; set; } = Stream.Null;
            public bool HasStarted => false;

            public void OnStarting(Func<object, Task> callback, object state)
            {
                _onStartingCallbacks.Add((callback, state));
            }

            public void OnCompleted(Func<object, Task> callback, object state)
            {
            }

            public async Task FireOnStartingAsync()
            {
                for (var i = _onStartingCallbacks.Count - 1; i >= 0; i--)
                {
                    var (callback, state) = _onStartingCallbacks[i];
                    await callback(state);
                }
            }
        }
    }
}

