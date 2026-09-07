using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using MiddlewareLibrary.Middlewares;
using Moq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MiddlewareLibrary.Tests;

public class CachingMiddlewareTests
{
    private readonly IMemoryCache _memoryCache = new MemoryCache(new MemoryCacheOptions());
    private readonly Mock<ILogger<CachingMiddleware>> _loggerMock = new();

    [Fact]
    public async Task InvokeAsync_WhenGetRequestWithQueryString_ShouldCacheAndReturnOnSecondCall()
    {
        var executionCount = 0;
        const string responsePayload = "{\"categoria\":\"eletronicos\",\"itens\":[1,2,3]}";

        RequestDelegate next = async ctx =>
        {
            executionCount++;
            ctx.Response.StatusCode = StatusCodes.Status200OK;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsync(responsePayload);
        };

        var middleware = new CachingMiddleware(next, _memoryCache, _loggerMock.Object);

        var context1 = CreateHttpContext("GET", "/api/produtos", "?categoria=eletronicos&pagina=1");
        var context2 = CreateHttpContext("GET", "/api/produtos", "?categoria=eletronicos&pagina=1");

        await middleware.InvokeAsync(context1);
        await middleware.InvokeAsync(context2);

        Assert.Equal(1, executionCount);
        Assert.Equal(200, context2.Response.StatusCode);

        context2.Response.Body.Seek(0, SeekOrigin.Begin);
        var cachedBody = await new StreamReader(context2.Response.Body, Encoding.UTF8).ReadToEndAsync();
        Assert.Equal(responsePayload, cachedBody);
    }

    [Fact]
    public async Task InvokeAsync_WhenDifferentQueryStringProvided_ShouldNotReturnOtherQueryCache()
    {
        var executionCount = 0;

        RequestDelegate next = async ctx =>
        {
            executionCount++;
            ctx.Response.StatusCode = StatusCodes.Status200OK;
            ctx.Response.ContentType = "application/json";
            var body = ctx.Request.Query["categoria"] == "eletronicos" ? "{\"tipo\":\"eletronicos\"}" : "{\"tipo\":\"livros\"}";
            await ctx.Response.WriteAsync(body);
        };

        var middleware = new CachingMiddleware(next, _memoryCache, _loggerMock.Object);

        var contextEletronicos = CreateHttpContext("GET", "/api/produtos", "?categoria=eletronicos");
        var contextLivros = CreateHttpContext("GET", "/api/produtos", "?categoria=livros");

        await middleware.InvokeAsync(contextEletronicos);
        await middleware.InvokeAsync(contextLivros);

        Assert.Equal(2, executionCount);

        contextLivros.Response.Body.Seek(0, SeekOrigin.Begin);
        var bodyLivros = await new StreamReader(contextLivros.Response.Body, Encoding.UTF8).ReadToEndAsync();
        Assert.Equal("{\"tipo\":\"livros\"}", bodyLivros);
    }

    [Fact]
    public async Task InvokeAsync_WhenPostMethodUsed_ShouldBypassCacheCompletely()
    {
        var executionCount = 0;

        RequestDelegate next = async ctx =>
        {
            executionCount++;
            ctx.Response.StatusCode = StatusCodes.Status201Created;
            await ctx.Response.WriteAsync("{\"id\":1}");
        };

        var middleware = new CachingMiddleware(next, _memoryCache, _loggerMock.Object);

        var context1 = CreateHttpContext("POST", "/api/pedidos");
        var context2 = CreateHttpContext("POST", "/api/pedidos");

        await middleware.InvokeAsync(context1);
        await middleware.InvokeAsync(context2);

        Assert.Equal(2, executionCount);
    }

    [Fact]
    public async Task InvokeAsync_WhenEndpointReturns500Error_ShouldNotCacheFailureResponse()
    {
        var executionCount = 0;

        RequestDelegate next = ctx =>
        {
            executionCount++;
            ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
            return Task.CompletedTask;
        };

        var middleware = new CachingMiddleware(next, _memoryCache, _loggerMock.Object);

        var context1 = CreateHttpContext("GET", "/api/falha");
        var context2 = CreateHttpContext("GET", "/api/falha");

        await middleware.InvokeAsync(context1);
        await middleware.InvokeAsync(context2);

        Assert.Equal(2, executionCount);
    }

    [Fact]
    public async Task InvokeAsync_WhenLargePayloadReturned_ShouldBufferAndCopyCorrectlyUsingArrayPool()
    {
        var largePayload = new string('A', 32 * 1024);
        RequestDelegate next = async ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status200OK;
            ctx.Response.ContentType = "text/plain";
            await ctx.Response.WriteAsync(largePayload);
        };

        var middleware = new CachingMiddleware(next, _memoryCache, _loggerMock.Object);
        var context = CreateHttpContext("GET", "/api/dados-pesados");

        await middleware.InvokeAsync(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(context.Response.Body, Encoding.UTF8).ReadToEndAsync();

        Assert.Equal(largePayload.Length, responseBody.Length);
        Assert.Equal(largePayload, responseBody);
    }

    private static DefaultHttpContext CreateHttpContext(string method, string path, string queryString = "")
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Path = path;
        context.Request.QueryString = new QueryString(queryString);
        context.Response.Body = new MemoryStream();
        return context;
    }
}
