using System;
using System.ComponentModel;

namespace MiniDashboard.Avalonia;

/// <summary>A proposed or committed content assignment, shared by both dashboard hosts.</summary>
public sealed class DashboardContentChangingEventArgs : CancelEventArgs
{
    internal DashboardContentChangingEventArgs(string? beforeId, string? afterId, Guid? cellId = null)
    {
        BeforeId = beforeId;
        AfterId = afterId;
        CellId = cellId;
    }

    /// <summary>Gets the previous assignment.</summary>
    public string? BeforeId { get; }

    /// <summary>Gets the proposed assignment.</summary>
    public string? AfterId { get; }

    /// <summary>Gets a matrix cell ID, or null for a content tile.</summary>
    public Guid? CellId { get; }
}
