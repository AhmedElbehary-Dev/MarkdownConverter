using System;

namespace MarkdownConverter.Platform;

public interface IUiTimer : IDisposable
{
    void Start();
    void Stop();
}
