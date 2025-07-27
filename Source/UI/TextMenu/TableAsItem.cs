using System;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.MacroRoutingTool.UI;

partial class TableMenu {
    /// <summary>
    /// Representation of a <see cref="TableMenu"/> as an item in another <see cref="TextMenu"/> 
    /// (which may or may not itself be a <see cref="TableMenu"/>). 
    /// </summary>
    public class AsItem : Item {
        /// <summary>
        /// The <see cref="TableMenu"/> this item represents.
        /// </summary>
        public TableMenu Menu;

        public Action OnNavigateUpFromTop;
        public Action OnNavigateDownFromBottom;

        /// <inheritdoc cref="AsItem"/> 
        public AsItem() {
            IncludeWidthInMeasurement = false;
        }

        /// <summary>
        /// Called when <see cref="Menu"/> is to gain control.
        /// </summary>
        public void GainFocus() {
            if (Menu != null) {
                SelectWiggler.StopAndClear(); //otherwise the entire table would wiggle
                if (Container != null) {
                    Container.Focused = false;
                    Container.RenderAsFocused = Menu.Items.Count == 0 || (!Menu.Items[0].Selectable && Menu.FirstPossibleSelection == 0);
                }
                Menu.Focused = true;
                Menu.FirstSelection();
                foreach (var row in Menu.Rows) {
                    row.HoverIndex = row.FirstPossibleHover;
                }
            }
        }

        /// <summary>
        /// Called when <see cref="Menu"/> is to lose control.
        /// </summary>
        public void LoseFocus() {
            if (Menu != null) {
                Menu.Focused = false;
            }
            if (Container != null) {
                Container.RenderAsFocused = false;
                Container.Focused = true;
            }
        }

        /// <summary>
        /// Determines whether this item can be hovered.
        /// </summary>
        public virtual bool SelectableCheck() => Menu.Items[0].Selectable || Menu.FirstPossibleSelection != 0;

        /// <summary>
        /// Called each frame while this item is hovered to determine whether its <see cref="Menu"/> is to gain control.
        /// </summary>
        public virtual bool EnterCheck() => true;

        public override void Added() {
            if (Menu != null) {
                Menu.DefaultNavigateUpFromTop = () => {
                    OnNavigateUpFromTop?.Invoke();
                    if (Input.MenuUp.Pressed) {
                        //if there is another item in Container to navigate to, including by wrapping to the opposite side, then navigate to it
                        bool shouldNavigate = false;
                        if (Container != null) {
                            shouldNavigate = true;
                            for (int i = Container.Items.Count - 1; i >= 0; i--) {
                                var item = Container.Items[i];
                                if (shouldNavigate) {
                                    if (item.Hoverable) {
                                        if (item == this) { shouldNavigate = false; }
                                        else { break; }
                                    }
                                } else {
                                    if (item.Hoverable) {
                                        shouldNavigate = true;
                                        break;
                                    }
                                }
                            }
                        }
                        if (shouldNavigate) {
                            Input.MenuUp.ConsumePress();
                            LoseFocus();
                            Container.MoveSelection(-1, true);
                        }
                    }
                };
                Menu.DefaultNavigateDownFromBottom = () => {
                    OnNavigateDownFromBottom?.Invoke();
                    if (Input.MenuDown.Pressed) {
                        //if there is another item in Container to navigate to, including by wrapping to the opposite side, then navigate to it
                        bool shouldNavigate = false;
                        if (Container != null) {
                            shouldNavigate = true;
                            for (int i = 0; i < Container.Items.Count; i++) {
                                var item = Container.Items[i];
                                if (shouldNavigate) {
                                    if (item.Hoverable) {
                                        if (item == this) { shouldNavigate = false; }
                                        else { break; }
                                    }
                                } else {
                                    if (item.Hoverable) {
                                        shouldNavigate = true;
                                        break;
                                    }
                                }
                            }
                        }
                        if (shouldNavigate) {
                            Input.MenuDown.ConsumePress();
                            LoseFocus();
                            Container.MoveSelection(1, true);
                        }
                    }
                };
            }
            base.Added();
        }

        public override float LeftWidth() => Menu.AllowShrinkWidth ? Math.Min(Menu.FullWidth, Menu.DisplayWidth) : Menu.DisplayWidth;
        public override float Height() => Menu.AllowShrinkHeight ? Math.Min(Menu.FullHeight, Menu.DisplayHeight) : Menu.DisplayHeight;
        public override void Render(Vector2 position, bool highlighted) {
            if (Menu != null) {
                Menu.Position.X = position.X + Menu.Width * Menu.Justify.X;
                Menu.Position.Y = position.Y + Menu.Height * (Menu.Justify.Y - 0.5f);
                Menu.Render();
            }
        }

