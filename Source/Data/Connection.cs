using System.Collections.Generic;
using Celeste.Editor;
using Celeste.Mod.MacroRoutingTool.UI;
using Microsoft.Xna.Framework;
using Monocle;
using YamlDotNet.Serialization;

namespace Celeste.Mod.MacroRoutingTool.Data;

//TODO YAML serialization

public class Connection : Traversable {
  #region Graph structure
    /// <summary>
    /// ID of the <c cref="Point">Point</c> this connection comes from. 
    /// </summary>
    public uint From;

    /// <summary>
    /// ID of the <c cref="Point">Point</c> this connection goes to. 
    /// </summary>
    public uint To;
  #endregion
    
  #region Viewer display

    /// <summary>
    /// Whether this connection is currently being displayed in the graph viewer.
    /// </summary>
    [YamlIgnore]
    public bool Visible;

    //TODO line colors, styles (e.g. dashed vs solid), shapes (e.g. points to curve through)

    public override float HoverPointCheck()
    {
        if (!Visible || GraphViewer.Graph.Connections.Contains(this)) {return float.NaN;}
        Camera camera = (Camera)typeof(MapEditor).GetField(nameof(MapEditor.Camera), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).GetValue(GraphViewer.DebugMap);
        Point from = GraphViewer.Graph.Points[(int)From];
        Point to = GraphViewer.Graph.Points[(int)To];
        float distance = MInput.Mouse.Position.DistanceToLineSegment(new Vector2(from.X, from.Y).OnCamera(camera), new Vector2(to.X, to.Y).OnCamera(camera));
        return distance > GraphViewer.ConnectionHoverDistance ? float.NaN : distance;
    }

    public override bool HoverRectCheck()
    {
        //TODO
        return false;
    }
  #endregion

    public static Dictionary<string, Connection> FastTravel;
}