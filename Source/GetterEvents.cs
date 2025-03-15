namespace Celeste.Mod.MacroRoutingTool;

/// <summary>
/// Delegate type for the event handlers of a <c>GetterEventProperty</c>'s <c>Event</c>.
/// </summary>
/// <typeparam name="TValue">Value type of the property which this getter event is for.</typeparam>
/// <param name="initialValue">The initial value, passed by reference.</param>
public delegate void GetterEvent<TValue>(ref TValue initialValue);

/// <summary>
/// Object that wraps a property (held in this object's <c>Value</c>) whose getter is to have an event attached.<br/>
/// When this object's <c>Value</c>'s getter is called, this object's <c>Event</c> will be raised, allowing all event handlers to run and potentially modify the value before the getter returns it.
/// </summary>
/// <typeparam name="TValue">The property value's type.</typeparam>
public class GetterEventProperty<TValue> {
    //ok so a couple of things here:
    //1. if a variable's value is changed by one event handler, does the next event handler receive the new value or the original one?
    //   seems annoying to debug so i'll just pass the variable by reference to ensure later handles can get the newest value
    //2. if an event handler tries to get the value, won't the getter raise the event again, resulting in infinite recursion?
    //   we can add a short-circuit to the getter to prevent that

    /// <summary>
    /// The event to raise when <c>Value</c>'s getter is called.
    /// </summary>
    public GetterEvent<TValue> Event;

    /// <summary>
    /// If accessed from an event handler that was invoked by the event being raised, holds the value this property had just before the event was raised.<br/>
    /// Otherwise, holds the value this property had just before it was last set.
    /// </summary>
    public TValue PreviousValue = default;

    protected TValue _value = default;
    protected bool getting = false;
    protected virtual ref TValue GetValue() {
        if (getting || Event == null) {
            return ref _value;
        }
        getting = true;
        Event(ref _value);
        PreviousValue = _value;
        getting = false;
        return ref _value;
    }

    /// <summary>
    /// The property's value.<br/>
    /// Calling its getter will raise <c>Event</c>, allowing its handlers to run and potentially modify the value before the getter returns it.
    /// </summary>
    public TValue Value {
        get {return GetValue();}
        set {_value = value;}
    }
}

/// <summary>
/// Delegate type for the event handlers of a <c>GetterEventProperty</c>'s <c>Event</c>.
/// </summary>
/// <typeparam name="TValue">Value type of the property which this getter event is for.</typeparam>
/// <param name="initialValue">The initial value, passed by reference.</param>
/// <typeparam name="TArgs">Type of arguments object this getter will receive from the property it's for.</typeparam>
/// <param name="args">The arguments object passed from the property which this getter event is for.</param>
public delegate void GetterEvent<TValue, TArgs>(ref TValue initialValue, TArgs args);

/// <summary>
/// Object that wraps a property (held in this object's <c>Value</c>) whose getter is to have an event attached.<br/>
/// When this object's <c>Value</c>'s getter is called, this object's <c>Event</c> will be raised, allowing all event handlers to run and potentially modify the value before the getter returns it.
/// </summary>
/// <typeparam name="TValue">The property value's type.</typeparam>
public class GetterEventProperty<TValue, TArgs> : GetterEventProperty<TValue> {
    /// <summary>
    /// The event to raise when <c>Value</c>'s getter is called.
    /// </summary>
    public new GetterEvent<TValue, TArgs> Event;

    /// <summary>
    /// The arguments that the event will receive when called.
    /// </summary>
    public TArgs Arguments = (TArgs)typeof(TArgs).GetConstructor([]).Invoke(null);

    protected override ref TValue GetValue() {
        if (getting || Event == null) {
            return ref _value;
        }
        getting = true;
        Event(ref _value, Arguments);
        PreviousValue = _value;
        getting = false;
        return ref _value;
    }
}