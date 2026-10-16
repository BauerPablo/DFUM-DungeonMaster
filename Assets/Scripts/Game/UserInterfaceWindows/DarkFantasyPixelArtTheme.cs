// Project:         Daggerfall Unity
// Copyright:       Copyright (C) 2009-2022 Daggerfall Workshop
// Web Site:        http://www.dfworkshop.net
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Source Code:     https://github.com/Interkarma/daggerfall-unity

using UnityEngine;
using DaggerfallWorkshop.Game.UserInterface;

namespace DaggerfallWorkshop.Game.UserInterfaceWindows
{
    /// <summary>
    /// Procedural pixel-art theme generator inspired by Dark Fantasy RPG UI packs.
    /// Produces crisp Point-filtered textures with ornate bronze/gold corner filigree,
    /// dark iron/wood frames, aged parchment cards, and crimson/emerald/gold bevel buttons.
    /// </summary>
    public static class DarkFantasyPixelArtTheme
    {
        // Color Palette
        public static readonly Color ObsidianBg = new Color32(18, 16, 20, 245);
        public static readonly Color SlateBg = new Color32(28, 25, 30, 248);
        public static readonly Color WoodFrameDark = new Color32(42, 31, 23, 255);
        public static readonly Color WoodFrameMid = new Color32(68, 49, 35, 255);
        public static readonly Color BronzeOuter = new Color32(115, 88, 48, 255);
        public static readonly Color GoldHighlight = new Color32(214, 178, 96, 255);
        public static readonly Color GoldBright = new Color32(245, 215, 128, 255);

        public static readonly Color ParchmentBase = new Color32(194, 175, 148, 255);
        public static readonly Color ParchmentDark = new Color32(166, 146, 118, 255);
        public static readonly Color ParchmentBorder = new Color32(92, 72, 48, 255);
        public static readonly Color ParchmentInk = new Color32(38, 28, 20, 255);

        public static readonly Color CrimsonBase = new Color32(124, 38, 38, 245);
        public static readonly Color CrimsonHighlight = new Color32(168, 56, 54, 255);
        public static readonly Color CrimsonShadow = new Color32(74, 20, 20, 255);

        public static readonly Color EmeraldBase = new Color32(36, 98, 52, 245);
        public static readonly Color EmeraldHighlight = new Color32(58, 142, 76, 255);
        public static readonly Color EmeraldShadow = new Color32(20, 56, 30, 255);

        public static readonly Color IronButtonBase = new Color32(48, 44, 52, 245);
        public static readonly Color IronButtonHighlight = new Color32(78, 72, 84, 255);
        public static readonly Color IronButtonShadow = new Color32(26, 24, 28, 255);

        public static readonly Color TextGold = new Color32(238, 206, 122, 255);
        public static readonly Color TextIvory = new Color32(232, 224, 208, 255);
        public static readonly Color TextMuted = new Color32(162, 152, 138, 255);
        public static readonly Color TextReadyGreen = new Color32(115, 230, 125, 255);
        public static readonly Color TextWarnAmber = new Color32(245, 185, 75, 255);
        public static readonly Color TextErrorRed = new Color32(240, 95, 95, 255);

