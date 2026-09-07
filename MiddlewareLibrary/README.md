# 🚀 TL.MiddlewareLibrary (Core Package)

[![.NET](https://img.shields.io/badge/.NET-net6.0%20%7C%20net8.0%20%7C%20net9.0-blue.svg)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](../LICENSE.txt)

> **Modular ASP.NET Core middleware suite: latency diagnostics, IP-based rate limiting, in-memory caching, and standardized ProblemDetails error responses.**  
> *Suíte modular de middlewares para ASP.NET Core: diagnóstico de latência, controle de taxa por IP, cache em memória e respostas padronizadas de erro com ProblemDetails.*

O **`TL.MiddlewareLibrary`** é uma biblioteca de infraestrutura transversal para o pipeline HTTP do ASP.NET Core (.NET 6, .NET 8 e .NET 9), concebida para simplificar o desenvolvimento de APIs modernas, eliminar código defensivo repetitivo em controllers e proteger a aplicação contra sobrecargas.

---

## 📦 Instalação

Adicione o pacote via .NET CLI:

```bash
dotnet add package TL.MiddlewareLibrary
```

---

## 🚀 Métodos de Extensão do Pipeline

| Extensão | Middleware | Descrição Resumida |
| :--- | :--- | :--- |
| `app.UseRequestTiming()` | `RequestTimingMiddleware` | Mede a latência da requisição com zero alocação e injeta o header `X-Response-Time-Ms`. |
| `app.UseRateLimiting(limit, period)` | `RateLimitingMiddleware` | Protege a API contra rajadas excessivas de chamadas por IP, retornando HTTP 429. |
| `app.UseAuthenticationMiddleware()` | `AuthenticationMiddleware` | Validação ágil de cabeçalho `Authorization` no início do fluxo. |
| `app.UseCachingMiddleware(duration)` | `CachingMiddleware` | Cache em memória transparente para requisições `GET` e `HEAD` (200 OK) com chave composta. |
| `app.UseExceptionHandling()` | `ExceptionHandlingMiddleware` | Captura global de exceções não tratadas, gerando resposta amigável com `traceId`. |
| `app.UseStatusCodeMiddleware()` | `StatusCodeMiddleware` | Mapeia exceções de domínio tipadas e assegura que respostas 204 não tenham corpo. |

---

## 💡 Exemplo Rápido de Uso

```csharp
using MiddlewareLibrary.Exceptions;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMemoryCache();

var app = builder.Build();

app.UseRequestTiming();
app.UseRateLimiting(100, TimeSpan.FromMinutes(1));
app.UseAuthenticationMiddleware();
app.UseCachingMiddleware(TimeSpan.FromMinutes(2));
app.UseExceptionHandling();
app.UseStatusCodeMiddleware();

app.MapGet("/api/clientes/{id}", (int id) =>
{
    if (id <= 0)
        throw new BadRequestException("O ID informado é inválido.");

    return Results.Ok(new { id, nome = "Cliente Exemplo" });
});

app.Run();
```

---

## 🏛️ Decisões Arquiteturais e Referências

Para detalhes aprofundados sobre design, thread-safety e invariantes do pipeline:
- 📄 [ADR 001: Arquitetura, Convenções e Pipeline HTTP](../docs/adr/ADR-001-arquitetura-e-convencoes.md)
- 🗺️ [Visão Geral da Arquitetura & Modelo C4](../docs/arquitetura/visao-geral.md)

---

## 📄 Licença

Distribuído sob a licença [MIT](../LICENSE.txt).

