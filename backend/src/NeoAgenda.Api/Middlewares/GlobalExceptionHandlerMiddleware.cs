using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using NeoAgenda.Domain.Common;

namespace NeoAgenda.Api.Middlewares;

public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

    public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning("Violação de regra de negócio: {Message}", ex.Message);

            // Se for duplicidade ou prazo expirado, usa status correspondente
            int statusCode = (int)HttpStatusCode.BadRequest;
            if (ex.Message.Contains("Já existe", StringComparison.OrdinalIgnoreCase) || 
                ex.Message.Contains("em uso", StringComparison.OrdinalIgnoreCase))
            {
                statusCode = (int)HttpStatusCode.Conflict; // 409
            }
            else if (ex.Message.Contains("expirou", StringComparison.OrdinalIgnoreCase))
            {
                statusCode = (int)HttpStatusCode.Gone; // 410
            }
            else if (ex.Message.Contains("não foi encontrado", StringComparison.OrdinalIgnoreCase))
            {
                statusCode = (int)HttpStatusCode.NotFound; // 404
            }

            var problemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = statusCode == 409 ? "Conflito de Regra de Negócio" : "Erro de Validação",
                Detail = ex.Message,
                Instance = context.Request.Path
            };

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro interno não tratado no servidor.");

            var problemDetails = new ProblemDetails
            {
                Status = (int)HttpStatusCode.InternalServerError,
                Title = "Erro Interno no Servidor",
                Detail = "Ocorreu uma falha inesperada ao processar sua requisição. Tente novamente mais tarde.",
                Instance = context.Request.Path
            };

            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails));
        }
    }
}
