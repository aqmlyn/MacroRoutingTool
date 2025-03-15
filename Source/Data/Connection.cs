using System.Collections.Generic;

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
    /// Text displayed for this connection when shown in the graph viewer and editor.
    /// </summary>
    public string Name;

    //TODO line colors, styles (e.g. dashed vs solid), shapes (e.g. points to curve through)
  #endregion

    public static Dictionary<string, Connection> FastTravel;
}