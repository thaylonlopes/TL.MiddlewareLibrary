# TL.MiddlewareLibrary

Biblioteca de middlewares para ASP.NET Core (.NET 6, .NET 8 e .NET 9). Centraliza tratamento de erros com `ProblemDetails`, rate limiting por IP, cache em memória e medição de latência, com configuração simples e sem dependências externas.

---

## Por que usar?

Em muitas APIs, acabamos repetindo o mesmo código básico em todo projeto:
- Controladores cheios de blocos `try/catch` manuais.
- Cada endpoint ou microsserviço retornando erros em formatos JSON diferentes (`{ erro }`, `{ message }`, `{ error_description }`).
- Erros 500 vazando stack trace ou mensagens internas para o cliente.
- Falta de controle de requisições por IP e medição de tempo de resposta.

A **TL.MiddlewareLibrary** resolve isso direto no pipeline do ASP.NET Core, deixando os controllers focados apenas na regra de negócio.

---

### Antes vs. Depois

#### Antes (Sem a biblioteca)
```csharp
app.MapGet("/api/produtos/{id:int}", async (int id, IProdutoService service, ILogger<Program> logger) =>
{
    try
    {
        if (id <= 0)
            return Results.BadRequest(new { mensagem = "Id inválido" });

        var produto = await service.ObterPorIdAsync(id);
        if (produto == null)
            return Results.NotFound(new { erro = "Não encontrado" });

        return Results.Ok(produto);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Erro ao buscar produto");
        return Results.StatusCode(500, new { erro = ex.Message, stack = ex.StackTrace }); // Vaza stack trace
    }
});
```

#### Depois (Com a TL.MiddlewareLibrary)
```csharp
app.MapGet("/api/produtos/{id:int}", async (int id, IProdutoService service) =>
{
    if (id <= 0)
        throw new BadRequestException("O id do produto deve ser maior que zero.");

    var produto = await service.ObterPorIdAsync(id) 
        ?? throw new NotFoundException($"Produto {id} não encontrado.");

    return Results.Ok(produto);
});
```
> O middleware intercepta a exceção, retorna o status code correto (ex: 400, 404), formata o JSON com `ProblemDetails` e adiciona o `traceId` automaticamente.

---

## Recursos

- **Menos código repetitivo**: Lance exceções de negócio direto nos serviços sem precisar de `try/catch` nos controllers.
- **Respostas de erro padronizadas**: Retorno consistente em `ProblemDetails` (`application/problem+json`) com `traceId` para facilitar a busca em logs.
- **Proteção em erros 500**: Erros não esperados têm suas mensagens técnicas mascaradas para não vazar detalhes de infraestrutura.
- **Tempo de resposta**: Injeta o header `X-Response-Time-Ms` com a duração da requisição em milissegundos.
- **Rate Limiting por IP**: Controle simples de requisições por cliente com limpeza automática de memória.
- **Cache em memória**: Cache transparente para consultas `GET` e `HEAD` (200 OK), isolado por QueryString.
- **Multi-Targeting**: Suporte nativo para **.NET 6.0**, **.NET 8.0** e **.NET 9.0**.

---

## Instalação

```bash
dotnet add package TL.MiddlewareLibrary --version 0.0.1-beta.2
```

---

## Como configurar no `Program.cs`

```csharp
using MiddlewareLibrary.Exceptions;
using MiddlewareLibrary.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Necessário para o middleware de cache
builder.Services.AddMemoryCache();

var app = builder.Build();

// Pipeline de middlewares (ordem recomendada)
app.UseRequestTiming();                                              // 1. Injeta header X-Response-Time-Ms
app.UseExceptionHandling();                                          // 2. Fallback para erros 500 não tratados
app.UseStatusCodeMiddleware();                                       // 3. Mapeia exceções de negócio para ProblemDetails
app.UseRateLimiting(limit: 100, period: TimeSpan.FromMinutes(1));    // 4. Limite de requisições por IP
app.UseAuthenticationMiddleware();                                   // 5. Validação de header Authorization
app.UseCachingMiddleware(cacheDuration: TimeSpan.FromMinutes(2));    // 6. Cache em memória para GET/HEAD

// Endpoints da aplicação
app.MapGet("/api/produtos/{id:int}", (int id) =>
{
    if (id <= 0)
        throw new BadRequestException("O id deve ser positivo.");

    return Results.Ok(new { Id = id, Nome = "Notebook", Preco = 5000.00 });
});

app.Run();
```

---

## Exceções Disponíveis

Você pode lançar qualquer uma dessas exceções em qualquer camada da aplicação (serviços, repositórios ou controllers):

| Exceção | Status HTTP | Título |
| :--- | :---: | :--- |
| `BadRequestException` | 400 | Bad Request |
| `UnauthorizedException` | 401 | Unauthorized |
| `ForbiddenException` | 403 | Forbidden |
| `NotFoundException` | 404 | Not Found |
| `NotAcceptableException` | 406 | Not Acceptable |
| `ConflictException` | 409 | Conflict |
| `UnsupportedMediaTypeException` | 415 | Unsupported Media Type |
| `LockedException` | 423 | Locked |
| `TooManyRequestsException` | 429 | Too Many Requests |
| `BadGatewayException` | 502 | Bad Gateway |
| `GatewayTimeoutException` | 504 | Gateway Timeout |
| `InsufficientStorageException` | 507 | Insufficient Storage |

---

## Testes

Para rodar os testes unitários em todos os targets (.NET 6, .NET 8 e .NET 9):

```bash
dotnet test
```

---

## Licença

MIT