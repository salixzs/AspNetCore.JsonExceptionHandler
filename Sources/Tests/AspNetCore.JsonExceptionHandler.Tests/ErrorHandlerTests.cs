using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text.Json.Serialization;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Salix.AspNetCore.JsonExceptionHandler;
using Microsoft.AspNetCore.Http;

namespace AspNetCore.JsonExceptionHandler.Tests;

[ExcludeFromCodeCoverage]
public class ApiJsonExceptionMiddlewareTests
{
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public ApiJsonExceptionMiddlewareTests() =>
        _jsonSerializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, Converters = { new JsonStringEnumConverter() } };

    [Fact]
    public async Task UnhandledException_WrappedFully()
    {
        // Arrange
        ApplicationException? exc = null;
        try
        {
            // To get stack trace
            var prepared = new ApplicationException("Testable problem");
            throw prepared;
        }
        catch (ApplicationException e)
        {
            exc = e;
        }

        var logger = new Mock<ILogger<ApiJsonExceptionMiddleware>>();
        var middleware = new TestExceptionMiddleware(next: (innerHttpContext) => throw exc, logger.Object, true);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.Invoke(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var reader = new StreamReader(context.Response.Body);
        string responseString = reader.ReadToEnd();

        var responseError = JsonSerializer.Deserialize<ApiError>(responseString, _jsonSerializerOptions);

        // Assert
        responseString.Should().NotContain("ErrorBehavior");
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);
        responseError.Should().NotBeNull();
        responseError!.ErrorType.Should().Be(ApiErrorType.ServerError);
        responseError.ExceptionType.Should().Be("ApplicationException");
        responseError.InnerException.Should().BeNull();
        responseError.Status.Should().Be(500);
        responseError.Title.Should().Be("Testable problem");
        responseError.StackTrace.Should().NotBeEmpty();
        responseError.ValidationErrors.Should().BeEmpty();

        logger.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => true),
            It.IsAny<Exception>(),
            It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task UnhandledInnerException_WrappedFully()
    {
        // Arrange
        ApplicationException exc = null;
        try
        {
            // To get stack trace
            var prepared = new ApplicationException("Testable problem", new ArgumentException("Goin' deeper"));
            throw prepared;
        }
        catch (ApplicationException e)
        {
            exc = e;
        }

        var logger = new Mock<ILogger<ApiJsonExceptionMiddleware>>();
        var middleware = new TestExceptionMiddleware(next: (innerHttpContext) => throw exc, logger.Object);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.Invoke(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var reader = new StreamReader(context.Response.Body);
        string responseString = reader.ReadToEnd();
        ApiError responseError = JsonSerializer.Deserialize<ApiError>(responseString, _jsonSerializerOptions);

        // Assert
        responseError!.InnerException.Should().NotBeNull();
        responseError.InnerException!.Title.Should().Be("Goin' deeper");
        responseError.InnerException.ExceptionType.Should().Be("ArgumentException");
        responseError.InnerException.InnerException.Should().BeNull();
    }

    [Fact]
    public async Task UnhandledInnerInnerException_IsShown()
    {
        // Arrange
        ApplicationException exc = null;
        try
        {
            // To get stack trace
            var prepared = new ApplicationException("Testable problem", new Exception("Goin' deeper", new Exception("Very deep")));
            throw prepared;
        }
        catch (ApplicationException e)
        {
            exc = e;
        }

        var logger = new Mock<ILogger<ApiJsonExceptionMiddleware>>();
        var middleware = new TestExceptionMiddleware(next: (innerHttpContext) => throw exc, logger.Object);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.Invoke(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var reader = new StreamReader(context.Response.Body);
        string responseString = reader.ReadToEnd();
        ApiError responseError = JsonSerializer.Deserialize<ApiError>(responseString, _jsonSerializerOptions);

        // Assert
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);
        responseError.Should().NotBeNull();
        responseError!.InnerException.Should().NotBeNull();
        responseError.InnerException!.Title.Should().Be("Goin' deeper");
        responseError.InnerException.InnerException.Should().NotBeNull();
        responseError.InnerException.InnerException!.Title.Should().Be("Very deep");
    }

    [Fact]
    public async Task UnhandledInnerInnerException_VeryDeep()
    {
        // Arrange
        ApplicationException exc = null;
        try
        {
            // To get stack trace
            var prepared = new ApplicationException("Testable problem", new Exception("Inner", new Exception("1", new Exception("2", new Exception("3", new Exception("4"))))));
            throw prepared;
        }
        catch (ApplicationException e)
        {
            exc = e;
        }

        var logger = new Mock<ILogger<ApiJsonExceptionMiddleware>>();
        var middleware = new TestExceptionMiddleware(next: (innerHttpContext) => throw exc, logger.Object);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.Invoke(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var reader = new StreamReader(context.Response.Body);
        string responseString = reader.ReadToEnd();
        ApiError responseError = JsonSerializer.Deserialize<ApiError>(responseString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true, Converters = { new JsonStringEnumConverter() } });

        // Assert
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);
        responseError.Should().NotBeNull();
        //responseError.InnerException.Should().Be("(Exception) Inner");
        //responseError.InnerInnerException.Should().Be("(Exception) 1; (Exception) 2; (Exception) 3; (Exception) 4");
    }

    [Fact]
    public async Task DataValidationException_WrappedFully()
    {
        // Arrange
        TestDataValidationException exc = null;
        try
        {
            // To get stack trace
            var prepared = new TestDataValidationException("Data validation problem.", new List<TestValidatedProperty> { new TestValidatedProperty { PropertyName = "Uno", ValidationMessage = "is not a game", AppliedValue = "cards" } });
            throw prepared;
        }
        catch (TestDataValidationException e)
        {
            exc = e;
        }

        var logger = new Mock<ILogger<ApiJsonExceptionMiddleware>>();
        var middleware = new TestExceptionMiddleware(next: (innerHttpContext) => throw exc, logger.Object, true);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.Invoke(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var reader = new StreamReader(context.Response.Body);
        string responseString = reader.ReadToEnd();
        ApiError responseError = JsonSerializer.Deserialize<ApiError>(responseString, _jsonSerializerOptions);

        // Assert
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
        responseError.Should().NotBeNull();
        responseError!.ErrorType.Should().Be(ApiErrorType.DataValidationError);
        responseError.ExceptionType.Should().Be("TestDataValidationException");
        responseError.InnerException.Should().BeNull();
        responseError.Status.Should().Be(400);
        responseError.Title.Should().Be("Data validation problem.");
        responseError.StackTrace.Should().NotBeEmpty();
        responseError.ValidationErrors.Should().NotBeEmpty();
        responseError.ValidationErrors.Should().HaveCount(1);
        responseError.ValidationErrors[0].PropertyName.Should().Be("Uno");
        responseError.ValidationErrors[0].Message.Should().Be("is not a game");
        responseError.ValidationErrors[0].AttemptedValue.ToString().Should().Be("cards");

        logger.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => true),
            It.IsAny<Exception>(),
            It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task CanceledException_IgnoredFully()
    {
        // Arrange
        OperationCanceledException exc = null;
        try
        {
            // To get stack trace
            var prepared = new OperationCanceledException("User canceled operation.");
            throw prepared;
        }
        catch (OperationCanceledException e)
        {
            exc = e;
        }

        var logger = new Mock<ILogger<ApiJsonExceptionMiddleware>>();
        var middleware = new TestExceptionMiddleware(next: (innerHttpContext) => throw exc, logger.Object, true);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.Invoke(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var reader = new StreamReader(context.Response.Body);
        string responseString = reader.ReadToEnd();

        // Assert
        responseString.Should().BeEmpty();
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.OK);

        logger.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => true),
            It.IsAny<Exception>(),
            It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Never);
    }

    [Fact]
    public async Task CanceledTask_NoLog()
    {
        // Arrange
        TaskCanceledException exc = new TaskCanceledException("Initial");
        try
        {
            // To get stack trace
            var prepared = new TaskCanceledException("Task was canceled");
            throw prepared;
        }
        catch (TaskCanceledException e)
        {
            exc = e;
        }

        var logger = new Mock<ILogger<ApiJsonExceptionMiddleware>>();
        var middleware = new TestExceptionMiddleware(next: (innerHttpContext) => throw exc, logger.Object, true);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.Invoke(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var reader = new StreamReader(context.Response.Body);
        string responseString = reader.ReadToEnd();
        var responseError = JsonSerializer.Deserialize<ApiError>(responseString, _jsonSerializerOptions);

        // Assert
        responseString.Should().NotContain("ErrorBehavior");
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.UnprocessableEntity);
        responseError.Should().NotBeNull();
        responseError!.ErrorType.Should().Be(ApiErrorType.CancelledOperation);
        responseError.ExceptionType.Should().Be("TaskCanceledException");
        responseError.InnerException.Should().BeNull();
        responseError.Status.Should().Be(422);
        responseError.Title.Should().Be("Task was canceled");
        responseError.StackTrace.Should().NotBeEmpty();
        responseError.ValidationErrors.Should().BeEmpty();

        logger.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => true),
            It.IsAny<Exception>(),
            It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Never);
    }
}

