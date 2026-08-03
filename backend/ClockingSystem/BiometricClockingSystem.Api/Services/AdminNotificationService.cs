using System.Collections.Concurrent;
using System.Threading.Channels;

namespace BiometricClockingSystem.Api.Services;

public sealed record AdminOverrideNotification(
    int OverrideRequestId,
    string EmployeeNumber,
    string RequestedClockType,
    DateTime RequestedAt);

public sealed class AdminNotificationService
{
    private readonly ConcurrentDictionary<Guid, Channel<AdminOverrideNotification>> _subscribers = new();

    public (Guid Id, ChannelReader<AdminOverrideNotification> Reader) Subscribe()
    {
        var id = Guid.NewGuid();
        var channel = Channel.CreateUnbounded<AdminOverrideNotification>();
        _subscribers[id] = channel;
        return (id, channel.Reader);
    }

    public void Unsubscribe(Guid id)
    {
        if (_subscribers.TryRemove(id, out var channel))
            channel.Writer.TryComplete();
    }

    public void Publish(AdminOverrideNotification notification)
    {
        foreach (var subscriber in _subscribers.Values)
            subscriber.Writer.TryWrite(notification);
    }
}
