using System;
using System.Collections.Generic;
using System.Reflection;
using Celeste.Editor;
using Microsoft.Xna.Framework.Input;
using Monocle;

namespace Celeste.Mod.MacroRoutingTool.UI;

public static partial class DebugMapHooks {
    public enum MouseEventTypes {
        Press,
        Release,
        Move
    }
    public enum ButtonEventTypes {
        Press,
        Release,
        Hold
    }
    public class Event {
        public GetterEventProperty<bool> Condition = new(){Value = false};
        public Action<MapEditor> Action;
    }
    public static Dictionary<string, Event> InputEvents = [];

    public static void CallInputEvents(MapEditor debugMap) {
        foreach (var inputEvent in InputEvents.Values) {
            if (inputEvent.Condition.Value) {inputEvent.Action?.Invoke(debugMap);}
        }
    }

    public static Func<object, bool> ReadInputProperty(string name) {
        PropertyInfo modBindProperty = typeof(ButtonBinding).GetProperty(name);
        PropertyInfo baseBindProperty = typeof(VirtualButton).GetProperty(name);
        MethodInfo keyMethod = typeof(MInput.KeyboardData).GetMethod(name, BindingFlags.Public | BindingFlags.Static, [typeof(Keys)]);
        MethodInfo buttonMethod = typeof(MInput.GamePadData).GetMethod(name, BindingFlags.Public | BindingFlags.Static, [typeof(Buttons), typeof(float)]);
        return input => {
            if (input is ButtonBinding modBind) {
                return (bool)modBindProperty.GetValue(modBind);
            }
            if (input is VirtualButton baseBind) {
                return (bool)baseBindProperty.GetValue(baseBind);
            }
            if (input is Keys key) {
                return (bool)keyMethod.Invoke(null, [key, 0.2f]);
            }
            if (input is Buttons button) {
                return (bool)buttonMethod.Invoke(null, [button, 0.2f]);
            }
            throw new InvalidCastException($"{nameof(ReadInputProperty)} received an object of type {input.GetType()}. Expected {nameof(ButtonBinding)}, {nameof(VirtualButton)}, {nameof(Keys)}, or {nameof(Buttons)}.");
        };
    }

    public static Func<object, bool> Pressed = ReadInputProperty(nameof(Pressed));
    public static Func<object, bool> Check = ReadInputProperty(nameof(Check));
    public static Func<object, bool> Released = ReadInputProperty(nameof(Released));

    public static Func<bool> Holding(params object[] binds) => () => {
        foreach (object bind in binds) {if (!Check(bind)) {return false;}}
        return true;
    };
}