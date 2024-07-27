﻿using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using LanguageExt.Common;
using Microsoft.Extensions.Logging;
using Package.Infrastructure.Common.Exceptions;
using Package.Infrastructure.Common.Extensions;
using SampleApp.Grpc.Mappers;
using SampleAppGrpc = SampleApp.Grpc.Proto;
using SampleAppModel = Application.Contracts.Model;

namespace SampleApp.Grpc;

//client cert auth only for this service class 
//[Authorize(AuthenticationSchemes = CertificateAuthenticationDefaults.AuthenticationScheme)]
//[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class TodoGrpcService(ILogger<TodoGrpcService> logger, Application.Contracts.Services.ITodoService todoService) : SampleAppGrpc.TodoService.TodoServiceBase
{
    public override async Task<SampleAppGrpc.ServiceResponsePageTodoItems> Page(SampleAppGrpc.ServiceRequestPage request, ServerCallContext context)
    {
        var page = await todoService.GetPageAsync(request.Pagesize, request.Pageindex);

        var response = new SampleAppGrpc.ServiceResponsePageTodoItems
        {
            ResponseCode = SampleAppGrpc.ResponseCode.Success,
            Data = new SampleAppGrpc.PagedResponseTodo
            {
                Pagesize = page.PageSize,
                Pageindex = page.PageIndex,
                Total = page.Total
            }
        };
        //grps repeated is 'readonly' so the collection can't be assigned, only added to
        foreach (var todo in page.Data)
        {
            response.Data.Data.Add(TodoItemMapper.ToGrpcDto(todo));
        }
        return response;
    }

    public override async Task<SampleAppGrpc.ServiceResponseTodoItem> Get(SampleAppGrpc.ServiceRequestId request, ServerCallContext context)
    {
        SampleAppModel.TodoItemDto? todo = await todoService.GetItemAsync(new Guid(request.Id));
        _ = todo ?? throw new NotFoundException($"TodoItem.Id:{request.Id} not found.");

        return new SampleAppGrpc.ServiceResponseTodoItem
        {
            ResponseCode = SampleAppGrpc.ResponseCode.Success,
            Data = TodoItemMapper.ToGrpcDto(todo)
        };
    }

    public override async Task<SampleAppGrpc.ServiceResponseTodoItem> Save(SampleAppGrpc.ServiceRequestTodoItem request, ServerCallContext context)
    {
        logger.Log(LogLevel.Information, "Save {TodoItemDto} - Start", request.Data.SerializeToJson());

        SampleAppModel.TodoItemDto? todo = request.Data.ToAppDto();

        //Save = update/insert
        Result<SampleAppModel.TodoItemDto?> result;

        if (todo == null)
        {
            result = new Result<SampleAppModel.TodoItemDto?>(new ArgumentNullException(nameof(request), "Request does not map to TodoItemDto"));
        }
        else
        {
            if (todo.Id == Guid.Empty)
                result = await todoService.AddItemAsync(todo);
            else
                result = await todoService.UpdateItemAsync(todo);

            todo = result.Match(
                dto => dto,
                err => null);
        }

        var response = new SampleAppGrpc.ServiceResponseTodoItem
        {
            ResponseCode = result.IsSuccess ? SampleAppGrpc.ResponseCode.Success : SampleAppGrpc.ResponseCode.Failure,
            Data = todo?.ToGrpcDto()
        };

        logger.Log(LogLevel.Information, "Save {TodoItemDto} - Finish", response.Data?.SerializeToJson());
        return response;
    }

    public override async Task<Empty> Delete(SampleAppGrpc.ServiceRequestId request, ServerCallContext context)
    {
        await todoService.DeleteItemAsync(new Guid(request.Id));
        return new Empty();
    }
}
