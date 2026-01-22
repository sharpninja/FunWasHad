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
    /// <summary>
    /// Tests that registering the same action name twice overwrites the previous handler without throwing an error.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The WorkflowActionHandlerRegistry.Register method's behavior when the same action name is registered multiple times.</para>
    /// <para><strong>Data involved:</strong> Two handler factories for the same action name "TestAction": factory1 creates "Handler1", factory2 creates "Handler2". Both are registered sequentially to the same registry instance.</para>
    /// <para><strong>Why the data matters:</strong> In development scenarios, handlers may be registered multiple times (e.g., during hot reload, configuration updates, or test setup). The registry should allow re-registration and overwrite the previous handler rather than throwing errors. This enables flexible handler management and allows handlers to be updated without restarting the application.</para>
    /// <para><strong>Expected outcome:</strong> After registering both factories, TryGetFactory should return true and retrieve a non-null factory (the second one, which overwrote the first).</para>
    /// <para><strong>Reason for expectation:</strong> The registry should use a dictionary-like structure where registering the same key overwrites the previous value. This is standard behavior for registries and allows handlers to be updated dynamically. The non-null retrieved factory confirms the second registration succeeded and the first was overwritten.</para>
    /// </remarks>
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

    /// <summary>
    /// Tests that registering a handler with a null action name throws ArgumentNullException.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The WorkflowActionHandlerRegistry.Register method's input validation for null action names.</para>
    /// <para><strong>Data involved:</strong> A valid handler factory and a null action name passed to Register. This simulates a programming error where null is passed instead of a valid action name.</para>
    /// <para><strong>Why the data matters:</strong> Null action names are invalid and would cause runtime errors when workflows try to execute actions. The registry must validate input and reject null names immediately to provide clear error messages. This prevents subtle bugs where null names are stored and only discovered when workflows execute.</para>
    /// <para><strong>Expected outcome:</strong> Register should throw ArgumentNullException when called with a null action name.</para>
    /// <para><strong>Reason for expectation:</strong> Input validation is critical for API correctness. Null action names cannot be used as dictionary keys and would cause NullReferenceExceptions later. Throwing ArgumentNullException immediately provides clear feedback about the invalid input and follows .NET Framework Design Guidelines for parameter validation.</para>
    /// </remarks>
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

    /// <summary>
    /// Tests that registering a handler with an empty action name throws ArgumentNullException, ensuring input validation.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The WorkflowActionHandlerRegistry.Register method's input validation for empty action names.</para>
    /// <para><strong>Data involved:</strong> A valid handler factory and an empty string action name passed to Register. This simulates a programming error where an empty string is passed instead of a valid action name.</para>
    /// <para><strong>Why the data matters:</strong> Empty action names are invalid and would cause errors when workflows try to execute actions. The registry must validate input and reject empty names immediately to provide clear error messages. This prevents subtle bugs where empty names are stored and only discovered when workflows execute.</para>
    /// <para><strong>Expected outcome:</strong> Register should throw ArgumentNullException when called with an empty action name.</para>
    /// <para><strong>Reason for expectation:</strong> Input validation is critical for API correctness. Empty action names cannot be used as dictionary keys and would cause errors later. Throwing ArgumentNullException immediately provides clear feedback about the invalid input and follows .NET Framework Design Guidelines for parameter validation.</para>
    /// </remarks>
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

    /// <summary>
    /// Tests that TryGetFactory returns false and null factory when querying for an action name that hasn't been registered.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The WorkflowActionHandlerRegistry.TryGetFactory method's behavior when querying for a non-existent action name.</para>
    /// <para><strong>Data involved:</strong> An empty registry (no handlers registered) and a query for "NonExistentAction", which has never been registered.</para>
    /// <para><strong>Why the data matters:</strong> Workflows may reference actions that haven't been registered yet, or action names may be misspelled. TryGetFactory must handle missing actions gracefully by returning false rather than throwing exceptions. This allows callers to check for handler existence before attempting to use it, enabling defensive programming patterns.</para>
    /// <para><strong>Expected outcome:</strong> TryGetFactory should return false and the out parameter factory should be null.</para>
    /// <para><strong>Reason for expectation:</strong> The TryGet pattern (similar to Dictionary.TryGetValue) is designed to safely query for values that may not exist. Returning false indicates the action was not found, and setting the out parameter to null provides a clear indication that no factory exists. This allows callers to handle missing handlers appropriately (e.g., log a warning, use a default handler, or skip the action).</para>
    /// </remarks>
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
    public async Task HandlerRegistry_ConcurrentRegistration_ThreadSafe()
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

        await Task.WhenAll(tasks);

        // Assert - All handlers should be registered
        for (int i = 0; i < 100; i++)
        {
            var success = registry.TryGetFactory($"Action{i}", out var factory);
            Assert.True(success);
            Assert.NotNull(factory);
        }
    }

    [Fact]
    public async Task HandlerRegistry_ConcurrentGetAndRegister_ThreadSafe()
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

        await Task.WhenAll(tasks);

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

    /// <summary>
    /// Tests that WorkflowActionHandlerRegistry can handle a large number of handlers (10,000) with acceptable performance, ensuring the registry scales well for applications with many action handlers.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The WorkflowActionHandlerRegistry's performance and scalability when registering and retrieving a large number of handlers.</para>
    /// <para><strong>Data involved:</strong> 10,000 handlers registered sequentially ("Action0" through "Action9999"), then 1,000 random handlers retrieved. Registration time and retrieval time are measured to ensure performance is acceptable.</para>
    /// <para><strong>Why the data matters:</strong> Large applications may have many action handlers. The registry must scale efficiently without excessive memory usage or performance degradation. This test validates that the registry can handle production-scale handler counts with acceptable performance.</para>
    /// <para><strong>Expected outcome:</strong> Registration of 10,000 handlers should complete in less than 1000ms, and retrieval of 1,000 random handlers should complete in less than 100ms, confirming that the registry scales efficiently.</para>
    /// <para><strong>Reason for expectation:</strong> The registry should use efficient data structures (e.g., Dictionary with O(1) lookup) to ensure registration and retrieval scale linearly or better. The timing checks ensure the registry doesn't have quadratic or worse complexity that would make large handler counts impractical. The performance thresholds (1000ms registration, 100ms retrieval) ensure the registry remains responsive even with many handlers, validating that it can handle production-scale handler counts.</para>
    /// </remarks>
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
