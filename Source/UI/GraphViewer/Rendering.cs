using System;
using System.Linq;
using Celeste.Editor;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.MacroRoutingTool.UI;

public static partial class GraphViewer {
    /// <summary>
    /// The MRT menu's width, <inheritdoc cref="XMLDoc.Unit_PxAtTargetRes"/>.
    /// </summary>
    public const int MenuWidth = (int)(Celeste.TargetWidth * 0.25f);

    /// <summary>
    /// The opacity of the black background displayed behind the MRT menu.
    /// </summary>
    public const float MenuBackOpacity = 0.75f;

    /// <summary>
    /// The opacity of the black background displayed behind the graph while the viewer is enabled.
    /// </summary>
    public const float GraphBackOpacity = 0.25f;

    /// <summary>
    /// Width of the banner drawn behind the level name, <inheritdoc cref="XMLDoc.Unit_PxAtTargetRes"/>.
    /// </summary>
    public const int LevelBannerWidth = (int)(Celeste.TargetWidth * 0.375f);

    /// <summary>
    /// Size of the margins on the left and right sides of the level banner, <inheritdoc cref="XMLDoc.Unit_PxAtTargetRes"/>.<br/>
    /// Intended to prevent text from being drawn outside the banner.
    /// </summary>
    public const int LevelBannerMarginH = 16;

    /// <summary>
    /// The headbar's height, <inheritdoc cref="XMLDoc.Unit_PxAtTargetRes"/>, as hardcoded in the debug map's <c cref="MapEditor.Render">Render</c> method.<br/>
    /// (It seems this specific value was chosen to be able to draw the 64px Renogare font at scale 1 and leave a 4px margin above and below.)
    /// </summary>
    public static int HeadbarHeight => (int)(ActiveFont.FontSize.Size + MarginV * 2);

    /// <summary>
    /// The headbar's left and right margin, <inheritdoc cref="XMLDoc.Unit_PxAtTargetRes"/>, as hardcoded in the debug map's <c cref="MapEditor.Render">Render</c> method. 
    /// </summary>
    public const int MarginH = 16;

    /// <summary>
    /// The headbar's top margin, <inheritdoc cref="XMLDoc.Unit_PxAtTargetRes"/>, as hardcoded in the debug map's <c cref="MapEditor.Render">Render</c> method. 
    /// </summary>
    public const int MarginV = 4;

    //TODO allow changing point stuff
    public static float PointNameScale = 0.5f;
    public static Color PointColor = Color.White;
    public static Color PointHoveredColor = Color.Orange;
    public static Color PointSelectedColor = Color.Aqua;
    public static float PointNameMargin = 1.5f;

    /// <summary>
    /// Render all parts of the graph viewer that stay at a fixed position on the screen.
    /// </summary>
    public static void RenderFixed() {
        RenderMenuContainer();
        RenderHeadbar();
    }

