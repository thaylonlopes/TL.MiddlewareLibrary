using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http;

namespace MiddlewareLibrary.Benchmarks
{
    [MemoryDiagnoser]
    public class CacheKeyGenerationBenchmarks
    {
        private DefaultHttpContext _shortContext = null!;
        private DefaultHttpContext _complexContext = null!;

        [GlobalSetup]
        public void Setup()
        {
            _shortContext = new DefaultHttpContext();
            _shortContext.Request.Method = "GET";
            _shortContext.Request.Path = "/api/produtos";

            _complexContext = new DefaultHttpContext();
            _complexContext.Request.Method = "GET";
            _complexContext.Request.Path = "/api/produtos/filtrar";
            _complexContext.Request.QueryString = new QueryString("?categoria=eletronicos&ordenacao=preco_asc&pagina=2&limite=50");
        }

        [Benchmark(Baseline = true, Description = "Cache Key - Simple Route")]
        public string CacheKey_SimpleRoute()
        {
            return GenerateCacheKey(_shortContext.Request);
        }

        [Benchmark(Description = "Cache Key - Complex Route With QueryString")]
        public string CacheKey_ComplexQueryString()
        {
            return GenerateCacheKey(_complexContext.Request);
        }

        private static string GenerateCacheKey(HttpRequest request)
        {
            var method = request.Method.ToUpperInvariant();
            var path = request.Path.Value?.ToLowerInvariant() ?? string.Empty;
            var queryString = request.QueryString.HasValue ? request.QueryString.Value : string.Empty;

            return $"{method}:{path}{queryString}";
        }
    }
}
