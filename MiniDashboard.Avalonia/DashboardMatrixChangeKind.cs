namespace MiniDashboard.Avalonia;

/// <summary>The kind of matrix transaction.</summary>
public enum DashboardMatrixChangeKind
{
    /// <summary>A row is inserted.</summary>
    InsertRow,

    /// <summary>A column is inserted.</summary>
    InsertColumn,

    /// <summary>A row is removed.</summary>
    RemoveRow,

    /// <summary>A column is removed.</summary>
    RemoveColumn,

    /// <summary>Content is assigned or cleared.</summary>
    Content,

    /// <summary>Content assignments are moved or swapped.</summary>
    MoveContent,

    /// <summary>Proportional sizes change.</summary>
    Proportions,

    /// <summary>A saved layout replaces the current layout.</summary>
    Restore
}