    /// <summary>
    /// Render the graph viewer's headbar, which includes the MRT title and level/levelset information.
    /// </summary>
    public static void RenderHeadbar() {
        //TODO where possible, move image/text measuring to somewhere only called if Dialog.Language has changed since opening debug map.
        //should noticeably improve performance over measuring here, since this is called every frame

        //headbar back
        Draw.Rect(0f, 0f, Celeste.TargetWidth, HeadbarHeight, Color.Black);

        //MRT title
        string mrtTitle = MRTDialog.DebugMapTitle;
        Vector2 mrtTitleSize = ActiveFont.Measure(mrtTitle);
        float mrtTitleScale = Calc.Min(1, MenuWidth / mrtTitleSize.X, (HeadbarHeight - MarginV * 2) / mrtTitleSize.Y);
        ActiveFont.DrawEdgeOutline(
            mrtTitle, //text to draw
            new Vector2(MenuWidth / 2 + MarginH, MarginV), //position: origin to draw from
            new Vector2(0.5f, 0f), //justify: where the origin should be relative to the result (in this case, top center)
            Vector2.One * mrtTitleScale, //scale
            Color.Gray, //text color
            4, //drop shadow ("edge") offset
            Color.DarkSlateBlue, //drop shadow color
            1, //outline thickness
            Color.DarkSlateGray //outline color
        );

        //get map + levelset info
        string mapTitle = MRTDialog.Get(UIHelpers.GetAreaData().Name.DialogKeyify());
        string levelsetTitle = "";
        string levelsetSubtitle = "";
        if (false /*TODO CollabUtils2 support -- check if this map is in a lobby*/) {
    
        } else {
            //campaign
            bool multipleChapters = false;
            if (UIHelpers.GetAreaData().IsOfficialLevelSet()) {
                levelsetTitle = MRTDialog.Get("levelset_celeste");
                multipleChapters = true;
            } else if (UIHelpers.GetAreaData().LevelSet == "") {
                levelsetTitle = MRTDialog.Get("levelset_"); //"Uncategorized" in English
            } else {
                levelsetTitle = MRTDialog.Get(UIHelpers.GetAreaData().LevelSet.DialogKeyify());
                multipleChapters = AreaData.Areas.Count(ad => ad.SID.StartsWith(UIHelpers.GetAreaData().LevelSet)) > 1;
            }
            if (multipleChapters && !UIHelpers.GetAreaData().Interlude) {
                //don't show chapter number if the map is a standalone or interlude
                levelsetSubtitle = MRTDialog.Get("area_chapter", false).Replace("{x}", UIHelpers.GetAreaKey().ChapterIndex.ToString());
            }
        }

        MTexture subtitleBackTexture = GFX.Gui["strawberryCountBG"];

        //map title banner
        //using the actual texture (GFX.Gui["areaselect/title"]) is impractical because it's slightly angled and includes the shadow.
        //so we recreate our own banner instead! TODO i dont like it just being a rectangle but i have other priorities for now
        bool mapHasSubtitle = UIHelpers.GetAreaData().Mode.Length > 1 && UIHelpers.GetAreaKey().Mode != AreaMode.Normal;
        float levelBannerExtraHeight = 0f;
        if (mapHasSubtitle) {
            levelBannerExtraHeight = subtitleBackTexture.Height - MarginV * 2;
        }
        //main back
        Draw.Rect(Celeste.TargetCenter.X - LevelBannerWidth / 2, 0, LevelBannerWidth, HeadbarHeight + levelBannerExtraHeight + MarginV, UIHelpers.GetAreaData().TitleBaseColor);
        Draw.Rect(Celeste.TargetCenter.X - LevelBannerWidth / 2 - LevelBannerMarginH, 0, LevelBannerMarginH, HeadbarHeight + levelBannerExtraHeight + MarginV, UIHelpers.GetAreaData().TitleAccentColor);
        Draw.Rect(Celeste.TargetCenter.X + LevelBannerWidth / 2, 0, LevelBannerMarginH, HeadbarHeight + levelBannerExtraHeight + MarginV, UIHelpers.GetAreaData().TitleAccentColor);
        //borders
        Draw.Rect(Celeste.TargetCenter.X - LevelBannerWidth / 2 - LevelBannerMarginH - MarginV, HeadbarHeight + levelBannerExtraHeight + MarginV, LevelBannerWidth + LevelBannerMarginH * 2 + MarginV * 2, MarginV, Color.Black);
        Draw.Rect(Celeste.TargetCenter.X - LevelBannerWidth / 2 - LevelBannerMarginH - MarginV, 0, MarginV, HeadbarHeight + levelBannerExtraHeight + MarginV, Color.Black);
        Draw.Rect(Celeste.TargetCenter.X + LevelBannerWidth / 2 + LevelBannerMarginH, 0, MarginV, HeadbarHeight + levelBannerExtraHeight + MarginV, Color.Black);
        //text
        Vector2 mapTitleOrigSize = ActiveFont.Measure(mapTitle);
        float mapTitleScale = Math.Min((LevelBannerWidth - LevelBannerMarginH * 2) / mapTitleOrigSize.X, (HeadbarHeight - MarginV * 3) / mapTitleOrigSize.Y);
        ActiveFont.Draw(mapTitle, new Vector2(Celeste.TargetCenter.X, MarginV * 2), new Vector2(0.5f, 0f), Vector2.One * mapTitleScale, UIHelpers.GetAreaData().TitleTextColor);
        if (mapHasSubtitle) {
            string mapSubtitle = MRTDialog.ChapterSideTexts[UIHelpers.GetAreaKey().Mode]();
            ActiveFont.Draw(mapSubtitle, new Vector2(Celeste.TargetCenter.X, HeadbarHeight - MarginV), new Vector2(0.5f, 0f), Vector2.One * levelBannerExtraHeight / ActiveFont.Measure(mapSubtitle).Y, UIHelpers.GetAreaData().TitleAccentColor);
        }

        //levelset title
        Vector2 levelsetTitleOrigSize = ActiveFont.Measure(levelsetTitle);
        float levelsetTitleScale = Math.Min((Celeste.TargetCenter.X - LevelBannerWidth / 2 - LevelBannerMarginH * 3 - MarginV) / levelsetTitleOrigSize.X, ActiveFont.FontSize.Size / levelsetTitleOrigSize.Y);
        ActiveFont.Draw(levelsetTitle, new Vector2(Celeste.TargetWidth - 1 - MarginH, MarginV), Vector2.UnitX, Vector2.One * levelsetTitleScale, Color.White);

        //levelset subtitle (chapter # or lobby)
        if (levelsetSubtitle != "") {
            Vector2 levelsetSubtitleOrigSize = ActiveFont.Measure(levelsetSubtitle);
            float levelsetSubtitleScale = (subtitleBackTexture.Height - MarginV) / levelsetSubtitleOrigSize.Y;
            float subtitleBackSeamPos = Celeste.TargetWidth - 1 - MarginH * 3 - levelsetSubtitleOrigSize.X * levelsetSubtitleScale + subtitleBackTexture.Width;
            if (subtitleBackSeamPos < Celeste.TargetWidth) {
                Draw.Rect(subtitleBackSeamPos, HeadbarHeight, Celeste.TargetWidth - subtitleBackSeamPos, subtitleBackTexture.Height, Color.Black);
            }
            subtitleBackTexture.DrawJustified(new Vector2(subtitleBackSeamPos, HeadbarHeight), Vector2.UnitX, Color.White, Vector2.One, 0f, Microsoft.Xna.Framework.Graphics.SpriteEffects.FlipHorizontally);
            ActiveFont.Draw(levelsetSubtitle, new Vector2(Celeste.TargetWidth - MarginH, HeadbarHeight), Vector2.UnitX, Vector2.One * levelsetSubtitleScale, Color.LightGray);
        }
    }

