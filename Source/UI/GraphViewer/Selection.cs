using System.Collections.Generic;
using Celeste.Mod.MacroRoutingTool.Data;

namespace Celeste.Mod.MacroRoutingTool.UI;

public static partial class GraphViewer {
    /// <summary>
    /// Contains references to each item type the <see cref="Selection"/> could contain. 
    /// </summary>
    public static class SelectionContents {
        /// <summary>
        /// The <see cref="Selection"/> currently contains <see cref="Point"/>s.  
        /// </summary>
        public const string Points = nameof(Points);
        /// <summary>
        /// The <see cref="Selection"/> currently contains <see cref="Connection"/>s. 
        /// </summary>
        public const string Connections = nameof(Connections);
    }

    /// <summary>
    /// List of items currently selected in the graph viewer.
    /// </summary>
    public static List<Traversable> Selection = [];
    /// <summary>
    /// The type of item currently in the <see cref="Selection"/>. 
    /// </summary>
    public static string SelectionHas;

    /// <summary>
    /// List of items currently hovered in the graph viewer.
    /// </summary>
    public static List<Traversable> Hovers = [];
    /// <summary>
    /// The type of item currently in the <see cref="Hovers"/>. 
    /// </summary>
    public static string HoversHas;

    /// <summary>
    /// Maximum distance between the cursor and a point for that point to be hoverable over, <inheritdoc cref="XMLDoc.Unit_PxAtTargetRes"/>.
    /// </summary>
    public static float PointHoverDistance = 40;

    /// <summary>
    /// Maximum distance between the cursor and a connection for that connection to be hoverable over, <inheritdoc cref="XMLDoc.Unit_PxAtTargetRes"/>.
    /// </summary>
    public static float ConnectionHoverDistance = 40;

    public static void UpdateHover() {
        Hovers.Clear();
        Traversable closest = null;
        float closestDistance = float.MaxValue;
        foreach (var point in Graph.Points) {
            float distance = point.HoverCheck();
            if (!float.IsNaN(distance) && distance < closestDistance) {
                closest = point;
                closestDistance = distance;
            }
        }
        foreach (var conn in Graph.Connections) {
            float distance = conn.HoverCheck();
            if (!float.IsNaN(distance) && distance < closestDistance) {
                closest = conn;
                closestDistance = distance;
            }
        }
        if (closest != null) {
            Hovers.Add(closest);
        }
    }
}