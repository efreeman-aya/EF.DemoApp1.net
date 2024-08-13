﻿using Application.Contracts.Model;
using Application.Contracts.Services;
using Package.Infrastructure.AspNetCore;
using Package.Infrastructure.AspNetCore.Filters;
using AppConstants = Application.Contracts.Constants.Constants;

namespace SampleApp.Api.Endpoints;

public static class TodoItemEndpoints
{
    private const string _apiRoute = "api/todoitems";
    private static IWebHostEnvironment? _env;

    public static void MapTodoItemEndpoints(this WebApplication app)
    {
        _env = app.Environment;

        //auth
        //openapidocs replace swagger
        //api versioning
        //hybridcache
        //validation https://www.youtube.com/watch?v=Kt9TiXrwIp4

        var group = app.MapGroup(_apiRoute); //.RequireAuthorization("policy1", "policy2");
        group.MapGet("/", GetPage);
        group.MapGet("/{id:guid}", GetById);
        group.MapPost("/", Create).AddEndpointFilter<ValidationFilter<TodoItemDto>>();
        group.MapPut("/{id:guid}", Update).AddEndpointFilter<ValidationFilter<TodoItemDto>>();
        group.MapDelete("/{id:guid}", Delete);
    }

    private static async Task<IResult> GetPage(ITodoService todoService, int pageSize = 10, int pageIndex = 1)
    {
        var items = await todoService.GetPageAsync(pageSize, pageIndex);
        return TypedResults.Ok(items);
    }

    private static async Task<IResult> GetById(ITodoService todoService, Guid id)
    {
        var option = await todoService.GetItemAsync(id);
        return option.Match(
            Some: dto => TypedResults.Ok(dto),
            None: () => ProblemDetailsHelper.BuildProblemDetailsResponse(message: $"Id '{id.ToString()}' not found.", statusCodeOverride: StatusCodes.Status404NotFound));
    }

    private static async Task<IResult> Create(HttpContext httpContext, ITodoService todoService, TodoItemDto todoItemDto)
    {
        var result = await todoService.CreateItemAsync(todoItemDto);
        return result.Match(
            dto => TypedResults.Created(httpContext.Request.Path, dto),
            err => ProblemDetailsHelper.BuildProblemDetailsResponse(exception: err, traceId: httpContext.TraceIdentifier, includeStackTrace: _env?.IsDevelopment() ?? false)
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
            err => ProblemDetailsHelper.BuildProblemDetailsResponse(exception: err, traceId: httpContext.TraceIdentifier, includeStackTrace: _env?.IsDevelopment() ?? false));
    }

    private static async Task<IResult> Delete(ITodoService todoService, Guid id)
    {
        await todoService.DeleteItemAsync(id);
        return Results.NoContent();
    }
}
