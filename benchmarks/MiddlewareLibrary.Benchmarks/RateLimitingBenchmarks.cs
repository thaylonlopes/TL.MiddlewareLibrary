using BenchmarkDotNet.Attributes;
using System.Collections.Generic;

namespace MiddlewareLibrary.Benchmarks
{
    [MemoryDiagnoser]
    public class RateLimitingBenchmarks
    {
        private readonly Dictionary<string, int> _requestCounts = new();
        private const int Limit = 100000;

        [Benchmark(Description = "RateLimiting Lookup and Counter Increment")]
        public bool RateLimitLookupAndIncrement()
        {
            var clientIp = "192.168.1.100";
            lock (_requestCounts)
            {
                if (_requestCounts.TryGetValue(clientIp, out var count))
                {
                    if (count >= Limit)
                    {
                        return false;
                    }
                    _requestCounts[clientIp] = count + 1;
                    return true;
                }

                _requestCounts[clientIp] = 1;
                return true;
            }
        }
    }
}
