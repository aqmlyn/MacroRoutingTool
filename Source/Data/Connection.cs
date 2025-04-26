using System;
using System.Collections.Generic;
using System.Linq;
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
    public Guid From;

    /// <summary>
    /// ID of the <c cref="Point">Point</c> this connection goes to. 
    /// </summary>
    public Guid To;
  #endregion
    
  #region Viewer display
    public static class VisibleTypes {
        /// <summary>
        /// This connection should always be visible.
        /// </summary>
        public const string Always = nameof(Always);
        /// <summary>
        /// This connection should only be visible if either itself, the point it comes <see cref="From"/>, or the point it goes <see cref="To"/> is selected.  
        /// </summary>
        public const string FromOrToSelected = nameof(FromOrToSelected);
        /// <summary>
        /// This connection should only be visible if either itself or the point it comes <see cref="From"/> is selected.
        /// </summary>
        public const string FromSelected = nameof(FromSelected);
    }

    public static Dictionary<string, Func<Connection, bool>> VisibleChecks = new(){
        {VisibleTypes.Always, conn => true},
        {VisibleTypes.FromOrToSelected, conn => GraphViewer.Selection.Contains(conn) || GraphViewer.Selection.Contains(GraphViewer.Graph.Points.First(pt => pt.ID == conn.From || pt.ID == conn.To))},
        {VisibleTypes.FromSelected, conn => GraphViewer.Selection.Contains(conn) || GraphViewer.Selection.Contains(GraphViewer.Graph.Points.First(pt => pt.ID == conn.From))}
    };

    /// <summary>
    /// Circumstances defined in <see cref="VisibleChecks"/> under which this connection should be displayed in the graph viewer.
    /// </summary>
    public string VisibleWhen = VisibleTypes.Always;

    /// <summary>
    /// Whether this connection is currently being displayed in the graph viewer.
    /// </summary>
    [YamlIgnore]
    public bool Visible;

    //TODO line colors, styles (e.g. dashed vs solid), shapes (e.g. points to curve through)

    public override float HoverPointCheck()
    {
        if (!Visible || !GraphViewer.Graph.Connections.Contains(this)) {return float.NaN;}
        Camera camera = (Camera)typeof(MapEditor).GetField(nameof(MapEditor.Camera), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).GetValue(GraphViewer.DebugMap);
        Point from = GraphViewer.Graph.Points.FirstOrDefault(pt => pt.ID == From, null);
        Point to = GraphViewer.Graph.Points.FirstOrDefault(pt => pt.ID == To, null);
        float distance = GraphViewer.DebugMap.mousePosition.DistanceToLineSegment(new Vector2(from.X, from.Y), new Vector2(to.X, to.Y)) * camera.Zoom;
        return distance > GraphViewer.ConnectionHoverDistance ? float.NaN : distance;
    }

    public override bool HoverRectCheck()
    {
        Point from = GraphViewer.Graph.Points.FirstOrDefault(pt => pt.ID == From, null);
        Point to = GraphViewer.Graph.Points.FirstOrDefault(pt => pt.ID == To, null);
        return Visible && GraphViewer.Graph.Connections.Contains(this) && Collide.RectToLine(GraphViewer.DebugMap.GetMouseRect(GraphViewer.DebugMap.mouseDragStart, GraphViewer.DebugMap.mousePosition), new Vector2(from.X, from.Y), new Vector2(to.X, to.Y));
    }
  #endregion

    public static Dictionary<string, Connection> FastTravel;
}