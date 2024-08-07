﻿using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Package.Infrastructure.Common.Extensions;

namespace SampleApp.Api.ExceptionHandlers;

/// <summary>
/// Singleton
/// https://anthonygiretti.com/2023/06/14/asp-net-core-8-improved-exception-handling-with-iexceptionhandler/
/// https://www.milanjovanovic.tech/blog/global-error-handling-in-aspnetcore-8
/// </summary>
/// <param name="logger"></param>
/// <param name="hostEnvironment"></param>
public sealed class DefaultExceptionHandler(ILogger<DefaultExceptionHandler> logger, IHostEnvironment hostEnvironment)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        try
        {
            logger.ErrorLog("DefaultExceptionHandler caught exception", exception);
        }
        catch (Exception exInternal)
        {
            try
            {
                logger.ErrorLog("DefaultExceptionHandler internal exception when attempting to log an application exception", exInternal);
            }
            catch
            {
                //internal logging error; throw the original
                throw exception;
            }
        }

        httpContext.Response.StatusCode = exception.GetType().Name switch
        {
            "ValidationException" => StatusCodes.Status400BadRequest,
            "NotFoundException" => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status500InternalServerError
        };

        var problemDetails = new ProblemDetails
        {
            Type = exception.GetType().Name,
            Title = "Error occurred",
            Detail = exception.Message,
            Status = httpContext.Response.StatusCode
        };
        problemDetails.Extensions.Add("traceidentifier", httpContext.TraceIdentifier);
        if (hostEnvironment.IsDevelopment())
        {
            problemDetails.Extensions.Add("stacktrace", exception.StackTrace);
        }

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}

