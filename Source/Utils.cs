using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Celeste.Mod.MacroRoutingTool;

public static class Utils {
    public static int BoolToInt(bool val) => val ? 1 : 0;

    /// <summary>
    /// If this dictionary has an entry with the given key, return the entry's value.<br/>
    /// Otherwise, add an entry with the given key and given fallback value, and return the fallback value.
    /// </summary>
    /// <typeparam name="TKey">Type of the dictionary's keys.</typeparam>
    /// <typeparam name="TValue">Type of the dictionary's values.</typeparam>
    /// <param name="dict">The dictionary an entry is to be found or created in.</param>
    /// <param name="key">The key to try finding a value for.</param>
    /// <param name="fallback">The value to insert and return if the key is not found.<br/>Optional, defaults to the type's <c>default</c> value.</param>
    public static TValue EnsureGet<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, TValue fallback = default) {
        if (dict.TryGetValue(key, out TValue result)) {
            return result;
        } else {
            dict.Add(key, fallback);
            return fallback;
        }
    }
    
    public static TValue EnsureGet<TValue>(this List<TValue> list, int index, Func<int, TValue> fallback = null) {
        if (index < 0) { throw new IndexOutOfRangeException($"Cannot use negative index to access a list item (received index {index})"); }
        if (index < list.Count) { return list[index]; }
        fallback ??= (_) => default;
        while (list.Count <= index) { list.Add(fallback(list.Count)); }
        return list[^1];
    }
    
    public static void EnsureSet<TValue>(this List<TValue> list, int index, TValue value, Func<int, TValue> fillempty = null) {
        if (index < 0) { throw new IndexOutOfRangeException($"Cannot use negative index to access a list item (received index {index})"); }
        if (index < list.Count) { list[index] = value; }
        fillempty ??= (_) => default;
        while (list.Count < index) { list.Add(fillempty(list.Count)); }
        list.Add(value);
    }

    /// <inheritdoc cref="MethodInfo.CreateDelegate(Type, object?)"/>
    public static Delegate CreateDelegate(this MethodInfo self, object instance = null) {
        //largely adapted from https://stackoverflow.com/a/74252965
        var delType = Expression.GetDelegateType([.. self.GetParameters().Select(param => param.ParameterType), self.ReturnType]);
        return self.IsStatic ? self.CreateDelegate(delType) : self.CreateDelegate(delType, instance);
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