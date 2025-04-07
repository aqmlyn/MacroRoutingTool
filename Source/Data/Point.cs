using System.Collections.Generic;
using Celeste.Mod.MacroRoutingTool.UI;
using Microsoft.Xna.Framework;
using Monocle;
using YamlDotNet.Serialization;

namespace Celeste.Mod.MacroRoutingTool.Data;

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

    [YamlIgnore]
    public int _x;
    /// <summary>
    /// X position at which this point is displayed.
    /// </summary>
    public int X {
        get => _x;
        set {
            _x = value;
            TextElement.Position.X = value;
            TextureElement.Position.X = value;
        }
    }

    [YamlIgnore]
    public int _y;
    /// <summary>
    /// Y position at which this point is displayed.
    /// </summary>
    public int Y {
        get => _y;
        set {
            _y = value;
            TextElement.Position.Y = value;
            TextureElement.Position.Y = value;
        }
    }

    [YamlIgnore]
    public string _image;
    /// <summary>
    /// Path to the image displayed when this point is shown.
    /// </summary>
    public string Image {
        get => _image;
        set {
            _image = value;
            if (UIHelpers.TryGetTexture(value, out MTexture texture)) {
                TextureElement.Texture = texture;
                TextureElement.Scale = Vector2.One * (TextureElement.Texture.Atlas == GFX.Game ? 6f : 1f);
            } else {
                //TODO log warning
            }
        }
    }

    [YamlIgnore]
    public UIHelpers.TextureElement TextureElement = new() {
        Justify = Vector2.One * 0.5f,
        IgnoreCameraZoom = true,
        BorderThickness = 3f
    };

    [YamlIgnore]
    public UIHelpers.TextElement TextElement = new() {
        Justify = new Vector2(0.5f, 1f),
        IgnoreCameraZoom = true,
        BorderThickness = 3f
    };
  #endregion
}