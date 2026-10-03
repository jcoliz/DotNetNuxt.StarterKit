using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using DotNetNuxt.StarterKit.Controllers.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace DotNetNuxt.Tests.Unit;

public class TestCorrelationMiddlewareTests
{
    [Test]
    public async Task Invoke_enriches_activity_and_scopes_downstream_logs()
    {
        using var activity = new Activity("request").Start();
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Test-Name"] = "Weather_returns_forecasts";
        context.Request.Headers["X-Test-Id"] = "123";
        context.Request.Headers["X-Test-Class"] = "WeatherTests";
        context.Request.Headers["X-Test-Client"] = "BackEnd";
        var logger = new ScopeLogger();
        var called = false;
        var middleware = new TestCorrelationMiddleware(receivedContext =>
        {
            called = true;
            Assert.That(receivedContext, Is.SameAs(context));
            Assert.That(logger.Scope!["TestName"], Is.EqualTo("Weather_returns_forecasts"));
            Assert.That(logger.Disposed, Is.False);
            Assert.That(activity.GetTagItem("test.name"), Is.EqualTo("Weather_returns_forecasts"));
            Assert.That(activity.GetTagItem("test.id"), Is.EqualTo("123"));
            Assert.That(activity.GetTagItem("test.class"), Is.EqualTo("WeatherTests"));
            Assert.That(activity.GetTagItem("test.client"), Is.EqualTo("BackEnd"));
            return Task.CompletedTask;
        }, logger);

        await middleware.Invoke(context);

        Assert.That(called, Is.True);
        Assert.That(logger.Disposed, Is.True);
    }

    [Test]
    public async Task Invoke_without_test_name_does_not_enrich_request()
    {
        using var activity = new Activity("request").Start();
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Test-Id"] = "123";
        var logger = new ScopeLogger();
        var called = false;
        var middleware = new TestCorrelationMiddleware(_ =>
        {
            called = true;
            return Task.CompletedTask;
        }, logger);

        await middleware.Invoke(context);

        Assert.That(called, Is.True);
        Assert.That(logger.Scope, Is.Null);
        Assert.That(activity.TagObjects, Is.Empty);
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task Invoke_with_only_test_name_allows_missing_activity_and_optional_headers(bool hasActivity)
    {
        using var activity = hasActivity ? new Activity("request").Start() : null;
        var previousActivity = Activity.Current;
        if (!hasActivity)
        {
            Activity.Current = null;
        }

        try
        {
            var context = new DefaultHttpContext();
            context.Request.Headers["X-Test-Name"] = "Weather_returns_forecasts";
            var logger = new ScopeLogger();
            var called = false;
            var middleware = new TestCorrelationMiddleware(_ =>
            {
                called = true;
                Assert.That(logger.Scope!["TestName"], Is.EqualTo("Weather_returns_forecasts"));
                return Task.CompletedTask;
            }, logger);

            await middleware.Invoke(context);

            Assert.That(called, Is.True);
            Assert.That(logger.Disposed, Is.True);
            if (activity is not null)
            {
                Assert.That(activity.GetTagItem("test.name"), Is.EqualTo("Weather_returns_forecasts"));
                Assert.That(activity.GetTagItem("test.id"), Is.Null);
                Assert.That(activity.GetTagItem("test.class"), Is.Null);
                Assert.That(activity.GetTagItem("test.client"), Is.Null);
            }
        }
        finally
        {
            Activity.Current = previousActivity;
        }
    }

    [Test]
    public async Task Invoke_disposes_scope_when_downstream_throws()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Test-Name"] = "Weather_returns_forecasts";
        var logger = new ScopeLogger();
        var exception = new InvalidOperationException("Failed request");
        var middleware = new TestCorrelationMiddleware(_ => Task.FromException(exception), logger);

        var result = await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.Invoke(context));

        Assert.That(result, Is.SameAs(exception));
        Assert.That(logger.Disposed, Is.True);
    }

    private sealed class ScopeLogger : ILogger<TestCorrelationMiddleware>, IDisposable
    {
        public IReadOnlyDictionary<string, object>? Scope { get; private set; }
        public bool Disposed { get; private set; }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            Scope = (IReadOnlyDictionary<string, object>)(object)state;
            return this;
        }

        public bool IsEnabled(LogLevel logLevel) => false;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
        { }

        public void Dispose() => Disposed = true;
    }
}