using System;
using System.Collections.Generic;
using System.Linq;

namespace Celeste.Mod.MacroRoutingTool.UI;

public abstract class MemberEditor {
    public Func<List<object>> _itemsToEdit;
    /// <summary>
    /// Function that returns instances of <typeparamref name="TMenuItem"/> of which this menu item edits a member of.
    /// </summary>
    public Func<List<object>> ItemsToEdit {
        get => _itemsToEdit;
        set {
            _itemsToEdit = value;
            PopulateItemOrigValues();
        }
    }

    /// <summary>
    /// Values returned by <see cref="OnGet"/> for each item returned by <see cref="ItemsToEdit"/>.
    /// Automatically updates when either of those fields are changed. 
    /// </summary>
    public Dictionary<object, object> ItemOrigValues = [];

    public void PopulateItemOrigValues() {
        ItemOrigValues.Clear();
        var items = ItemsToEdit?.Invoke();
        if (items == null) {return;}
        var fieldGetter = OnGet?.Invoke();
        if (fieldGetter == null) {return;}
        foreach (var item in items) {
            ItemOrigValues.Add(item, fieldGetter(item));
        }
    }

    public virtual object GetInitialValue() {
        var fieldGetter = OnGet?.Invoke();
        if (fieldGetter == null) {return null;}
        var values = ItemOrigValues.DistinctBy(entry => entry.Value);
        if (values.Count() == 1) {return values.First().Value;}
        return null;
    }

    public Func<Func<object, object>> _onget;
    /// <summary>
    /// Returns a function that gets the value of the instance member this menu item is to edit.
    /// </summary>
    public Func<Func<object, object>> OnGet {
        get => _onget;
        set {
            _onget = value;
            PopulateItemOrigValues();
        }
    }

    public Func<Action<object, object>> _onset;
    /// <summary>
    /// Returns a function that sets the value of the instance member this menu item is to edit.
    /// </summary>
    public Func<Action<object, object>> OnSet {
        get => _onset;
        set {
            _onset = value;
            PopulateItemOrigValues();
        }
    }

    /// <summary>
    /// Function that returns the label to show for this menu item.
    /// </summary>
    public Func<string> LabelGetter;

    public class Option<TValue> : MemberEditor<MultiDisplayData.TextMenuOption<TValue>> {
        public Dictionary<string, TValue> LabeledOptions = [];

        public List<TValue> Options = [];

        public Func<TValue, string> OptionLabeller = val => val.ToString();

        public override MultiDisplayData.TextMenuOption<TValue> Create(object init = default)
        {
            //initialize menu item
            MultiDisplayData.TextMenuOption<TValue> option = new(LabelGetter?.Invoke() ?? "") {
                OnValueChange = val => {
                    foreach (var item in ItemsToEdit?.Invoke() ?? []) {
                        OnSet?.Invoke()?.Invoke(item, (object)val == (object)default(TValue) ? ItemOrigValues[item] : val);
                    }
                }
            };
            //initialize options
            if (LabeledOptions.Count == 0) {
                foreach (var val in Options) {
                    LabeledOptions.Add(OptionLabeller?.Invoke(val), val);
                }
            } else {
                Options = [.. LabeledOptions.Values];
            }
            //add options to the item's option list
            option.Add("-", default);
            foreach (var entry in LabeledOptions) {
                option.Add(entry.Key, entry.Value);
            }
            //initially select an option
            var valueGetter = OnGet?.Invoke();
            if (valueGetter != null) {
                object initObj = init == default ? GetInitialValue() : init;
                //use object.Equals instead of == with objects that have been cast to `object`
                option.Index = Math.Max(0, option.Values.FindIndex(entry => Equals(entry.Item2, initObj)));
            }
            return option;
        }
    }
}

public abstract class MemberEditor<TMenuItem> : MemberEditor where TMenuItem : TextMenu.Item {
    /// <summary>
    /// Creates and returns a <typeparamref name="TMenuItem"/> that edits a given member on each object in a given list
    /// in accordance with this <see cref="MemberEditor"/>'s configuration.
    /// </summary>
    /// <param name="init">Initial value to populate the <typeparamref name="TMenuItem"/> with.</param>
    public abstract TMenuItem Create(object init = default);
}