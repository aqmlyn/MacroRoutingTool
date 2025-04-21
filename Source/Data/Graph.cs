using System.Collections.Generic;
using Celeste.Mod.MacroRoutingTool.Logic;

namespace Celeste.Mod.MacroRoutingTool.Data;

/// <summary>
/// A graph component that can be traversed to or along.
/// </summary>
public abstract class Traversable {
    /// <summary>
    /// Internal value that uniquely identifies this traversable item among those in its graph.
    /// </summary>
    public uint ID;

    /// <summary>
    /// Arbitrary data associated with this traversable item.
    /// </summary>
    public Dictionary<string, object> Data;

    /// <summary>
    /// Associates a weight value to each of any number of categories. For example, each of a graph's traversable items might have both "Time" and "Dashes" weights.
    /// </summary>
    public Dictionary<string, object> Weight;

    /// <summary>
    /// Requirement that must be met to traverse to or along this item.
    /// </summary>
    public NumericExpression Requirement;

    /// <summary>
    /// Each key is the name of a variable in <c cref="NumericExpression.Variables">NumericExpression.Variables</c>, and
    /// each value is a <c cref="NumericExpression">NumericExpression</c> whose result will be assigned to the variable when this item is traversed to or along.
    /// </summary>
    public Dictionary<string, NumericExpression> Results;

    /// <summary>
    /// Determines whether the cursor is hovering over this item. If not, returns NaN.
    /// If so, returns the distance from the cursor to this item, intended so that if the user is hovering over multiple items and
    /// isn't multi-selecting, only the closest item to the cursor will be hovered.
    /// </summary>
    public abstract float HoverPointCheck();

    /// <summary>
    /// Returns whether this item is currently inside any part of the selection rectangle
    /// created by left clicking and dragging in the graph viewer.
    /// </summary>
    public abstract bool HoverRectCheck();
}

public class Graph : MRTExport<Graph> {
  #region Graph structure
    /// <summary>
    /// The points in this graph.
    /// </summary>
    public List<Point> Points = [];

    /// <summary>
    /// The connections in this graph.
    /// </summary>
    public List<Connection> Connections = [];
  #endregion

    /// <summary>
    /// Data associated with the Celeste chapter this graph is assigned to.
    /// </summary>
    public AreaData Area;

    /// <summary>
    /// Index of the side of the chapter this graph is assigned to.
    /// </summary>
    public int Side;
}