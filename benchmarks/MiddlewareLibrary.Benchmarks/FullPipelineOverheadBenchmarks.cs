using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using MiddlewareLibrary.Middlewares;
using System.Threading.Tasks;

namespace MiddlewareLibrary.Benchmarks
{
    [MemoryDiagnoser]
    public class FullPipelineOverheadBenchmarks
    {
        private RequestTimingMiddleware _timingMiddleware = null!;
        private DefaultHttpContext _context = null!;

        [GlobalSetup]
        public void Setup()
        {
            _timingMiddleware = new RequestTimingMiddleware(
                _ => Task.CompletedTask,
                NullLogger<RequestTimingMiddleware>.Instance);

            _context = new DefaultHttpContext();
        }

        [Benchmark(Description = "RequestTimingMiddleware.InvokeAsync Overhead")]
        public async Task TimingMiddleware_InvokeAsync_Overhead()
        {
            await _timingMiddleware.InvokeAsync(_context);
        }
    }
}
