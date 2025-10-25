using System.Collections.Concurrent;
using System.Threading.Channels;

namespace AISmartHome.Agents.Events;

/// <summary>
/// Event bus for agent-to-agent event-driven communication
/// Supports pub-sub pattern for asynchronous event handling
/// </summary>
public class EventBus
{
    private readonly ConcurrentDictionary<string, List<Func<IAgentEvent, Task>>> _subscribers = new();
    private readonly Channel<IAgentEvent> _eventChannel;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _processingTask;

    public EventBus(int channelCapacity = 1000)
    {
        _eventChannel = Channel.CreateBounded<IAgentEvent>(new BoundedChannelOptions(channelCapacity)
        {
            FullMode = BoundedChannelFullMode.DropOldest
        });
        
        // Start background event processing
        _processingTask = Task.Run(ProcessEventsAsync);
        
        Console.WriteLine($"[EventBus] Initialized with capacity: {channelCapacity}");
    }

    /// <summary>
    /// Publish an event to the bus
    /// </summary>
    public async Task PublishAsync(IAgentEvent agentEvent, CancellationToken ct = default)
    {
        Console.WriteLine($"[EventBus] Publishing event: type={agentEvent.EventType}, source={agentEvent.Source}");
        
        try
        {
            await _eventChannel.Writer.WriteAsync(agentEvent, ct);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] EventBus failed to publish event: {ex.Message}");
        }
    }

    /// <summary>
    /// Subscribe to events of a specific type
    /// </summary>
    public void Subscribe(string eventType, Func<IAgentEvent, Task> handler)
    {
        Console.WriteLine($"[EventBus] New subscription for event type: {eventType}");
        
        if (!_subscribers.TryGetValue(eventType, out var handlers))
        {
            handlers = new List<Func<IAgentEvent, Task>>();
            _subscribers[eventType] = handlers;
        }
        
        handlers.Add(handler);
    }

    /// <summary>
    /// Subscribe to all events
    /// </summary>
    public void SubscribeAll(Func<IAgentEvent, Task> handler)
    {
        Subscribe("*", handler);
    }

    /// <summary>
    /// Unsubscribe from events
    /// </summary>
    public void Unsubscribe(string eventType, Func<IAgentEvent, Task> handler)
    {
        if (_subscribers.TryGetValue(eventType, out var handlers))
        {
            handlers.Remove(handler);
        }
    }

    /// <summary>
    /// Process events from the channel
    /// </summary>
    private async Task ProcessEventsAsync()
    {
        Console.WriteLine("[EventBus] Event processing started");
        
        await foreach (var agentEvent in _eventChannel.Reader.ReadAllAsync(_cts.Token))
        {
            try
            {
                // Get handlers for this event type
                var handlers = new List<Func<IAgentEvent, Task>>();
                
                if (_subscribers.TryGetValue(agentEvent.EventType, out var specificHandlers))
                {
                    handlers.AddRange(specificHandlers);
                }
                
                if (_subscribers.TryGetValue("*", out var wildcardHandlers))
                {
                    handlers.AddRange(wildcardHandlers);
                }
                
                if (handlers.Count == 0)
                {
                    Console.WriteLine($"[EventBus] No handlers for event type: {agentEvent.EventType}");
                    continue;
                }
                
                Console.WriteLine($"[EventBus] Dispatching event to {handlers.Count} handlers");
                
                // Execute handlers in parallel (fire and forget)
                var handlerTasks = handlers.Select(handler => 
                    Task.Run(async () =>
                    {
                        try
                        {
                            await handler(agentEvent);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[ERROR] Event handler failed: {ex.Message}");
                        }
                    })
                );
                
                await Task.WhenAll(handlerTasks);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] EventBus processing failed: {ex.Message}");
            }
        }
        
        Console.WriteLine("[EventBus] Event processing stopped");
    }

    /// <summary>
    /// Get event statistics
    /// </summary>
    public EventBusStats GetStats()
    {
        return new EventBusStats
        {
            SubscriberCount = _subscribers.Sum(s => s.Value.Count),
            EventTypeCount = _subscribers.Count,
            QueuedEventCount = _eventChannel.Reader.Count
        };
    }

    /// <summary>
    /// Shutdown the event bus
    /// </summary>
    public async Task ShutdownAsync()
    {
        Console.WriteLine("[EventBus] Shutting down...");
        
        _eventChannel.Writer.Complete();
        _cts.Cancel();
        
        try
        {
            await _processingTask;
        }
        catch (OperationCanceledException)
        {
            // Expected
        }
        
        Console.WriteLine("[EventBus] Shutdown complete");
    }
}

public record EventBusStats
{
    public int SubscriberCount { get; init; }
    public int EventTypeCount { get; init; }
    public int QueuedEventCount { get; init; }
}

