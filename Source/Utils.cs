using System.Collections.Generic;

namespace Celeste.Mod.MacroRoutingTool;

public static class Utils {
    public static int BoolToInt(bool val) => val ? 1 : 0;

    public static TValue Ensure<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, TValue fallback = default) {
        if (!dict.TryGetValue(key, out TValue result)) {result = fallback;}
        return result;
    }
}