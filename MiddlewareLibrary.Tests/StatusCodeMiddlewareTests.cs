using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MiddlewareLibrary.Exceptions;
using MiddlewareLibrary.Middlewares;
using MiddlewareLibrary.Models;
using Moq;
using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace MiddlewareLibrary.Tests;

public class StatusCodeMiddlewareTests
{
    private readonly Mock<ILogger<StatusCodeMiddleware>> _loggerMock = new();

    [Fact]
    public async Task InvokeAsync_WhenDownstreamThrowsNotFoundException_ShouldReturn404ProblemDetails()
    {
        var context = new DefaultHttpContext();
        context.TraceIdentifier = "test-trace-123";
        context.Request.Path = "/api/produtos/99";
        var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        RequestDelegate next = _ => throw new NotFoundException("Produto não encontrado.");
        var middleware = new StatusCodeMiddleware(next, _loggerMock.Object);

        await middleware.InvokeAsync(context);

        Assert.Equal(404, context.Response.StatusCode);
        Assert.Equal("application/problem+json", context.Response.ContentType);

        responseBody.Seek(0, SeekOrigin.Begin);
        var json = await new StreamReader(responseBody, Encoding.UTF8).ReadToEndAsync();
        var problemDetails = JsonSerializer.Deserialize<ProblemDetailsResponse>(json);

        Assert.NotNull(problemDetails);
        Assert.Equal(404, problemDetails.Status);
        Assert.Equal("Not Found", problemDetails.Title);
        Assert.Equal("Produto não encontrado.", problemDetails.Detail);
        Assert.Equal("/api/produtos/99", problemDetails.Instance);
        Assert.Equal("test-trace-123", problemDetails.TraceId);
    }

    [Fact]
    public async Task InvokeAsync_WhenDownstreamThrowsBadRequestException_ShouldReturn400ProblemDetails()
    {
        var context = new DefaultHttpContext();
        context.TraceIdentifier = "bad-req-trace";
        context.Request.Path = "/api/clientes";
        var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        RequestDelegate next = _ => throw new BadRequestException("Payload inválido.");
        var middleware = new StatusCodeMiddleware(next, _loggerMock.Object);

        await middleware.InvokeAsync(context);

        Assert.Equal(400, context.Response.StatusCode);
        Assert.Equal("application/problem+json", context.Response.ContentType);

        responseBody.Seek(0, SeekOrigin.Begin);
        var json = await new StreamReader(responseBody, Encoding.UTF8).ReadToEndAsync();
        var problemDetails = JsonSerializer.Deserialize<ProblemDetailsResponse>(json);

        Assert.NotNull(problemDetails);
        Assert.Equal(400, problemDetails.Status);
        Assert.Equal("Bad Request", problemDetails.Title);
        Assert.Equal("Payload inválido.", problemDetails.Detail);
    }

    [Fact]
    public async Task InvokeAsync_WhenDownstreamSucceedsWith204_ShouldNotWriteResponseBody()
    {
        var context = new DefaultHttpContext();
        var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        RequestDelegate next = ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status204NoContent;
            return Task.CompletedTask;
        };

        var middleware = new StatusCodeMiddleware(next, _loggerMock.Object);

        await middleware.InvokeAsync(context);

        Assert.Equal(204, context.Response.StatusCode);
        Assert.Equal(0, responseBody.Length);
    }

    [Fact]
    public async Task InvokeAsync_WhenDownstreamSucceedsWith200AndData_ShouldPreserveOriginalPayload()
    {
        var context = new DefaultHttpContext();
        var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        const string expectedPayload = "{\"id\":10,\"nome\":\"Teste\"}";

        RequestDelegate next = async ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status200OK;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsync(expectedPayload);
        };

        var middleware = new StatusCodeMiddleware(next, _loggerMock.Object);

        await middleware.InvokeAsync(context);

        Assert.Equal(200, context.Response.StatusCode);
        responseBody.Seek(0, SeekOrigin.Begin);
        var actualPayload = await new StreamReader(responseBody, Encoding.UTF8).ReadToEndAsync();
        Assert.Equal(expectedPayload, actualPayload);
    }

    [Fact]
    public async Task InvokeAsync_WhenDownstreamThrowsGenericException_ShouldReturn500WithMaskedDetail()
    {
        var context = new DefaultHttpContext();
        context.TraceIdentifier = "trace-500";
        context.Request.Path = "/api/fatal";
        var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        RequestDelegate next = _ => throw new InvalidOperationException("Falha interna de infraestrutura com credenciais secretas");
        var middleware = new StatusCodeMiddleware(next, _loggerMock.Object);

        await middleware.InvokeAsync(context);

        Assert.Equal(500, context.Response.StatusCode);
        responseBody.Seek(0, SeekOrigin.Begin);
        var json = await new StreamReader(responseBody, Encoding.UTF8).ReadToEndAsync();
        var problemDetails = JsonSerializer.Deserialize<ProblemDetailsResponse>(json);

        Assert.NotNull(problemDetails);
        Assert.Equal(500, problemDetails.Status);
        Assert.Equal("Internal Server Error", problemDetails.Title);
        Assert.DoesNotContain("secretas", problemDetails.Detail);
        Assert.Equal("Ocorreu um erro interno ao processar a solicitação.", problemDetails.Detail);
        Assert.Equal("trace-500", problemDetails.TraceId);
    }

    [Theory]
    [InlineData(typeof(BadRequestException), 400, "Bad Request")]
    [InlineData(typeof(UnauthorizedException), 401, "Unauthorized")]
    [InlineData(typeof(ForbiddenException), 403, "Forbidden")]
    [InlineData(typeof(NotFoundException), 404, "Not Found")]
    [InlineData(typeof(NotAcceptableException), 406, "Not Acceptable")]
    [InlineData(typeof(ConflictException), 409, "Conflict")]
    [InlineData(typeof(UnsupportedMediaTypeException), 415, "Unsupported Media Type")]
    [InlineData(typeof(LockedException), 423, "Locked")]
    [InlineData(typeof(TooManyRequestsException), 429, "Too Many Requests")]
    [InlineData(typeof(BadGatewayException), 502, "Bad Gateway")]
    [InlineData(typeof(GatewayTimeoutException), 504, "Gateway Timeout")]
    [InlineData(typeof(InsufficientStorageException), 507, "Insufficient Storage")]
    public async Task InvokeAsync_WhenMappedExceptionThrown_ShouldReturnExactExpectedStatusCodeAndTitle(
        Type exceptionType,
        int expectedStatusCode,
        string expectedTitle)
    {
        var context = new DefaultHttpContext();
        context.TraceIdentifier = "theory-trace";
        context.Request.Path = "/api/test";
        var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        var exceptionInstance = (Exception)Activator.CreateInstance(exceptionType, "Erro de teste")!;
        RequestDelegate next = _ => throw exceptionInstance;
        var middleware = new StatusCodeMiddleware(next, _loggerMock.Object);

        await middleware.InvokeAsync(context);

        Assert.Equal(expectedStatusCode, context.Response.StatusCode);
        Assert.Equal("application/problem+json", context.Response.ContentType);

        responseBody.Seek(0, SeekOrigin.Begin);
        var json = await new StreamReader(responseBody, Encoding.UTF8).ReadToEndAsync();
        var problemDetails = JsonSerializer.Deserialize<ProblemDetailsResponse>(json);

        Assert.NotNull(problemDetails);
        Assert.Equal(expectedStatusCode, problemDetails.Status);
        Assert.Equal(expectedTitle, problemDetails.Title);
        Assert.Equal("Erro de teste", problemDetails.Detail);
    }
}
