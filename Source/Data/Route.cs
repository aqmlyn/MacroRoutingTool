using System;
using System.Collections.Generic;

namespace Celeste.Mod.MacroRoutingTool.Data;

public class Route : MRTExport<Route> {
    /// <summary>
    /// ID of the graph this route is assigned to, if any.
    /// </summary>
    public Guid GraphID;

    /// <summary>
    /// Point this route starts at.
    /// </summary>
    public Point Start;

    /// <summary>
    /// Connections this route traverses along, in order.
    /// </summary>
    public List<Connection> Connections;
}