    /// <summary>
    /// Render the menu bind display and the menu background.
    /// </summary>
    public static void RenderMenuContainer() {
        //TODO where possible, move image/text measuring to somewhere only called if Dialog.Language has changed since opening debug map.
        //should noticeably improve performance over measuring here, since this is called every frame

        if (Mode != (int)Modes.Disabled) {
            //menu back
            Draw.Rect(0, HeadbarHeight, MenuWidth + MarginH * 2, Celeste.TargetHeight - HeadbarHeight, Color.Black * MenuBackOpacity);

            //fade
            for (int i = 1; i < MarginH * 2; i++) {
                Draw.Line(MenuWidth + MarginH * 2 + i, HeadbarHeight + 1, MenuWidth + MarginH * 2 + i, Celeste.TargetHeight, Color.Black * (MenuBackOpacity - (MenuBackOpacity - GraphBackOpacity) * (i / ((float)MarginH * 2))));
            }

            //graph back
            //Draw.Rect(MenuWidth + MarginH * 4 - 1, HeadbarHeight, Celeste.TargetWidth - (MenuWidth + MarginH * 4 - 1), Celeste.TargetHeight - HeadbarHeight, Color.Black * GraphBackOpacity);
        }

        MTexture controlBackTexture = GFX.Gui["strawberryCountBG"];

        //take measurements to determine position and size of each UI element in the mod control display
        //(we need to do this now so we can make the back big enough to fit it all)
        //TODO decide what to do if bind display text will be drawn behind the map banner
        MTexture focusBindTexture = UIHelpers.DebugBindTexture(MRT.Settings.Bind_DebugFocusGraphMenu);
        float focusBindTextureScale = (float)(controlBackTexture.Height - MarginV) / focusBindTexture.Height;
        float bindLabelScale = (controlBackTexture.Height - MarginV) / ActiveFont.Measure("T").Y;
        float focusBindLabelLeft = focusBindTexture.Width * focusBindTextureScale + MarginH * 1.5f;
        MTexture modeBindTexture = UIHelpers.DebugBindTexture(MRT.Settings.Bind_DebugGraphViewerMode);
        float modeBindTextureScale = (float)(controlBackTexture.Height - MarginV) / modeBindTexture.Height;
        string modeLabel = MRTDialog.ViewerMode + " ";
        float modeLabelRelLeft = modeBindTexture.Width * modeBindTextureScale + MarginH * 0.5f;
        float modeTextRelLeft = modeLabelRelLeft + ActiveFont.Measure(modeLabel).X * bindLabelScale;
        float fullModeWidth = modeTextRelLeft + MRTDialog.ViewerModes.Values.Max(getter => ActiveFont.Measure(getter()).X) * bindLabelScale;
        float fullModeLeft = focusBindLabelLeft + MRTDialog.ViewerFocusStates.Values.Max(getter => ActiveFont.Measure(getter()).X) * bindLabelScale + MarginH * 2f;
        float controlDisplayWidth = Math.Max(fullModeLeft + fullModeWidth, MenuWidth + MarginH) + MarginH * 2;
        if (controlDisplayWidth == MenuWidth + MarginH * 3) {
            fullModeLeft = MenuWidth + MarginH - fullModeWidth;
        }

        //mod control back
        float controlBackSeamPos = controlDisplayWidth - controlBackTexture.Width;
        Draw.Rect(0, HeadbarHeight, controlBackSeamPos + 1, controlBackTexture.Height, Color.Black);
        controlBackTexture.Draw(new Vector2(controlBackSeamPos, HeadbarHeight));

        float bindLabelHeight = HeadbarHeight + (controlBackTexture.Height - MarginV) / 2;
        
        //focus menu bind
        Color focusBindColor = FocusBindEnabled.Value ? Color.White : Color.DarkSlateGray;
        focusBindTexture.Draw(new Vector2(MarginH, HeadbarHeight), Vector2.Zero, focusBindColor, focusBindTextureScale);
        ActiveFont.Draw(MRTDialog.ViewerFocusStates[InMenu](), new Vector2(focusBindLabelLeft, bindLabelHeight), new Vector2(0f, 0.5f), Vector2.One * bindLabelScale, focusBindColor);

        //mode
        Color modeBindColor = ModeBindEnabled.Value ? Color.White : Color.DarkSlateGray;
        modeBindTexture.Draw(new Vector2(fullModeLeft, HeadbarHeight), Vector2.Zero, modeBindColor, modeBindTextureScale);
        ActiveFont.Draw(modeLabel, new Vector2(fullModeLeft + modeLabelRelLeft, bindLabelHeight), new Vector2(0f, 0.5f), Vector2.One * bindLabelScale, modeBindColor);
        ActiveFont.Draw(MRTDialog.ViewerModes[Mode](), new Vector2(fullModeLeft + modeTextRelLeft, bindLabelHeight), new Vector2(0f, 0.5f), Vector2.One * bindLabelScale, modeBindColor);
    }

