using System.Collections.Generic;
using Celeste.Mod.MacroRoutingTool.UI;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.MacroRoutingTool.Data;

//TODO YAML serialization

public class Point : Traversable {
  #region Graph structure
    public static class EndType {
      /// <summary>
      /// This point is not an endpoint.
      /// </summary>
      public const string None = nameof(EndType) + nameof(None);

      /// <summary>
      /// This point is an endpoint at which the player can start.
      /// </summary>
      public const string Start = nameof(EndType) + nameof(Start);

      /// <summary>
      /// This point is an endpoint at which the player can finish.
      /// </summary>
      public const string Finish = nameof(EndType) + nameof(Finish);
    }

    /// <summary>
    /// Names of systems through which the player can fast travel to this point in-game.
    /// </summary>
    public List<string> FastTravel;
  #endregion

  #region Viewer display
    /// <summary>
    /// Text displayed for this point when shown in the graph viewer and editor.
    /// </summary>
    public string Name;

    /// <summary>
    /// X position at which this point is displayed.
    /// </summary>
    public int X;

    /// <summary>
    /// Y position at which this point is displayed.
    /// </summary>
    public int Y;

    /// <summary>
    /// Path to the image displayed when this point is shown.
    /// </summary>
    public string Image = UIHelpers.AtlasPaths.Gui + "dot"; //TODO allow this to be changed, ideally with like an image picker
  #endregion
}