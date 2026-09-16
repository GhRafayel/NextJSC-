using Microsoft.AspNetCore.SignalR;
using Backend.Hubs;
using System.Collections.Concurrent;

namespace Backend.Services;

public class RoomCountdownService(IHubContext<GameHub> hub)
{
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _timers = new();

    public void StartCountdown(string roomId, int seconds, Func<Task> onExpire)
    {
        if (_timers.ContainsKey(roomId)) return;

        var cts = new CancellationTokenSource();
        _timers[roomId] = cts;

        _ = Task.Run(async () =>
        {
            try
            {
                for (int s = seconds; s > 0; s--)
                {
                    await hub.Clients.Group(roomId).SendAsync("room-countdown", new { roomId, seconds = s });
                    await Task.Delay(TimeSpan.FromSeconds(1), cts.Token);
                }
                _timers.TryRemove(roomId, out _);
                await onExpire();
            }
            catch (TaskCanceledException)
            {}
        });
    }

    public void CancelCountdown(string roomId)
    {
        if (_timers.TryRemove(roomId, out CancellationTokenSource? cts))
            cts.Cancel();
    }
}
