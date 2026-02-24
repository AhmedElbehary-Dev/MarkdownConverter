using Avalonia.Threading;
using MarkdownConverter.Platform;
using System;

namespace MarkdownConverter.Desktop.Services;

public sealed class AvaloniaUiTimer : IUiTimer
{
    private readonly DispatcherTimer _timer;
    private readonly EventHandler _handler;

    public AvaloniaUiTimer(TimeSpan interval, Action tick)
    {
        if (tick is null)
        {
            throw new ArgumentNullException(nameof(tick));
        }

        _timer = new DispatcherTimer
        {
            Interval = interval
        };

        _handler = (_, _) => tick();
        _timer.Tick += _handler;
    }

    public void Start() => _timer.Start();

    public void Stop() => _timer.Stop();

    public void Dispose()
    {
        _timer.Tick -= _handler;
        _timer.Stop();
    }
}