    public static void RenderGraph(MapEditor debugMap, Camera camera) {
        //graph background
        if (Mode != (int)Modes.Disabled) {
            DebugMapTweaks.WhiteRect.Draw(new Vector2(camera.Left + (MenuWidth + MarginH * 4 - 1) / camera.Zoom, camera.Top + HeadbarHeight / camera.Zoom), Vector2.Zero, Color.Black * GraphBackOpacity, new Vector2((Celeste.TargetWidth - (MenuWidth + MarginH * 4 - 1)) / camera.Zoom, (Celeste.TargetHeight - HeadbarHeight) / camera.Zoom));
        }

        //points
        if (Graph != null) {
            foreach (Data.Point point in Graph.Points) {
                //scale: texture.Atlas == GFX.Game ? Settings.Instance.WindowScale : 1f
                point.TextElement.Camera = point.TextureElement.Camera = camera;
                point.TextElement.Color = point.TextureElement.Color = Hovers.Contains(point) ? PointHoveredColor : Selection.Contains(point) ? PointSelectedColor : PointColor;
                point.TextElement.Text = string.IsNullOrWhiteSpace(point.Name) ? (Graph.Points.IndexOf(point) + 1).ToString() : point.Name;
                point.TextElement.Position.Y = point.TextureElement.Position.Y - (((point.TextureElement.Texture?.Height ?? 0) / 2 * point.TextureElement.Scale.Y) - PointNameMargin) / camera.Zoom;
                point.TextureElement.Render();
                point.TextElement.Render();
            }
        }
    }
}