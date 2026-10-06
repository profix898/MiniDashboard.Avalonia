using System.Collections.Generic;

namespace DemoApp.Models;

public sealed class DynamicTableTileViewModel : DynamicTileViewModel
{
    public IReadOnlyList<DashboardTableRow> Rows { get; init; } = [];
}
