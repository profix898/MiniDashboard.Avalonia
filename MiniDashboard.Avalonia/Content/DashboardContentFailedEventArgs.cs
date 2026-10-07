using System;

namespace MiniDashboard.Avalonia.Content;

/// <summary>Reports a content creation error to the host. The original exception is still propagated.</summary>
public sealed class DashboardContentFailedEventArgs : EventArgs
{
    internal DashboardContentFailedEventArgs(Exception error)
    {
        Error = error;
    }

    /// <summary>Gets the factory or catalog error.</summary>
    public Exception Error { get; }
}
