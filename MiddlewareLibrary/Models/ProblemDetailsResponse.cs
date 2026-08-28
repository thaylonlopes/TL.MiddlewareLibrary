using System.Text.Json.Serialization;

namespace MiddlewareLibrary.Models
{
    /// <summary>
    /// Modelo imutável de resposta de erro padronizado (ProblemDetails).
    /// </summary>
    public sealed record ProblemDetailsResponse
    {
        /// <summary>
        /// URI de referência que identifica o tipo do problema.
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; init; }

        /// <summary>
        /// Resumo curto e legível por humanos do tipo de problema.
        /// </summary>
        [JsonPropertyName("title")]
        public string Title { get; init; }

        /// <summary>
        /// O código de status HTTP gerado pelo servidor de origem para esta ocorrência do problema.
        /// </summary>
        [JsonPropertyName("status")]
        public int Status { get; init; }

        /// <summary>
        /// Explicação detalhada legível por humanos específica para esta ocorrência do problema.
        /// </summary>
        [JsonPropertyName("detail")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Detail { get; init; }

        /// <summary>
        /// Referência URI que identifica a ocorrência específica do problema (ex: o caminho da requisição).
        /// </summary>
        [JsonPropertyName("instance")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Instance { get; init; }

        /// <summary>
        /// Identificador único de correlação e rastreabilidade da requisição (TraceIdentifier).
        /// </summary>
        [JsonPropertyName("traceId")]
        public string TraceId { get; init; }

        /// <summary>
        /// Construtor para inicialização do ProblemDetailsResponse.
        /// </summary>
        /// <param name="type">URI do tipo de erro.</param>
        /// <param name="title">Título do erro.</param>
        /// <param name="status">Código de status HTTP.</param>
        /// <param name="detail">Detalhe legível da falha.</param>
        /// <param name="instance">Caminho da requisição.</param>
        /// <param name="traceId">Identificador de rastreio.</param>
        public ProblemDetailsResponse(
            string type,
            string title,
            int status,
            string? detail,
            string? instance,
            string traceId)
        {
            Type = type ?? "https://tools.ietf.org/html/rfc9110#section-15.6.1";
            Title = title ?? "Internal Server Error";
            Status = status;
            Detail = detail;
            Instance = instance;
            TraceId = traceId ?? string.Empty;
        }

        /// <summary>
        /// Cria uma instância de ProblemDetailsResponse com base no status HTTP e contexto.
        /// </summary>
        public static ProblemDetailsResponse Create(
            int statusCode,
            string title,
            string? detail,
            string? instance,
            string traceId)
        {
            var typeUri = GetRfcTypeUri(statusCode);
            return new ProblemDetailsResponse(typeUri, title, statusCode, detail, instance, traceId);
        }

        private static string GetRfcTypeUri(int statusCode) => statusCode switch
        {
            400 => "https://tools.ietf.org/html/rfc9110#section-15.5.1",
            401 => "https://tools.ietf.org/html/rfc9110#section-15.5.2",
            403 => "https://tools.ietf.org/html/rfc9110#section-15.5.4",
            404 => "https://tools.ietf.org/html/rfc9110#section-15.5.5",
            406 => "https://tools.ietf.org/html/rfc9110#section-15.5.7",
            408 => "https://tools.ietf.org/html/rfc9110#section-15.5.9",
            409 => "https://tools.ietf.org/html/rfc9110#section-15.5.10",
            415 => "https://tools.ietf.org/html/rfc9110#section-15.5.16",
            423 => "https://tools.ietf.org/html/rfc4918#section-11.2",
            429 => "https://tools.ietf.org/html/rfc6585#section-4",
            502 => "https://tools.ietf.org/html/rfc9110#section-15.6.3",
            504 => "https://tools.ietf.org/html/rfc9110#section-15.6.5",
            507 => "https://tools.ietf.org/html/rfc4918#section-11.5",
            _ => "https://tools.ietf.org/html/rfc9110#section-15.6.1"
        };
    }
}

