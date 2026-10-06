using System.Collections.Generic;

namespace DemoApp.Models;

public sealed class DynamicServicesTileViewModel : DynamicTileViewModel
{
    public IReadOnlyList<DashboardTableRow> Rows { get; init; } = [];
}
