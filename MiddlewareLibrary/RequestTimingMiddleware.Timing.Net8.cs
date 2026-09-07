using System.Diagnostics;

namespace MiddlewareLibrary.Middlewares
{
    public partial class RequestTimingMiddleware
    {
        private static long GetElapsedMilliseconds(long startTimestamp)
        {
            return (long)Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;
        }
    }
}
