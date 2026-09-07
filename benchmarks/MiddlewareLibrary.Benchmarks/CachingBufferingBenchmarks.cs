using BenchmarkDotNet.Attributes;
using System;
using System.Buffers;
using System.IO;
using System.Threading.Tasks;

namespace MiddlewareLibrary.Benchmarks
{
    [MemoryDiagnoser]
    public class CachingBufferingBenchmarks
    {
        private const int DefaultBufferSize = 4096;
        private byte[] _sourceData = Array.Empty<byte>();

        [Params(4096, 32768)]
        public int PayloadSize { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            _sourceData = new byte[PayloadSize];
            Random.Shared.NextBytes(_sourceData);
        }

        [Benchmark(Baseline = true, Description = "Stream.CopyToAsync (Heap Allocation)")]
        public async Task Tradicional_CopyToAsync()
        {
            using var source = new MemoryStream(_sourceData);
            using var destination = new MemoryStream();
            await source.CopyToAsync(destination);
        }

        [Benchmark(Description = "ArrayPool<byte>.Shared (Pooled Buffering)")]
        public async Task Otimizado_ArrayPoolBuffer()
        {
            using var source = new MemoryStream(_sourceData);
            using var destination = new MemoryStream();

            var pool = ArrayPool<byte>.Shared;
            byte[] buffer = pool.Rent(DefaultBufferSize);
            try
            {
                int bytesRead;
                while ((bytesRead = await source.ReadAsync(buffer.AsMemory(0, buffer.Length))) > 0)
                {
                    await destination.WriteAsync(buffer.AsMemory(0, bytesRead));
                }
            }
            finally
            {
                pool.Return(buffer);
            }
        }
    }
}
