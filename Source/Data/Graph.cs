using System;
using System.Collections.Generic;
using Celeste.Mod.MacroRoutingTool.Logic;

namespace Celeste.Mod.MacroRoutingTool.Data;

//TODO YAML serialization
//NumericExpression => NumericExpression.Source -- used by Traversable.Requirements and Traversable.Results
//AreaData => AreaData.SID, AreaData.Mode -- used by Graph.Area
//Guid => Guid.ToString() -- used by Graph.ID, Route.ID, Route.GraphID

/// <summary>
/// A graph component that can be traversed to or along.
/// </summary>
public class Traversable {
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
    /// Single requirement or list of requirements that must be met to traverse to or along this item.
    /// </summary>
    public IRequirement Requirements;

    /// <summary>
    /// Each key is the name of a variable in <c cref="NumericExpression.Variables">NumericExpression.Variables</c>, and
    /// each value is a <c cref="NumericExpression">NumericExpression</c> whose result will be assigned to the variable when this item is traversed to or along.
    /// </summary>
    public Dictionary<string, NumericExpression> Results;
}

public class Graph : MRTExport {
    /// <summary>
    /// Unique ID for this graph.
    /// </summary>
    public Guid ID;

  #region Graph structure
    /// <summary>
    /// The points in this graph.
    /// </summary>
    public List<Point> Points;

    /// <summary>
    /// The connections in this graph.
    /// </summary>
    public List<Connection> Connections;
  #endregion

    /// <summary>
    /// Data associated with the Celeste map this graph is assigned to.
    /// </summary>
    public AreaData Area;

  #region Viewer display
    /// <summary>
    /// Name displayed when this graph is shown in the viewer.
    /// </summary>
    public string Name;
  #endregion

    /// <summary>
    /// Tries to parse a given string of <seealso href="https://yaml.org/spec/1.2.2/#chapter-2-language-overview">YAML</seealso>-conformant
    /// text into a <see cref="Graph"/>. Returns whether the parse was successful.
    /// </summary>
    public static bool TryParse(string yaml, out Graph graph) {
        //TODO
        graph = new();
        return true;
    }
}