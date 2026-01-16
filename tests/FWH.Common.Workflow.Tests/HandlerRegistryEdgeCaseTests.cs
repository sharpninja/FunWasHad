using Xunit;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using FWH.Common.Workflow.Actions;
using Microsoft.Extensions.DependencyInjection;

namespace FWH.Common.Workflow.Tests;

/// <summary>
/// Tests for edge cases and error conditions in WorkflowActionHandlerRegistry
/// </summary>
public class HandlerRegistryEdgeCaseTests
{
    [Fact]
    public void HandlerRegistry_RegisterSameActionTwice_OverwritesWithoutError()
    {
        // Arrange
        var registry = new WorkflowActionHandlerRegistry();
        var services = new ServiceCollection().BuildServiceProvider();
        
        Func<IServiceProvider, IWorkflowActionHandler> factory1 = sp => new TestActionHandler("Handler1");
        Func<IServiceProvider, IWorkflowActionHandler> factory2 = sp => new TestActionHandler("Handler2");

        // Act
        registry.Register("TestAction", factory1);
        registry.Register("TestAction", factory2); // Should overwrite

        // Assert
        var success = registry.TryGetFactory("TestAction", out var retrieved);
        Assert.True(success);
        Assert.NotNull(retrieved);
    }

    [Fact]
    public void HandlerRegistry_RegisterNullActionName_ThrowsArgumentNullException()
    {
        // Arrange
        var registry = new WorkflowActionHandlerRegistry();
        Func<IServiceProvider, IWorkflowActionHandler> factory = sp => new TestActionHandler("Test");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            registry.Register(null!, factory));
    }

    [Fact]
    public void HandlerRegistry_RegisterEmptyActionName_ThrowsArgumentException()
    {
        // Arrange
        var registry = new WorkflowActionHandlerRegistry();
        Func<IServiceProvider, IWorkflowActionHandler> factory = sp => new TestActionHandler("Test");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            registry.Register(string.Empty, factory));
    }

    [Fact]
    public void HandlerRegistry_RegisterWhitespaceActionName_ThrowsArgumentException()
    {
        // Arrange
        var registry = new WorkflowActionHandlerRegistry();
        Func<IServiceProvider, IWorkflowActionHandler> factory = sp => new TestActionHandler("Test");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            registry.Register("   ", factory));
    }

    [Fact]
    public void HandlerRegistry_RegisterNullFactory_ThrowsArgumentNullException()
    {
        // Arrange
        var registry = new WorkflowActionHandlerRegistry();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            registry.Register("TestAction", null!));
    }

    [Fact]
    public void HandlerRegistry_TryGetFactoryForNonExistentAction_ReturnsFalse()
    {
        // Arrange
        var registry = new WorkflowActionHandlerRegistry();

        // Act
        var success = registry.TryGetFactory("NonExistentAction", out var factory);

        // Assert
        Assert.False(success);
        Assert.Null(factory);
    }

    [Fact]
    public void HandlerRegistry_TryGetFactoryWithNullActionName_ReturnsFalse()
    {
        // Arrange
        var registry = new WorkflowActionHandlerRegistry();

        // Act
        var success = registry.TryGetFactory(null!, out var factory);

        // Assert
        Assert.False(success);
        Assert.Null(factory);
    }

    [Fact]
    public void HandlerRegistry_ConcurrentRegistration_ThreadSafe()
    {
        // Arrange
        var registry = new WorkflowActionHandlerRegistry();
        var tasks = new List<Task>();

        // Act - Register 100 handlers concurrently
        for (int i = 0; i < 100; i++)
        {
            var index = i;
            tasks.Add(Task.Run(() =>
            {
                Func<IServiceProvider, IWorkflowActionHandler> factory = sp => new TestActionHandler($"Handler{index}");
                registry.Register($"Action{index}", factory);
            }));
        }

        Task.WaitAll(tasks.ToArray());

        // Assert - All handlers should be registered
        for (int i = 0; i < 100; i++)
        {
            var success = registry.TryGetFactory($"Action{i}", out var factory);
            Assert.True(success);
            Assert.NotNull(factory);
        }
    }

    [Fact]
    public void HandlerRegistry_ConcurrentGetAndRegister_ThreadSafe()
    {
        // Arrange
        var registry = new WorkflowActionHandlerRegistry();
        var tasks = new List<Task>();
        var exceptions = new List<Exception>();

        // Pre-register some handlers
        for (int i = 0; i < 50; i++)
        {
            var index = i;
            registry.Register($"Action{index}", sp => new TestActionHandler($"Handler{index}"));
        }

        // Act - Concurrent reads and writes
        for (int i = 0; i < 100; i++)
        {
            var index = i;
            tasks.Add(Task.Run(() =>
            {
                try
                {
                    if (index % 2 == 0)
                    {
                        // Read
                        registry.TryGetFactory($"Action{index % 50}", out _);
                    }
                    else
                    {
                        // Write
                        registry.Register($"NewAction{index}", sp => new TestActionHandler($"NewHandler{index}"));
                    }
                }
                catch (Exception ex)
                {
                    lock (exceptions)
                    {
                        exceptions.Add(ex);
                    }
                }
            }));
        }

        Task.WaitAll(tasks.ToArray());

        // Assert - No exceptions should have occurred
        Assert.Empty(exceptions);
    }

    [Fact]
    public void HandlerRegistry_FactoryReturnsNull_ReturnsNullHandler()
    {
        // Arrange
        var registry = new WorkflowActionHandlerRegistry();
        var services = new ServiceCollection().BuildServiceProvider();

        registry.Register("TestAction", sp => null!);

        // Act
        var success = registry.TryGetFactory("TestAction", out var factory);
        var handler = factory?.Invoke(services);

        // Assert
        Assert.True(success);
        Assert.NotNull(factory);
        Assert.Null(handler); // Factory returned null
    }

    [Fact]
    public void HandlerRegistry_FactoryThrowsException_PropagatesException()
    {
        // Arrange
        var registry = new WorkflowActionHandlerRegistry();
        var services = new ServiceCollection().BuildServiceProvider();

        registry.Register("TestAction", sp =>
        {
            throw new InvalidOperationException("Factory error");
        });

        // Act & Assert
        var success = registry.TryGetFactory("TestAction", out var factory);
        Assert.True(success);
        
        var exception = Assert.Throws<InvalidOperationException>(() =>
            factory!.Invoke(services));
        
        Assert.Equal("Factory error", exception.Message);
    }

    [Fact]
    public void HandlerRegistry_FactoryCalledMultipleTimes_CreatesNewInstanceEachTime()
    {
        // Arrange
        var registry = new WorkflowActionHandlerRegistry();
        var services = new ServiceCollection().BuildServiceProvider();
        var callCount = 0;

        registry.Register("TestAction", sp =>
        {
            callCount++;
            return new TestActionHandler($"Handler{callCount}");
        });

        // Act
        registry.TryGetFactory("TestAction", out var factory);
        var handler1 = factory!.Invoke(services);
        var handler2 = factory.Invoke(services);

        // Assert
        Assert.Equal(2, callCount);
        Assert.NotNull(handler1);
        Assert.NotNull(handler2);
        // They should be different instances
        Assert.NotSame(handler1, handler2);
    }

    [Fact]
    public void HandlerRegistry_CaseInsensitiveActionNames_TreatsAsSame()
    {
        // Arrange
        var registry = new WorkflowActionHandlerRegistry();
        Func<IServiceProvider, IWorkflowActionHandler> factory1 = sp => new TestActionHandler("Handler1");
        Func<IServiceProvider, IWorkflowActionHandler> factory2 = sp => new TestActionHandler("Handler2");

        // Act
        registry.Register("TestAction", factory1);
        registry.Register("testaction", factory2); // Should overwrite due to case-insensitive

        // Assert
        var success1 = registry.TryGetFactory("TestAction", out var retrieved1);
        var success2 = registry.TryGetFactory("testaction", out var retrieved2);

        Assert.True(success1);
        Assert.True(success2);
        // Should be the same factory (second overwrote first)
        Assert.Same(retrieved1, retrieved2);
    }

    [Fact]
    public void HandlerRegistry_LargeNumberOfHandlers_PerformanceAcceptable()
    {
        // Arrange
        var registry = new WorkflowActionHandlerRegistry();

        // Act - Register 10,000 handlers
        var sw = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < 10000; i++)
        {
            var index = i;
            registry.Register($"Action{index}", sp => new TestActionHandler($"Handler{index}"));
        }
        sw.Stop();

        var registrationTime = sw.Elapsed;

        // Retrieve 1,000 random handlers
        sw.Restart();
        var random = new Random(42);
        for (int i = 0; i < 1000; i++)
        {
            var index = random.Next(10000);
            var success = registry.TryGetFactory($"Action{index}", out var factory);
            Assert.True(success);
            Assert.NotNull(factory);
        }
        sw.Stop();

        var retrievalTime = sw.Elapsed;

        // Assert - Performance should be acceptable
        Assert.True(registrationTime.TotalMilliseconds < 1000, $"Registration took {registrationTime.TotalMilliseconds}ms (expected < 1000ms)");
        Assert.True(retrievalTime.TotalMilliseconds < 100, $"Retrieval took {retrievalTime.TotalMilliseconds}ms (expected < 100ms)");
    }

    /// <summary>
    /// Test implementation of IWorkflowActionHandler for testing purposes
    /// </summary>
    private class TestActionHandler : IWorkflowActionHandler
    {
        private readonly string _name;

        public TestActionHandler(string name)
        {
            _name = name;
        }

        public string Name => _name;

        public Task<IDictionary<string, string>?> HandleAsync(
            ActionHandlerContext context,
            IDictionary<string, string> parameters,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<IDictionary<string, string>?>(new Dictionary<string, string>
            {
                ["handlerName"] = _name,
                ["handled"] = "true"
            });
        }
    }
}
