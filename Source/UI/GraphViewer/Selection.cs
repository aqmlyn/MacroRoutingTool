using System;
using System.Collections.Generic;
using System.Linq;
using Celeste.Mod.MacroRoutingTool.Data;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.MacroRoutingTool.UI;

public static partial class GraphViewer {
    /// <summary>
    /// Contains references to each item type a collection such as the <see cref="Selection"/> could contain. 
    /// </summary>
    public static class SelectionContents {
        /// <summary>
        /// The collection currently contains only <see cref="Point"/>s.  
        /// </summary>
        public const string Points = nameof(Points);
        /// <summary>
        /// The collection currently contains only <see cref="Connection"/>s. 
        /// </summary>
        public const string Connections = nameof(Connections);
        /// <summary>
        /// The collection currently contains <see cref="Traversable"/>s of any type. 
        /// </summary>
        public const string Traversables = nameof(Traversables);
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

    /// <summary>
    /// Runs each frame in <see cref="Editor.MapEditor.MouseModes.Hover"/>  to determine whether the user is
    /// currently hovering over any items in the graph viewer.
    /// </summary>
    public static void UpdateHover() {
        Hovers.Clear();
        HoversHas = null;

        //do not allow hovering over items behind MRT menu
        if (MInput.Mouse.Position.X < MenuWidth + MarginH) {return;}

        Traversable closest = null;
        float closestDistance = float.MaxValue;

        //check points
        foreach (var point in Graph.Points) {
            float distance = point.HoverPointCheck();
            if (!float.IsNaN(distance) && distance < closestDistance) {
                closest = point;
                closestDistance = distance;
                HoversHas = SelectionContents.Points;
            }
        }

        //check connections
        foreach (var conn in Graph.Connections) {
            float distance = conn.HoverPointCheck();
            if (!float.IsNaN(distance) && distance < closestDistance) {
                closest = conn;
                closestDistance = distance;
                HoversHas = SelectionContents.Connections;
            }
        }

        //record the closest hovered item found, if any
        if (closest != null) {
            Hovers.Add(closest);
        }
    }

    /// <summary>
    /// Runs each frame in <see cref="Editor.MapEditor.MouseModes.Select"/> to determine whether any items
    /// in the map editor are currently within the selection rectangle created by clicking and dragging.
    /// </summary>
    public static void UpdateHoverRect() {
        Hovers.Clear();
        HoversHas = null;

        Hovers.AddRange(Graph.Points.Where(point => point.HoverRectCheck()));
        Hovers.AddRange(Graph.Connections.Where(conn => conn.HoverRectCheck()));

        //determine what the hovers now contains
        bool points = false;
        bool connections = false;
        foreach (var item in Hovers) {
            if (item is Data.Point) {points = true;}
            else if (item is Data.Connection) {connections = true;}
            if (points && connections) {break;}
        }
        HoversHas =
            points && connections ? SelectionContents.Traversables
          : points ? SelectionContents.Points
          : connections ? SelectionContents.Connections
          : null;
    }

    public static void ReleaseHoverRect(ref bool isCommit) {
        isCommit = true;

        //replace the selection with the hovers
        Selection.Clear();
        Selection.AddRange(Hovers);
        SelectionHas = HoversHas;
        Hovers.Clear();
        HoversHas = null;
    }

    public static void StartDrag(ref bool start) {
        if (Hovers.Any(item => !Selection.Contains(item))) {
            Selection.Clear();
            SelectionHas = null;
        }
        if (Hovers.Count == 0) {DebugMap.mouseMode = Editor.MapEditor.MouseModes.Select;}
        start = true;
    }

    public static Dictionary<Func<bool>, Action> StartMoveDecisions = new() {
        {() => SelectionHas == SelectionContents.Points, StartMovingPoints}
    };

    public static void DragCheckStartMove(ref bool allow) {
        allow = Selection.Count > 1 && Hovers.Count != 0 && Hovers.Any(Selection.Contains);
        if (allow) {
            Hovers.Clear();
            HoversHas = null;
        } else {
            allow = Hovers.Count != 0;
            if (allow) {
                Selection.Clear();
                SelectionHas = HoversHas;
                Selection.AddRange(Hovers);
            }
        }
        if (allow) {
            StartMoveDecisions.FirstOrDefault(entry => entry.Key(), default).Value?.Invoke();
        }
    }

    public static void StartMovingPoints() {
        PointDragOrigPos.Clear();
        foreach (var point in Selection.ConvertAll(item => (Data.Point)item)) {
            PointDragOrigPos.Add(new Vector2(point.X, point.Y));
        }
    }

    public static Dictionary<Func<bool>, Action> MoveDecisions = new() {
        {() => SelectionHas == SelectionContents.Points, WhileMovingPoints}
    };

    public static void WhileMovingAny() {
        MoveDecisions.FirstOrDefault(entry => entry.Key(), default).Value?.Invoke();
    }

    public static List<Vector2> PointDragOrigPos = [];

    public static void WhileMovingPoints() {
        Vector2 drag = DebugMap.mousePosition - DebugMap.mouseDragStart;
        for (int i = 0; i < Selection.Count; i++) {
            Data.Point point = (Data.Point)Selection[i];
            point.X = (int)Math.Round(PointDragOrigPos[i].X + drag.X);
            point.Y = (int)Math.Round(PointDragOrigPos[i].Y + drag.Y);
        }
    }
}