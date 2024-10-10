﻿using Application.Contracts.Model;
using Application.Contracts.Services;
using Package.Infrastructure.AspNetCore;
using Package.Infrastructure.AspNetCore.Filters;
using AppConstants = Application.Contracts.Constants.Constants;

namespace SampleApp.Api.Endpoints;

public static class TodoItemEndpoints
{
    private static bool _problemDetailsIncludeStackTrace;

    public static void MapTodoItemEndpoints(this IEndpointRouteBuilder group, bool problemDetailsIncludeStackTrace)
    {
        _problemDetailsIncludeStackTrace = problemDetailsIncludeStackTrace;

        //openapidocs replace swagger

        //auth, version, aoutput cache, etc. can be applied to specific enpoints if needed
        group.MapGet("/", GetPage1).MapToApiVersion(1.0)
            .Produces<List<TodoItemDto>>().ProducesProblem(500);
        group.MapGet("/", GetPage1_1).MapToApiVersion(1.1)
            .Produces<List<TodoItemDto>>().ProducesProblem(500);
        group.MapGet("/{id:guid}", GetById)
            .Produces<TodoItemDto>().ProducesProblem(404).ProducesProblem(500);
        group.MapPost("/", Create).AddEndpointFilter<ValidationFilter<TodoItemDto>>()
            .Produces<TodoItemDto>().ProducesValidationProblem().ProducesProblem(500);
        group.MapPut("/{id:guid}", Update).AddEndpointFilter<ValidationFilter<TodoItemDto>>()
            .Produces<TodoItemDto>().ProducesValidationProblem().ProducesProblem(500);
        group.MapDelete("/{id:guid}", Delete)
            .Produces(204).ProducesValidationProblem().ProducesProblem(500);
    }

    private static async Task<IResult> GetPage1(ITodoService todoService, int pageSize = 10, int pageIndex = 1)
    {
        var items = await todoService.GetPageAsync(pageSize, pageIndex);
        return TypedResults.Ok(items);
    }

    private static async Task<IResult> GetPage1_1(ITodoService todoService, int pageSize = 20, int pageIndex = 1)
    {
        var items = await todoService.GetPageAsync(pageSize, pageIndex);
        return TypedResults.Ok(items);
    }

    private static async Task<IResult> GetById(ITodoService todoService, Guid id)
    {
        var option = await todoService.GetItemAsync(id);
        return option.Match(
            Some: dto => TypedResults.Ok(dto),
            None: () => ProblemDetailsHelper.BuildProblemDetailsResponse(message: $"Id '{id}' not found.", statusCodeOverride: StatusCodes.Status404NotFound));
    }

    private static async Task<IResult> Create(HttpContext httpContext, ITodoService todoService, TodoItemDto todoItemDto)
    {
        var result = await todoService.CreateItemAsync(todoItemDto);
        return result.Match(
            dto => TypedResults.Created(httpContext.Request.Path, dto),
            err => ProblemDetailsHelper.BuildProblemDetailsResponse(exception: err, traceId: httpContext.TraceIdentifier, includeStackTrace: _problemDetailsIncludeStackTrace)
            );
    }

    private static async Task<IResult> Update(HttpContext httpContext, ITodoService todoService, Guid id, TodoItemDto todoItem)
    {
        //validator?
        if (todoItem.Id != null && todoItem.Id != id)
        {
            return ProblemDetailsHelper.BuildProblemDetailsResponse(statusCodeOverride: StatusCodes.Status400BadRequest, message: $"{AppConstants.ERROR_URL_BODY_ID_MISMATCH}: {id} <> {todoItem.Id}");
        }

        var result = await todoService.UpdateItemAsync(todoItem);
        return result.Match(
            dto => dto is null ? Results.NotFound(id) : TypedResults.Ok(dto),
            err => ProblemDetailsHelper.BuildProblemDetailsResponse(exception: err, traceId: httpContext.TraceIdentifier, includeStackTrace: _problemDetailsIncludeStackTrace));
    }

    private static async Task<IResult> Delete(ITodoService todoService, Guid id)
    {
        await todoService.DeleteItemAsync(id);
        return Results.NoContent();
    }
}
