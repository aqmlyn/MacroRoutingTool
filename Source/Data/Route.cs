using System.Collections.Generic;

namespace Celeste.Mod.MacroRoutingTool.Data;

//TODO YAML serialization

public class Route {
    /// <summary>
    /// The graph this route is assigned to, if any.
    /// </summary>
    public Graph Graph;

    /// <summary>
    /// IDs of points this route visits in order.
    /// </summary>
    public List<uint> Points;

    /// <summary>
    /// Tries to parse a given string of <seealso href="https://yaml.org/spec/1.2.2/#chapter-2-language-overview">YAML</seealso>-compliant
    /// text into a <see cref="Route"/>. Returns whether the parse was successful. 
    /// </summary>
    public static bool TryParse(string yaml, out Route route) {
        //TODO
        route = new();
        return true;
    }
}