using System.Collections.Generic;

namespace DemoApp.Models;

public sealed class DerivedMetricTileViewModel : DynamicTileViewModel
{
    public IReadOnlyList<double> Series { get; init; } = [];
}
