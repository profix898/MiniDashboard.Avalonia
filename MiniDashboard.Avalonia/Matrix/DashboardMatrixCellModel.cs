using System;

namespace MiniDashboard.Avalonia.Matrix;

/// <summary>A persistent cell assignment, independent of any instantiated control.</summary>
public sealed record DashboardMatrixCellModel(Guid Id, int Row, int Column, string? ContentId = null);
