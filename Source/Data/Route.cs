using System;
using System.Collections.Generic;

namespace Celeste.Mod.MacroRoutingTool.Data;

public class Route : MRTExport {
    /// <summary>
    /// List of currently loaded routes.
    /// </summary>
    public static List<Route> List = [];

    /// <summary>
    /// ID of the graph this route is assigned to, if any.
    /// </summary>
    public Guid GraphID;

    /// <summary>
    /// IDs of points this route visits, in order.
    /// </summary>
    public List<uint> Points;
}