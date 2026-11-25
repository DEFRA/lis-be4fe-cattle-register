// <copyright file="ApiExceptionHandlerTest.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Be4Fe.Api.Tests.Exceptions;

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Defra.Livestock.Be4Fe.Api.Exceptions;
using Defra.Livestock.Be4Fe.Api.Middleware.Headers;
using Defra.Livestock.Be4Fe.CattleApi;
using Defra.Livestock.Sdk.Api.Strategies.Abstractions.Exceptions;
using Defra.Livestock.Sdk.Core.Exceptions;
using Defra.Livestock.Sdk.Submissions.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using CattleApiUnavailableException = Defra.Livestock.Be4Fe.CattleApi.ServiceUnavailableException;
using CoreServiceUnavailableException = Defra.Livestock.Sdk.Core.Exceptions.ServiceUnavailableException;

public sealed class ApiExceptionHandlerTest
{
    public static TheoryData<Exception, int, string> ExceptionCases => new()
    {
        { new NotFoundException("missing"), StatusCodes.Status404NotFound, "Not Found" },
        { new HoldingNotFoundException("22/050/0050"), StatusCodes.Status404NotFound, "Not Found" },
        { new ConflictException("duplicate"), StatusCodes.Status409Conflict, "Conflict" },
        { new SubmissionAlreadySubmittedException("ABC-DEF-GHJ"), StatusCodes.Status409Conflict, "Conflict" },
        { new SubmissionNotFoundException("ABC-DEF-GHJ"), StatusCodes.Status404NotFound, "Not Found" },
        { new SubmissionTransferNotConfiguredException(), StatusCodes.Status503ServiceUnavailable, "Service Unavailable" },
        { new CattleApiUnavailableException("try again later", new HttpRequestException()), StatusCodes.Status503ServiceUnavailable, "Service Unavailable" },
        { new CoreServiceUnavailableException("try again later"), StatusCodes.Status503ServiceUnavailable, "Service Unavailable" },
        { new BusinessRuleException("invalid"), StatusCodes.Status400BadRequest, "Bad Request" },
        { new BadHttpRequestException("unreadable body"), StatusCodes.Status400BadRequest, "Bad Request" },
        { new ArgumentException("bad argument"), StatusCodes.Status400BadRequest, "Bad Request" },
        { new UnauthorizedAccessException("denied"), StatusCodes.Status403Forbidden, "Forbidden" },
        { new InvalidOperationException("failure"), StatusCodes.Status500InternalServerError, "Internal Server Error" },
    };

    [Theory]
    [SuppressMessage(
        "Usage",
        "xUnit1045:Avoid using TheoryData type arguments that are not serializable",
        Justification = "Exception instances cannot be natively serialized by xUnit but are required for this test state.")]
    [MemberData(nameof(ExceptionCases))]
    public async Task ShouldMapExceptionsToProblemDetails(Exception exception, int expectedStatus, string expectedTitle)
    {
        var handler = new ApiExceptionHandler(NullLogger<ApiExceptionHandler>.Instance);
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/api/test";
        context.TraceIdentifier = "trace-123";
        context.Response.Body = new MemoryStream();

        var handled = await handler.TryHandleAsync(context, exception, TestContext.Current.CancellationToken);

        handled.ShouldBeTrue();
        context.Response.StatusCode.ShouldBe(expectedStatus);
        context.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(context.Response.Body, cancellationToken: TestContext.Current.CancellationToken);
        var problem = document.RootElement;
        problem.GetProperty("status").GetInt32().ShouldBe(expectedStatus);
        problem.GetProperty("title").GetString().ShouldBe(expectedTitle);
        problem.GetProperty("detail").GetString().ShouldBe(exception.Message);
        problem.GetProperty("instance").GetString().ShouldBe("/api/test");
        problem.GetProperty("traceId").GetString().ShouldBe("trace-123");
    }

    [Fact]
    public async Task ShouldReturnAndEchoTheCorrelationId()
    {
        var handler = new ApiExceptionHandler(NullLogger<ApiExceptionHandler>.Instance);
        var context = new DefaultHttpContext();
        context.Request.Headers[RequestHeaderNames.CorrelationId] = "corr-598";
        context.Response.Body = new MemoryStream();

        await handler.TryHandleAsync(context, new NotFoundException("missing"), TestContext.Current.CancellationToken);

        context.Response.Headers[RequestHeaderNames.CorrelationId].ToString().ShouldBe("corr-598");
        context.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(context.Response.Body, cancellationToken: TestContext.Current.CancellationToken);
        document.RootElement.GetProperty("correlationId").GetString().ShouldBe("corr-598");
    }
}
