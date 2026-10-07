using System;

namespace MiniDashboard.Avalonia.Content;

/// <summary>Context for provisional content creation. A cell is a location, not an instance identity.</summary>
/// <param name="InstanceId">Fresh identity for this creation attempt, preserved by the live instance during moves.</param>
/// <param name="ContentId">Stable catalog definition ID.</param>
/// <param name="CellId">Creation cell ID in a matrix, or null outside a matrix. Not the instance's permanent location.</param>
/// <param name="State">Opaque application restoration state from ContentStateProvider, or null.</param>
public sealed record DashboardContentCreationContext(Guid InstanceId, string ContentId, Guid? CellId, object? State);
