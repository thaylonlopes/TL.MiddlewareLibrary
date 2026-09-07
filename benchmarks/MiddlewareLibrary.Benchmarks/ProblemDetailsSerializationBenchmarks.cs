using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http;
using MiddlewareLibrary.Exceptions;
using MiddlewareLibrary.Models;
using System;
using System.Net;
using System.Text.Json;

namespace MiddlewareLibrary.Benchmarks
{
    [MemoryDiagnoser]
    public class ProblemDetailsSerializationBenchmarks
    {
        private ProblemDetailsResponse _sampleProblemDetails = null!;
        private readonly Exception _notFoundException = new NotFoundException("Cliente 100 não encontrado.");

        [GlobalSetup]
        public void Setup()
        {
            _sampleProblemDetails = ProblemDetailsResponse.Create(
                StatusCodes.Status404NotFound,
                "Not Found",
                "O registro solicitado não foi localizado na base de dados.",
                "/api/clientes/100",
                "0HNOCGST030NU:00000001");
        }

        [Benchmark(Description = "ProblemDetails JSON Serialization")]
        public string ProblemDetails_Serialize()
        {
            return JsonSerializer.Serialize(_sampleProblemDetails);
        }

        [Benchmark(Description = "StatusCode Exception Pattern Matching (12 Types)")]
        public (HttpStatusCode StatusCode, string Title) Exception_PatternMatching()
        {
            return _notFoundException switch
            {
                BadRequestException _ or ArgumentNullException _ or ArgumentException _ => (HttpStatusCode.BadRequest, "Bad Request"),
                UnauthorizedAccessException _ or UnauthorizedException _ => (HttpStatusCode.Unauthorized, "Unauthorized"),
                ForbiddenException _ => (HttpStatusCode.Forbidden, "Forbidden"),
                TimeoutException _ => (HttpStatusCode.RequestTimeout, "Request Timeout"),
                NotFoundException _ => (HttpStatusCode.NotFound, "Not Found"),
                NotAcceptableException _ => (HttpStatusCode.NotAcceptable, "Not Acceptable"),
                ConflictException _ => (HttpStatusCode.Conflict, "Conflict"),
                UnsupportedMediaTypeException _ => (HttpStatusCode.UnsupportedMediaType, "Unsupported Media Type"),
                LockedException _ => (HttpStatusCode.Locked, "Locked"),
                TooManyRequestsException _ => (HttpStatusCode.TooManyRequests, "Too Many Requests"),
                BadGatewayException _ => (HttpStatusCode.BadGateway, "Bad Gateway"),
                GatewayTimeoutException _ => (HttpStatusCode.GatewayTimeout, "Gateway Timeout"),
                InsufficientStorageException _ => (HttpStatusCode.InsufficientStorage, "Insufficient Storage"),
                _ => (HttpStatusCode.InternalServerError, "Internal Server Error")
            };
        }
    }
}
