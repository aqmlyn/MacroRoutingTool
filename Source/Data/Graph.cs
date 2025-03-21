using System;
using System.Collections.Generic;
using Celeste.Mod.MacroRoutingTool.Logic;

namespace Celeste.Mod.MacroRoutingTool.Data;

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
    /// Requirement that must be met to traverse to or along this item.
    /// </summary>
    public NumericExpression Requirement;

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

  #region Viewer display
    /// <summary>
    /// Name displayed when this graph is shown in the viewer.
    /// </summary>
    public string Name = "";
  #endregion

    /// <summary>
    /// Tries to parse a given string of <seealso href="https://yaml.org/spec/1.2.2/#chapter-2-language-overview">YAML</seealso>-conformant
    /// text into a <see cref="Graph"/>. Returns whether the parse was successful.
    /// </summary>
    public static bool TryParse(string yaml, out Graph graph) {
        try {
            graph = Reader.Deserialize<Graph>(yaml);
        } catch (Exception e) {
            Logger.Error("MacroRoutingTool/YAML", $"{e.Message}\n{e.StackTrace}");
            graph = null;
        }
        return graph == null;
    }
}