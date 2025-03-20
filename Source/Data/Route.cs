using System;
using System.Collections.Generic;

namespace Celeste.Mod.MacroRoutingTool.Data;

public class Route : MRTExport {
    /// <summary>
    /// Unique ID for this route.
    /// </summary>
    public Guid ID;

    /// <summary>
    /// ID of the graph this route is assigned to, if any.
    /// </summary>
    public Guid GraphID;

    /// <summary>
    /// IDs of points this route visits, in order.
    /// </summary>
    public List<uint> Points;

    /// <summary>
    /// Tries to parse a given string of <seealso href="https://yaml.org/spec/1.2.2/#chapter-2-language-overview">YAML</seealso>-compliant
    /// text into a <see cref="Route"/>. Returns whether the parse was successful. 
    /// </summary>
    public static bool TryParse(string yaml, out Route route) {
        try {
            route = Reader.Deserialize<Route>(yaml);
        } catch (Exception e) {
            Logger.Error("MacroRoutingTool/YAML", $"{e.Message}\n{e.StackTrace}");
            route = null;
        }
        return route == null;
    }
}