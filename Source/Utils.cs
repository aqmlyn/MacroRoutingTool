using System;
using System.Collections.Generic;
using System.Linq;

namespace Celeste.Mod.MacroRoutingTool;

public static class Utils {
    public static int BoolToInt(bool val) => val ? 1 : 0;

    public static TValue Ensure<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, TValue fallback = default) {
        if (!dict.TryGetValue(key, out TValue result)) {result = fallback;}
        return result;
    }

    /// <summary>
    /// An alternative to <see cref="AreaKey"/> used for <see href="https://gamebanana.com/mods/166210">AltSidesHelper</see> support
    /// and to make it easier to ignore the <see cref="AreaKey.ID"/>.
    /// </summary>
    public class MapSide {
        /// <summary>
        /// SID of the chapter this map is a side of.
        /// </summary>
        public string SID;
        /// <summary>
        /// Side index of this map. Usually an <see cref="AreaMode"/> cast to an integer.<br/>
        /// If the chapter uses <see href="https://gamebanana.com/mods/166210">AltSidesHelper</see>, this is instead the index of this side
        /// in the <see cref="AltSidesHelperMeta"/> assigned to the <see cref="AreaData"/> for this SID.
        /// </summary>
        public int Side;

        /// <summary>
        /// Attempts to create a new <see cref="MapSide"/> referencing the same side that the given <see cref="AreaKey"/> references.<br/>
        /// Returns whether the creation was successful.
        /// </summary>
        public static bool TryParse(AreaKey key, out MapSide side) {
            side = null;
            AreaData data = AreaData.Areas.FirstOrDefault(data => data.ToKey() == key, null);
            if (data == null) {return false;}
            side = new(){
                SID = data.IsOfficialLevelSet() ? $"Celeste/{data.Mode[0].MapData.Filename}" : key.SID,
                Side = (int)key.Mode
            };
            return true;
        }

        /// <summary>
        /// Attempts to create an <see cref="AreaKey"/> referencing the same side that this <see cref="MapSide"/> references.<br/>
        /// Returns whether the creation was successful.
        /// </summary>
        public bool TryMakeAreaKey(out AreaKey key) {
            key = AreaKey.Default;
            if (Side >= Enum.GetValues(typeof(AreaMode)).Length) {
                return false;
            }
            AreaData data = AreaData.Areas.FirstOrDefault(data => SID.StartsWith("Celeste/") ? data.IsOfficialLevelSet() : data.SID == SID, null);
            if (data == null) {return false;}
            key = data.ToKey((AreaMode)Side);
            return true;
        }
    }
}