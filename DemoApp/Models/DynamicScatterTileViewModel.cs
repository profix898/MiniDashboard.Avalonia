using System.Collections.Generic;

namespace DemoApp.Models;

public sealed class DynamicScatterTileViewModel : DynamicTileViewModel
{
    public IReadOnlyList<double> Series { get; init; } = [];
}
