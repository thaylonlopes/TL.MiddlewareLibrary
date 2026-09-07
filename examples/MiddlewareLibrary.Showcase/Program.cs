using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MiddlewareLibrary.Exceptions;
using MiddlewareLibrary.Extensions;
using System;
using System.Threading.Tasks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "TL.MiddlewareLibrary Showcase API",
        Version = "v1",
        Description = "Showcase interativo demonstrando os 6 middlewares HTTP da biblioteca em execucao com ProblemDetails RFC 7807, Rate Limiting, Caching e Zero-Allocation Timing."
    });
});

builder.Services.AddMemoryCache();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "TL.MiddlewareLibrary Showcase v1");
    c.RoutePrefix = string.Empty;
});

app.UseRequestTiming();
app.UseExceptionHandling();
app.UseStatusCodeMiddleware();
app.UseWhen(
    context => context.Request.Path.StartsWithSegments("/api/demo/auth-protected"),
    branch => branch.UseAuthenticationMiddleware());
app.UseRateLimiting(limit: 5, period: TimeSpan.FromSeconds(30));
app.UseCachingMiddleware(cacheDuration: TimeSpan.FromSeconds(30));

MapShowcaseEndpoints(app);

app.Run();

static void MapShowcaseEndpoints(WebApplication app)
{
    var group = app.MapGroup("/api/demo").WithTags("Showcase Middlewares");

    group.MapGet("/ok", () => Results.Ok(new
    {
        Status = "Sucesso",
        Message = "Requisicao concluida com sucesso. Verifique o cabecalho X-Response-Time-Ms na resposta HTTP.",
        TimestampUtc = DateTimeOffset.UtcNow
    }))
    .WithName("GetOk")
    .WithSummary("Demonstra execucao bem-sucedida com medicao de tempo no cabecalho HTTP.");

    group.MapGet("/cache", (string? categoria) =>
    {
        var categoryFilter = string.IsNullOrWhiteSpace(categoria) ? "padrao" : categoria;
        return Results.Ok(new
        {
            Mensagem = "Resposta cacheada em memoria por 30 segundos com suporte a QueryString.",
            Categoria = categoryFilter,
            GeradoEmUtc = DateTimeOffset.UtcNow
        });
    })
    .WithName("GetCache")
    .WithSummary("Demonstra o CachingMiddleware em memoria (respostas GET idempotentes com variacao por QueryString).");

    group.MapGet("/rate-limit", () => Results.Ok(new
    {
        Mensagem = "Requisicao permitida pela politica de Rate Limiting (limite: 5 chamadas por 30 segundos).",
        TimestampUtc = DateTimeOffset.UtcNow
    }))
    .WithName("GetRateLimit")
    .WithSummary("Demonstra o RateLimitingMiddleware (dispare mais de 5 chamadas seguidas para observar o status 429).");

    group.MapGet("/auth-protected", () => Results.Ok(new
    {
        Mensagem = "Acesso autorizado. O cabecalho 'Authorization: Bearer <token>' foi validado com sucesso.",
        TimestampUtc = DateTimeOffset.UtcNow
    }))
    .WithName("GetAuthProtected")
    .WithSummary("Demonstra o AuthenticationMiddleware (requer Authorization: Bearer <token>).");

    group.MapGet("/exceptions/{tipo}", (string tipo) =>
    {
        SimulateBusinessException(tipo);
        return Results.Ok(new { Mensagem = "Nenhuma excecao foi disparada." });
    })
    .WithName("GetSimulatedException")
    .WithSummary("Demonstra a traducao automatica de excecoes do catalogo para ProblemDetails RFC 7807.");
}

static void SimulateBusinessException(string tipo)
{
    switch (tipo.ToLowerInvariant())
    {
        case "notfound":
            throw new NotFoundException("O registro solicitado nao foi localizado na base de dados.");
        case "badrequest":
            throw new BadRequestException("O formato dos parametros enviados e invalido.");
        case "conflict":
            throw new ConflictException("Ja existe um registro ativo com este mesmo identificador.");
        case "unauthorized":
            throw new UnauthorizedException("Credenciais invalidas ou sessao expirada.");
        case "forbidden":
            throw new ForbiddenException("O usuario autenticado nao possui privilegios para este recurso.");
        case "unhandled":
            throw new InvalidOperationException("Falha imprevista no servidor disparada para testar o middleware catch-all global.");
        default:
            break;
    }
}