        /// <summary>
        /// Generates a crisp pixel-art ornate window frame with bronze/gold corner filigree.
        /// </summary>
        public static Texture2D CreateOrnateWindowTexture(int width, int height, Color bgFill, bool parchmentInterior = false)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color fill = parchmentInterior ? ParchmentBase : bgFill;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Subtle dither/vignette near inner edges
                    int distEdge = Mathf.Min(Mathf.Min(x, width - 1 - x), Mathf.Min(y, height - 1 - y));
                    Color c = fill;
                    if (parchmentInterior && distEdge < 8)
                    {
                        c = Color.Lerp(ParchmentDark, ParchmentBase, distEdge / 8f);
                    }
                    tex.SetPixel(x, y, c);
                }
            }

            // Outer 1px black silhouette
            DrawRectBorder(tex, 0, 0, width, height, new Color32(10, 8, 12, 255));
            // 2nd pixel dark wood/iron
            DrawRectBorder(tex, 1, 1, width - 2, height - 2, WoodFrameDark);
            // 3rd pixel bronze rim
            DrawRectBorder(tex, 2, 2, width - 4, height - 4, BronzeOuter);
            // 4th pixel inner dark groove
            DrawRectBorder(tex, 3, 3, width - 6, height - 6, WoodFrameDark);
            // 5th pixel subtle gold inner hairline
            DrawRectBorder(tex, 4, 4, width - 8, height - 8, new Color32(95, 75, 42, 220));

            // Ornate pixel-art corner brackets (top-left, top-right, bottom-left, bottom-right)
            DrawCornerOrnament(tex, 2, 2, 1, 1);
            DrawCornerOrnament(tex, width - 3, 2, -1, 1);
            DrawCornerOrnament(tex, 2, height - 3, 1, -1);
            DrawCornerOrnament(tex, width - 3, height - 3, -1, -1);

            tex.Apply();
            return tex;
        }

        /// <summary>
        /// Generates a recessed dark slate or parchment sub-panel texture with pixel-art bevels.
        /// </summary>
        public static Texture2D CreateRecessedPanelTexture(int width, int height, bool isParchment = false)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color baseCol = isParchment ? ParchmentBase : (Color)new Color32(16, 14, 18, 242);
            Color edgeDark = isParchment ? ParchmentDark : (Color)new Color32(11, 10, 13, 250);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int dist = Mathf.Min(Mathf.Min(x, width - 1 - x), Mathf.Min(y, height - 1 - y));
                    tex.SetPixel(x, y, dist < 5 ? edgeDark : baseCol);
                }
            }

            DrawRectBorder(tex, 0, 0, width, height, BronzeOuter);
            DrawRectBorder(tex, 1, 1, width - 2, height - 2, new Color32(10, 8, 10, 255));

            // Corner rivets
            SetPixelSafe(tex, 2, 2, GoldHighlight);
            SetPixelSafe(tex, width - 3, 2, GoldHighlight);
            SetPixelSafe(tex, 2, height - 3, GoldHighlight);
            SetPixelSafe(tex, width - 3, height - 3, GoldHighlight);

            tex.Apply();
            return tex;
        }

        /// <summary>
        /// Generates a pixel-art beveled button texture (Crimson, Emerald, Iron, or Gold).
        /// </summary>
        public static Texture2D CreatePixelButtonTexture(int width, int height, Color bodyColor, Color highlightColor, Color shadowColor, bool ornateEnds = true)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;

            for (int y = 0; y < height; y++)
            {
                float t = (float)y / Mathf.Max(1, height - 1);
                Color rowColor = Color.Lerp(shadowColor, bodyColor, Mathf.Clamp01(t * 1.3f));
                for (int x = 0; x < width; x++)
                {
                    tex.SetPixel(x, y, rowColor);
                }
            }

            // Outer dark outline
            DrawRectBorder(tex, 0, 0, width, height, new Color32(12, 10, 14, 255));

            // Top and left highlight bevel
            for (int x = 1; x < width - 1; x++)
                tex.SetPixel(x, height - 2, highlightColor);
            for (int y = 2; y < height - 1; y++)
                tex.SetPixel(1, y, highlightColor);

            // Bottom and right shadow bevel
            for (int x = 1; x < width - 1; x++)
                tex.SetPixel(x, 1, shadowColor);
            for (int y = 1; y < height - 2; y++)
                tex.SetPixel(width - 2, y, shadowColor);

            if (ornateEnds && width >= 24 && height >= 10)
            {
                // Gold corner studs
                SetPixelSafe(tex, 2, 2, GoldHighlight);
                SetPixelSafe(tex, width - 3, 2, GoldHighlight);
                SetPixelSafe(tex, 2, height - 3, GoldHighlight);
                SetPixelSafe(tex, width - 3, height - 3, GoldHighlight);
            }

            tex.Apply();
            return tex;
        }

        /// <summary>
        /// Generates a pixel-art radio button texture (circular medallion with bronze outer ring, dark core, and gold gem when active).
        /// </summary>
        public static Texture2D CreatePixelRadioButtonTexture(int size = 14, bool isChecked = false, bool isHover = false)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color clear = new Color(0, 0, 0, 0);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    tex.SetPixel(x, y, clear);

            float radius = (size - 1) * 0.5f;
            Vector2 center = new Vector2(radius, radius);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    if (dist > radius + 0.35f)
                        continue;

                    if (dist >= radius - 0.75f)
                    {
                        // Outer rim: Bronze / Gold highlight on top, shadow on bottom
                        tex.SetPixel(x, y, y >= center.y ? (isHover ? GoldBright : GoldHighlight) : BronzeShadow);
                    }
                    else if (dist >= radius - 1.75f)
                    {
                        // Inner dark groove
                        tex.SetPixel(x, y, WoodFrameDark);
                    }
                    else
                    {
                        // Recessed dark core
                        tex.SetPixel(x, y, new Color32(16, 14, 20, 255));
                    }
                }
            }

            if (isChecked)
            {
                float coreRadius = radius * 0.52f;
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float dist = Vector2.Distance(new Vector2(x, y), center);
                        if (dist <= coreRadius)
                        {
                            if (dist <= coreRadius * 0.35f)
                                tex.SetPixel(x, y, Color.white);
                            else if (dist <= coreRadius * 0.75f)
                                tex.SetPixel(x, y, GoldBright);
                            else
                                tex.SetPixel(x, y, GoldHighlight);
                        }
                    }
                }
            }

            tex.Apply();
            return tex;
        }

        /// <summary>
        /// Generates a pixel-art square beveled checkbox texture.
        /// </summary>
        public static Texture2D CreatePixelCheckboxTexture(int size = 14, bool isChecked = false, bool isHover = false)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color baseCol = new Color32(16, 14, 20, 255);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    tex.SetPixel(x, y, baseCol);

            // Outer bronze border
            DrawRectBorder(tex, 0, 0, size, size, isHover ? GoldBright : BronzeOuter);
            // Inner dark groove
            DrawRectBorder(tex, 1, 1, size - 2, size - 2, WoodFrameDark);

            // Corner gold studs
            SetPixelSafe(tex, 1, 1, GoldHighlight);
            SetPixelSafe(tex, size - 2, 1, BronzeShadow);
            SetPixelSafe(tex, 1, size - 2, GoldHighlight);
            SetPixelSafe(tex, size - 2, size - 2, BronzeShadow);

            if (isChecked)
            {
                // Glowing Emerald core gem (diamond / square)
                int pad = 3;
                for (int y = pad; y < size - pad; y++)
                {
                    for (int x = pad; x < size - pad; x++)
                    {
                        tex.SetPixel(x, y, EmeraldBase);
                    }
                }
                // Center jewel highlight
                int midX = size / 2;
                int midY = size / 2;
                SetPixelSafe(tex, midX, midY, Color.white);
                SetPixelSafe(tex, midX - 1, midY, EmeraldHighlight);
                SetPixelSafe(tex, midX + 1, midY, EmeraldHighlight);
                SetPixelSafe(tex, midX, midY - 1, EmeraldHighlight);
                SetPixelSafe(tex, midX, midY + 1, EmeraldHighlight);
            }

            tex.Apply();
            return tex;
        }

        private static Texture2D cachedRadioUnchecked;
        private static Texture2D cachedRadioChecked;
        private static Texture2D cachedRadioHoverUnchecked;
        private static Texture2D cachedRadioHoverChecked;

        private static Texture2D cachedBoxUnchecked;
        private static Texture2D cachedBoxChecked;
        private static Texture2D cachedBoxHoverUnchecked;
        private static Texture2D cachedBoxHoverChecked;

        public static Texture2D RadioButtonUncheckedTex
        {
            get
            {
                if (cachedRadioUnchecked == null)
                    cachedRadioUnchecked = CreatePixelRadioButtonTexture(14, false, false);
                return cachedRadioUnchecked;
            }
        }

        public static Texture2D RadioButtonCheckedTex
        {
            get
            {
                if (cachedRadioChecked == null)
                    cachedRadioChecked = CreatePixelRadioButtonTexture(14, true, false);
                return cachedRadioChecked;
            }
        }

        public static Texture2D CheckboxUncheckedTex
        {
            get
            {
                if (cachedBoxUnchecked == null)
                    cachedBoxUnchecked = CreatePixelCheckboxTexture(14, false, false);
                return cachedBoxUnchecked;
            }
        }

        public static Texture2D CheckboxCheckedTex
        {
            get
            {
                if (cachedBoxChecked == null)
                    cachedBoxChecked = CreatePixelCheckboxTexture(14, true, false);
                return cachedBoxChecked;
            }
        }

        public static Texture2D GetRadioButtonTexture(bool isChecked, bool isHover = false)
        {
            if (isChecked)
            {
                if (isHover)
                {
                    if (cachedRadioHoverChecked == null) cachedRadioHoverChecked = CreatePixelRadioButtonTexture(14, true, true);
                    return cachedRadioHoverChecked;
                }
                return RadioButtonCheckedTex;
            }
            if (isHover)
            {
                if (cachedRadioHoverUnchecked == null) cachedRadioHoverUnchecked = CreatePixelRadioButtonTexture(14, false, true);
                return cachedRadioHoverUnchecked;
            }
            return RadioButtonUncheckedTex;
        }

        public static Texture2D GetCheckboxTexture(bool isChecked, bool isHover = false)
        {
            if (isChecked)
            {
                if (isHover)
                {
                    if (cachedBoxHoverChecked == null) cachedBoxHoverChecked = CreatePixelCheckboxTexture(14, true, true);
                    return cachedBoxHoverChecked;
                }
                return CheckboxCheckedTex;
            }
            if (isHover)
            {
                if (cachedBoxHoverUnchecked == null) cachedBoxHoverUnchecked = CreatePixelCheckboxTexture(14, false, true);
                return cachedBoxHoverUnchecked;
            }
            return CheckboxUncheckedTex;
        }

        private static void DrawRectBorder(Texture2D tex, int x0, int y0, int w, int h, Color col)
        {
            int x1 = x0 + w - 1;
            int y1 = y0 + h - 1;
            for (int x = x0; x <= x1; x++)
            {
                SetPixelSafe(tex, x, y0, col);
                SetPixelSafe(tex, x, y1, col);
            }
            for (int y = y0; y <= y1; y++)
            {
                SetPixelSafe(tex, x0, y, col);
                SetPixelSafe(tex, x1, y, col);
            }
        }

        private static void DrawCornerOrnament(Texture2D tex, int cx, int cy, int dx, int dy)
        {
            // Gold L-bracket filigree (7x7 pixels)
            for (int i = 0; i < 7; i++)
            {
                SetPixelSafe(tex, cx + i * dx, cy, GoldHighlight);
                SetPixelSafe(tex, cx, cy + i * dy, GoldHighlight);
            }
            SetPixelSafe(tex, cx + dx, cy + dy, GoldBright);
            SetPixelSafe(tex, cx + 2 * dx, cy + dy, GoldHighlight);
            SetPixelSafe(tex, cx + dx, cy + 2 * dy, GoldHighlight);
            SetPixelSafe(tex, cx + 4 * dx, cy + 2 * dy, BronzeOuter);
            SetPixelSafe(tex, cx + 2 * dx, cy + 4 * dy, BronzeOuter);
        }

        private static void SetPixelSafe(Texture2D tex, int x, int y, Color col)
        {
            if (x >= 0 && x < tex.width && y >= 0 && y < tex.height)
                tex.SetPixel(x, y, col);
        }

        public static readonly Color BronzeShadow = new Color32(58, 42, 22, 255);
        public static readonly Color ShadowBlack = new Color32(10, 8, 12, 255);

        public enum ButtonVariant
        {
            IronDefault,
            EmeraldAction,
            CrimsonDanger,
            GoldHighlight
        }

        /// <summary>
        /// Overload using default ObsidianBg fill.
        /// </summary>
        public static Texture2D CreateOrnateWindowTexture(int width, int height, bool parchmentInterior = false)
        {
            return CreateOrnateWindowTexture(width, height, ObsidianBg, parchmentInterior);
        }

        /// <summary>
        /// Styles a native DaggerfallUI Button with a pixel-art beveled texture and crisp label.
        /// </summary>
        public static void StyleButton(Button btn, string text, ButtonVariant variant = ButtonVariant.IronDefault, float textScale = 1.0f)
        {
            if (btn == null)
                return;

            int w = Mathf.Max(24, Mathf.RoundToInt(btn.Size.x));
            int h = Mathf.Max(12, Mathf.RoundToInt(btn.Size.y));

            Color body = IronButtonBase;
            Color hi = IronButtonHighlight;
            Color sh = IronButtonShadow;
            Color txtCol = TextIvory;

            switch (variant)
            {
                case ButtonVariant.EmeraldAction:
                    body = EmeraldBase;
                    hi = EmeraldHighlight;
                    sh = EmeraldShadow;
                    txtCol = GoldBright;
                    break;
                case ButtonVariant.CrimsonDanger:
                    body = CrimsonBase;
                    hi = CrimsonHighlight;
                    sh = CrimsonShadow;
                    txtCol = TextIvory;
                    break;
                case ButtonVariant.GoldHighlight:
                    body = BronzeOuter;
                    hi = GoldHighlight;
                    sh = BronzeShadow;
                    txtCol = GoldBright;
                    break;
            }

            btn.BackgroundTexture = CreatePixelButtonTexture(w, h, body, hi, sh, true);
            btn.BackgroundTextureLayout = BackgroundLayout.StretchToFill;
            btn.Label.Text = text;
            btn.Label.TextScale = textScale;
            btn.Label.TextColor = txtCol;
            btn.Label.ShadowColor = ShadowBlack;
            btn.Label.ShadowPosition = Vector2.one;
        }

        // =====================================================================
        // IMGUI Dark Fantasy Pixel-Art Skin & Styles (In-Game DM & Player Menus)
        // =====================================================================

        private static GUISkin cachedImGuiSkin;
        private static GUIStyle cachedTabActiveStyle;
        private static GUIStyle cachedTabInactiveStyle;
        private static GUIStyle cachedEmeraldButtonStyle;
        private static GUIStyle cachedCrimsonButtonStyle;
        private static GUIStyle cachedGoldButtonStyle;
        private static GUIStyle cachedSectionBannerStyle;
        private static GUIStyle cachedSpeechBubbleBoxStyle;
        private static Texture2D cachedDividerTex;

        public static GUIStyle TabActiveStyle
        {
            get
            {
                EnsureImGuiStyles(GUI.skin);
                return cachedTabActiveStyle;
            }
        }

        public static GUIStyle TabInactiveStyle
        {
            get
            {
                EnsureImGuiStyles(GUI.skin);
                return cachedTabInactiveStyle;
            }
        }

        public static GUIStyle EmeraldButtonStyle
        {
            get
            {
                EnsureImGuiStyles(GUI.skin);
                return cachedEmeraldButtonStyle;
            }
        }

        public static GUIStyle CrimsonButtonStyle
        {
            get
            {
                EnsureImGuiStyles(GUI.skin);
                return cachedCrimsonButtonStyle;
            }
        }

        public static GUIStyle GoldButtonStyle
        {
            get
            {
                EnsureImGuiStyles(GUI.skin);
                return cachedGoldButtonStyle;
            }
        }

        public static GUIStyle SectionBannerStyle
        {
            get
            {
                EnsureImGuiStyles(GUI.skin);
                return cachedSectionBannerStyle;
            }
        }

        public static GUIStyle SpeechBubbleBoxStyle
        {
            get
            {
                EnsureImGuiStyles(GUI.skin);
                return cachedSpeechBubbleBoxStyle;
            }
        }

        public static GUISkin GetOrCreateImGuiSkin(GUISkin baseSkin)
        {
            EnsureImGuiStyles(baseSkin);
            return cachedImGuiSkin;
        }

        /// <summary>
        /// Generates a 9-sliceable IMGUI radio button texture (medallion on left, completely transparent stretch area on right).
        /// This eliminates horizontal stretch lines behind label text in GUILayout.Toggle.
        /// </summary>
        public static Texture2D CreateImGuiRadioButtonTexture(bool isChecked, bool isHover = false)
        {
            int w = 28;
            int h = 18;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color clear = new Color(0, 0, 0, 0);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    tex.SetPixel(x, y, clear);

            float radius = 6.5f;
            Vector2 center = new Vector2(7.5f, 8.5f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    if (dist > radius + 0.35f)
                        continue;

                    if (dist >= radius - 0.75f)
                    {
                        tex.SetPixel(x, y, y >= center.y ? (isHover ? GoldBright : GoldHighlight) : BronzeShadow);
                    }
                    else if (dist >= radius - 1.75f)
                    {
                        tex.SetPixel(x, y, WoodFrameDark);
                    }
                    else
                    {
                        tex.SetPixel(x, y, new Color32(16, 14, 20, 255));
                    }
                }
            }

            if (isChecked)
            {
                float coreRadius = radius * 0.52f;
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < 16; x++)
                    {
                        float dist = Vector2.Distance(new Vector2(x, y), center);
                        if (dist <= coreRadius)
                        {
                            if (dist <= coreRadius * 0.35f)
                                tex.SetPixel(x, y, Color.white);
                            else if (dist <= coreRadius * 0.75f)
                                tex.SetPixel(x, y, GoldBright);
                            else
                                tex.SetPixel(x, y, GoldHighlight);
                        }
                    }
                }
            }

            tex.Apply();
            return tex;
        }

        private static void EnsureImGuiStyles(GUISkin baseSkin)
        {
            if (cachedImGuiSkin != null && cachedTabActiveStyle != null)
                return;

            if (baseSkin == null)
                return;

            cachedImGuiSkin = Object.Instantiate(baseSkin);
            cachedImGuiSkin.name = "DarkFantasyPixelArtImGuiSkin";

            // 1. Ornate Window Frame
            Texture2D winTex = CreateOrnateWindowTexture(96, 96, ObsidianBg, false);
            winTex.hideFlags = HideFlags.HideAndDontSave;
            GUIStyle winStyle = new GUIStyle(baseSkin.window)
            {
                border = new RectOffset(8, 8, 8, 8),
                padding = new RectOffset(14, 14, 30, 12),
                fontStyle = FontStyle.Bold,
                fontSize = 13,
                alignment = TextAnchor.UpperCenter,
                richText = true
            };
            ApplyStyleBackgroundAllStates(winStyle, winTex, GoldHighlight);
            cachedImGuiSkin.window = winStyle;

            // 2. Recessed Obsidian & Bronze Box
            Texture2D boxTex = CreateRecessedPanelTexture(64, 64, false);
            boxTex.hideFlags = HideFlags.HideAndDontSave;
            GUIStyle boxStyle = new GUIStyle(baseSkin.box)
            {
                border = new RectOffset(6, 6, 6, 6),
                padding = new RectOffset(8, 8, 6, 6),
                margin = new RectOffset(2, 2, 4, 4),
                richText = true
            };
            ApplyStyleBackgroundAllStates(boxStyle, boxTex, TextIvory);
            cachedImGuiSkin.box = boxStyle;

            // 3. Iron & Gold Beveled Buttons
            Texture2D btnNormalTex = CreatePixelButtonTexture(64, 24, IronButtonBase, IronButtonHighlight, IronButtonShadow, true);
            Texture2D btnHoverTex = CreatePixelButtonTexture(64, 24, new Color32(66, 54, 44, 255), GoldHighlight, BronzeShadow, true);
            Texture2D btnActiveTex = CreatePixelButtonTexture(64, 24, IronButtonShadow, BronzeOuter, IronButtonHighlight, true);
            btnNormalTex.hideFlags = HideFlags.HideAndDontSave;
            btnHoverTex.hideFlags = HideFlags.HideAndDontSave;
            btnActiveTex.hideFlags = HideFlags.HideAndDontSave;

            GUIStyle btnStyle = new GUIStyle(baseSkin.button)
            {
                border = new RectOffset(4, 4, 4, 4),
                padding = new RectOffset(8, 8, 4, 4),
                margin = new RectOffset(2, 2, 2, 2),
                fontStyle = FontStyle.Bold,
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter,
                richText = true
            };
            btnStyle.normal.background = btnNormalTex;
            btnStyle.normal.textColor = TextIvory;
            btnStyle.hover.background = btnHoverTex;
            btnStyle.hover.textColor = GoldHighlight;
            btnStyle.active.background = btnActiveTex;
            btnStyle.active.textColor = GoldBright;
            btnStyle.focused.background = btnHoverTex;
            btnStyle.focused.textColor = GoldHighlight;
            cachedImGuiSkin.button = btnStyle;

            // 4. Sunken Obsidian TextFields
            Texture2D tfNormalTex = CreateRecessedPanelTexture(48, 22, false);
            Texture2D tfFocusTex = CreatePixelButtonTexture(48, 22, new Color32(20, 18, 26, 255), GoldHighlight, BronzeOuter, false);
            tfNormalTex.hideFlags = HideFlags.HideAndDontSave;
            tfFocusTex.hideFlags = HideFlags.HideAndDontSave;

            GUIStyle tfStyle = new GUIStyle(baseSkin.textField)
            {
                border = new RectOffset(4, 4, 4, 4),
                padding = new RectOffset(6, 6, 3, 3),
                fontSize = 12
            };
            tfStyle.normal.background = tfNormalTex;
            tfStyle.normal.textColor = GoldHighlight;
            tfStyle.hover.background = tfNormalTex;
            tfStyle.hover.textColor = GoldHighlight;
            tfStyle.focused.background = tfFocusTex;
            tfStyle.focused.textColor = Color.white;
            tfStyle.active.background = tfFocusTex;
            tfStyle.active.textColor = Color.white;
            cachedImGuiSkin.textField = tfStyle;

            // 5. Labels & Toggles
            GUIStyle lblStyle = new GUIStyle(baseSkin.label)
            {
                richText = true,
                fontSize = 12
            };
            lblStyle.normal.textColor = TextIvory;
            lblStyle.hover.textColor = TextIvory;
            cachedImGuiSkin.label = lblStyle;

            Texture2D radioUnchecked = CreateImGuiRadioButtonTexture(false, false);
            Texture2D radioChecked = CreateImGuiRadioButtonTexture(true, false);
            Texture2D radioHoverUnchecked = CreateImGuiRadioButtonTexture(false, true);
            Texture2D radioHoverChecked = CreateImGuiRadioButtonTexture(true, true);
            radioUnchecked.hideFlags = HideFlags.HideAndDontSave;
            radioChecked.hideFlags = HideFlags.HideAndDontSave;
            radioHoverUnchecked.hideFlags = HideFlags.HideAndDontSave;
            radioHoverChecked.hideFlags = HideFlags.HideAndDontSave;

            GUIStyle toggleStyle = new GUIStyle(baseSkin.toggle)
            {
                richText = true,
                fontSize = 12,
                imagePosition = ImagePosition.ImageLeft,
                fixedHeight = 18,
                border = new RectOffset(16, 2, 0, 0),
                padding = new RectOffset(22, 4, 1, 1),
                overflow = new RectOffset(0, 0, 0, 0)
            };
            toggleStyle.normal.background = radioUnchecked;
            toggleStyle.normal.textColor = TextIvory;
            toggleStyle.onNormal.background = radioChecked;
            toggleStyle.onNormal.textColor = GoldHighlight;
            toggleStyle.hover.background = radioHoverUnchecked;
            toggleStyle.hover.textColor = GoldBright;
            toggleStyle.onHover.background = radioHoverChecked;
            toggleStyle.onHover.textColor = GoldBright;
            toggleStyle.active.background = radioChecked;
            toggleStyle.active.textColor = GoldHighlight;
            toggleStyle.onActive.background = radioChecked;
            toggleStyle.onActive.textColor = GoldHighlight;
            cachedImGuiSkin.toggle = toggleStyle;

            // 6. Active & Inactive Tab Styles
            Texture2D tabActiveTex = CreatePixelButtonTexture(80, 28, new Color32(82, 62, 30, 255), GoldHighlight, BronzeOuter, true);
            Texture2D tabInactiveTex = CreatePixelButtonTexture(80, 28, new Color32(28, 26, 34, 255), IronButtonHighlight, new Color32(14, 12, 18, 255), false);
            Texture2D tabHoverTex = CreatePixelButtonTexture(80, 28, new Color32(48, 42, 38, 255), GoldBright, BronzeShadow, true);
            tabActiveTex.hideFlags = HideFlags.HideAndDontSave;
            tabInactiveTex.hideFlags = HideFlags.HideAndDontSave;
            tabHoverTex.hideFlags = HideFlags.HideAndDontSave;

            cachedTabActiveStyle = new GUIStyle(btnStyle)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(6, 6, 5, 5)
            };
            ApplyStyleBackgroundAllStates(cachedTabActiveStyle, tabActiveTex, GoldHighlight);

            cachedTabInactiveStyle = new GUIStyle(btnStyle)
            {
                fontSize = 12,
                fontStyle = FontStyle.Normal,
                padding = new RectOffset(6, 6, 5, 5)
            };
            cachedTabInactiveStyle.normal.background = tabInactiveTex;
            cachedTabInactiveStyle.normal.textColor = TextMuted;
            cachedTabInactiveStyle.hover.background = tabHoverTex;
            cachedTabInactiveStyle.hover.textColor = GoldHighlight;
            cachedTabInactiveStyle.active.background = tabActiveTex;
            cachedTabInactiveStyle.active.textColor = GoldHighlight;

            // 7. Specialized Emerald / Crimson / Gold Action Button Styles
            Texture2D emNorm = CreatePixelButtonTexture(64, 24, EmeraldBase, EmeraldHighlight, EmeraldShadow, true);
            Texture2D emHov = CreatePixelButtonTexture(64, 24, EmeraldHighlight, GoldHighlight, EmeraldBase, true);
            emNorm.hideFlags = HideFlags.HideAndDontSave;
            emHov.hideFlags = HideFlags.HideAndDontSave;
            cachedEmeraldButtonStyle = new GUIStyle(btnStyle);
            cachedEmeraldButtonStyle.normal.background = emNorm;
            cachedEmeraldButtonStyle.normal.textColor = GoldHighlight;
            cachedEmeraldButtonStyle.hover.background = emHov;
            cachedEmeraldButtonStyle.hover.textColor = Color.white;
            cachedEmeraldButtonStyle.active.background = emNorm;

            Texture2D crNorm = CreatePixelButtonTexture(64, 24, CrimsonBase, CrimsonHighlight, CrimsonShadow, true);
            Texture2D crHov = CreatePixelButtonTexture(64, 24, CrimsonHighlight, GoldHighlight, CrimsonBase, true);
            crNorm.hideFlags = HideFlags.HideAndDontSave;
            crHov.hideFlags = HideFlags.HideAndDontSave;
            cachedCrimsonButtonStyle = new GUIStyle(btnStyle);
            cachedCrimsonButtonStyle.normal.background = crNorm;
            cachedCrimsonButtonStyle.normal.textColor = TextIvory;
            cachedCrimsonButtonStyle.hover.background = crHov;
            cachedCrimsonButtonStyle.hover.textColor = GoldHighlight;
            cachedCrimsonButtonStyle.active.background = crNorm;

            Texture2D gdNorm = CreatePixelButtonTexture(64, 24, new Color32(94, 70, 32, 255), GoldHighlight, BronzeShadow, true);
            Texture2D gdHov = CreatePixelButtonTexture(64, 24, new Color32(122, 92, 42, 255), GoldHighlight, BronzeOuter, true);
            gdNorm.hideFlags = HideFlags.HideAndDontSave;
            gdHov.hideFlags = HideFlags.HideAndDontSave;
            cachedGoldButtonStyle = new GUIStyle(btnStyle);
            cachedGoldButtonStyle.normal.background = gdNorm;
            cachedGoldButtonStyle.normal.textColor = GoldHighlight;
            cachedGoldButtonStyle.hover.background = gdHov;
            cachedGoldButtonStyle.hover.textColor = Color.white;
            cachedGoldButtonStyle.active.background = gdNorm;

            // 8. Section Banner Style
            Texture2D bannerTex = CreatePixelButtonTexture(64, 22, new Color32(34, 28, 38, 255), BronzeOuter, new Color32(14, 12, 18, 255), true);
            bannerTex.hideFlags = HideFlags.HideAndDontSave;
            cachedSectionBannerStyle = new GUIStyle(boxStyle)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                fontSize = 12,
                padding = new RectOffset(8, 8, 4, 4)
            };
            ApplyStyleBackgroundAllStates(cachedSectionBannerStyle, bannerTex, GoldHighlight);

            // 9. Speech Bubble Box Style
            Texture2D bubbleTex = CreateOrnateWindowTexture(64, 48, ObsidianBg, false);
            bubbleTex.hideFlags = HideFlags.HideAndDontSave;
            cachedSpeechBubbleBoxStyle = new GUIStyle(boxStyle)
            {
                border = new RectOffset(6, 6, 6, 6),
                padding = new RectOffset(8, 8, 6, 6)
            };
            ApplyStyleBackgroundAllStates(cachedSpeechBubbleBoxStyle, bubbleTex, TextIvory);

            // 10. Gold Filigree Divider Line
            cachedDividerTex = new Texture2D(64, 3, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                hideFlags = HideFlags.HideAndDontSave
            };
            for (int x = 0; x < 64; x++)
            {
                float edgeFade = Mathf.Sin((float)x / 63f * Mathf.PI);
                Color c = Color.Lerp(BronzeShadow, GoldBright, edgeFade);
                cachedDividerTex.SetPixel(x, 0, BronzeShadow);
                cachedDividerTex.SetPixel(x, 1, c);
                cachedDividerTex.SetPixel(x, 2, BronzeShadow);
            }
            cachedDividerTex.Apply();
        }

        private static void ApplyStyleBackgroundAllStates(GUIStyle style, Texture2D tex, Color textColor)
        {
            style.normal.background = tex;
            style.normal.textColor = textColor;
            style.onNormal.background = tex;
            style.onNormal.textColor = textColor;
            style.hover.background = tex;
            style.hover.textColor = textColor;
            style.onHover.background = tex;
            style.onHover.textColor = textColor;
            style.active.background = tex;
            style.active.textColor = textColor;
            style.onActive.background = tex;
            style.onActive.textColor = textColor;
            style.focused.background = tex;
            style.focused.textColor = textColor;
            style.onFocused.background = tex;
            style.onFocused.textColor = textColor;
        }

        /// <summary>
        /// Draws a gold pixel-art ornamental divider inside an IMGUI layout.
        /// </summary>
        public static void DrawImGuiDivider()
        {
            EnsureImGuiStyles(GUI.skin);
            if (cachedDividerTex != null)
            {
                GUILayout.Space(3);
                Rect r = GUILayoutUtility.GetRect(10f, 3f, GUILayout.ExpandWidth(true));
                GUI.DrawTexture(r, cachedDividerTex, ScaleMode.StretchToFill);
                GUILayout.Space(3);
            }
        }
    }
}