        public override void Update() {
            if (Menu != null) {
                Selectable = SelectableCheck();
                if (!Menu.Focused && Container?.Current == this && EnterCheck()) {
                    GainFocus();
                    //vanilla checking for up/down doesn't consume them if found
                    if (Input.MenuUp.Pressed) { Input.MenuUp.ConsumePress(); }
                    if (Input.MenuDown.Pressed) { Input.MenuDown.ConsumePress(); }
                }
                Menu.Update();
            }
        }
    }

    /// <inheritdoc cref="AsItem"/>
    public class AsCollapsibleItem : AsItem {
        /// <summary>
        /// Whether this item is currently collapsed.
        /// </summary>
        public bool Collapsed;

        /// <summary>
        /// The <see cref="TextElement"/> shown above the table.
        /// </summary>
        public TextElement CollapserLabel = new(){Justify = new(0f, 0.5f), BorderThickness = 2f};

        /// <summary>
        /// Whether to automatically set <see cref="CollapserLabel"/>'s color based on whether
        /// this item is currently hovered.  
        /// </summary>
        public bool CollapserAutoColor = true;

        /// <summary>
        /// <see cref="TextureElement"/> for the arrow shown above the table.
        /// </summary>
        public TextureElement CollapserArrow = new() { Justify = new(0f, 0.5f), BorderThickness = 2f, Texture = GFX.Gui["downarrow"] };

        /// <summary>
        /// Distance between <see cref="CollapserLabel"/>'s right edge and <see cref="CollapserArrow"/>'s left edge.  
        /// </summary>
        public float CollapserArrowSpacing = 20f;

        public override bool SelectableCheck() => true;
        public override bool EnterCheck() => !Collapsed;
        public override float LeftWidth() => Math.Max(CollapserLabel.Measurements.Width + CollapserArrowSpacing + (CollapserArrow.Texture.Width * CollapserArrow.Scale.X), Collapsed ? 0f : base.LeftWidth());
        public override float Height() => CollapserLabel.Measurements.Height + (Collapsed ? 0f : ((Container?.ItemSpacing ?? 0f) + base.Height()));

        public override void ConfirmPressed() {
            if (Menu != null) {
                if (Collapsed) {
                    Input.MenuConfirm.ConsumePress();
                    Collapsed = false;
                    Menu.Visible = true;
                    GainFocus(); //even if there is nothing to hover, the table needs focused so that it can consume the cancel input to close it
                }
            }
            base.ConfirmPressed();
        }

        public override void Update() {
            base.Update();
            Selectable = SelectableCheck();
            CollapserLabel.Update();
            CollapserArrow.Update();
            if (Menu != null && !Collapsed) {
                if (Menu.Focused && Input.MenuCancel.Pressed) {
                    Input.MenuCancel.ConsumePress();
                    LoseFocus();
                    Menu.Visible = false;
                    Collapsed = true;
                    SelectWiggler.Start();
                    return;
                }
            }
        }

        public override void Render(Vector2 position, bool highlighted) {
            var origPosition = new Vector2(position.X, position.Y);
            var labelMsrmts = CollapserLabel.Measurements;
            position.Y = position.Y - Height() / 2f + labelMsrmts.Height / 2f;
            CollapserLabel.Position.X = position.X;
            CollapserLabel.Position.Y = CollapserArrow.Position.Y = position.Y;
            CollapserArrow.Position.X = position.X + labelMsrmts.Width + CollapserArrowSpacing * labelMsrmts.Scale.X;
            if (CollapserAutoColor) { CollapserLabel.Color = CollapserArrow.Color = highlighted ? Container.HighlightColor : Color.White; }
            CollapserLabel.Render();
            CollapserArrow.Render();
            if (!Collapsed && (Menu?.Visible ?? false)) {
                base.Render(new(origPosition.X, origPosition.Y + labelMsrmts.Height / 2f), highlighted);
            }
        }
    }

    /// <summary>
    /// Create a new <see cref="AsItem"/> containing this <see cref="TableMenu"/>, add it to the given <see cref="TextMenu"/>
    /// (which may or may not itself be a <see cref="TableMenu"/>), and return it.  
    /// </summary>
    public AsItem MakeSubmenuIn(TextMenu menu) {
        Focused = false;
        AsItem asItem = new(){Menu = this, Container = menu};
        menu.Add(asItem);
        return asItem;
    }

    /// <summary>
    /// Create a new <see cref="AsCollapsibleItem"/> containing this <see cref="TableMenu"/>, add it to the given <see cref="TextMenu"/>
    /// (which may or may not itself be a <see cref="TableMenu"/>), and return it.  
    /// </summary>
    public AsCollapsibleItem MakeSubmenuCollapsedIn(TextMenu menu) {
        Focused = false;
        AsCollapsibleItem asItem = new() { Menu = this, Container = menu, Collapsed = true };
        menu.Add(asItem);
        return asItem;
    }
}