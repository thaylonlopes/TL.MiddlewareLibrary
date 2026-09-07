# 🏛️ Registros de Decisões Arquiteturais (ADRs) — TL.MiddlewareLibrary

Bem-vindo ao diretório de decisões de arquitetura da **TL.MiddlewareLibrary**.

Este espaço documenta as escolhas técnicas fundamentais, padrões de design e trade-offs que orientam o desenvolvimento e a evolução contínua da biblioteca.

---

## 🗂️ Registro de Decisões do Projeto

Como a **TL.MiddlewareLibrary** é uma biblioteca coesa em um projeto único, consolidamos a arquitetura do pipeline, convenções e decisões técnicas em um documento unificado de referência:

| Identificador | Título da Decisão | Status | Data |
| :---: | :--- | :---: | :---: |
| **[ADR-001](./ADR-001-arquitetura-e-convencoes.md)** | **Arquitetura, Convenções e Pipeline HTTP da TL.MiddlewareLibrary** | ✅ Aceito | 2026-09-06 |

---

## 💡 Princípios das Decisões

1. **Foco na Produtividade:** Soluções simples e diretas que eliminam código repetitivo (como `try/catch` defensivo) no dia a dia.
2. **Performance sem Alocação Inútil:** Medições de latência e cache projetados para minimizar a pressão sobre o Garbage Collector.
3. **Previsibilidade para Consumidores:** Respostas de erro consistentes e transparentes para front-ends, aplicações mobile e integrações.