[ExcludeFromCodeCoverage]
public class TestExceptionMiddleware : ApiJsonExceptionMiddleware
{
    public TestExceptionMiddleware(RequestDelegate next, ILogger<ApiJsonExceptionMiddleware> logger, bool showStackTrace = false) : base(next, logger, showStackTrace)
    {
    }

    protected override ApiError HandleSpecialException(ApiError apiError, Exception exception)
    {
        // When using FluentValidation, could use also handler for its ValidationException in stead of this custom
        if (exception is TestDataValidationException validationException)
        {
            apiError.Status = 400;
            apiError.ErrorType = ApiErrorType.DataValidationError;
            apiError.ValidationErrors
                .AddRange(
                    validationException.ValidationErrors.Select(failure =>
                        new ApiDataValidationError
                        {
                            Message = failure.ValidationMessage,
                            PropertyName = failure.PropertyName,
                            AttemptedValue = failure.AppliedValue
                        }));
        }

        if (exception is OperationCanceledException cancelledOperationException)
        {
            apiError.ErrorBehavior = ApiErrorBehavior.Ignore;
        }

        if (exception is TaskCanceledException cancelledTaskException)
        {
            // Only respons with error, but do not log it.
            apiError.ErrorBehavior = ApiErrorBehavior.RespondWithError;
            apiError.Status = (int)HttpStatusCode.UnprocessableEntity; // or other by your design
            apiError.ErrorType = ApiErrorType.CancelledOperation;
        }

        return apiError;
    }
}

[ExcludeFromCodeCoverage]
public class TestDataValidationException : Exception
{
    public List<TestValidatedProperty> ValidationErrors { get; private set; } = new List<TestValidatedProperty>();

    public TestDataValidationException()
    {
    }

    public TestDataValidationException(string message)
        : base(message)
    {
    }

    public TestDataValidationException(string message, List<TestValidatedProperty> validationErrors)
        : base(message) => this.ValidationErrors = validationErrors;

    public TestDataValidationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

[ExcludeFromCodeCoverage]
public class TestValidatedProperty
{
    public string? PropertyName { get; set; }
    public string? ValidationMessage { get; set; }
    public object? AppliedValue { get; set; }
}
