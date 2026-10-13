using System.Collections.Generic;
using UnityEngine;

namespace Mutiny.Presentation
{
    /// <summary>Retains the original English atlases and renders CJK with a bundled dynamic font.</summary>
    public static class MutinyLocalizedText
    {
        private static Font s_CjkFont;
        private static readonly HashSet<string> MissingGlyphs = new HashSet<string>();
        // Optical correction for 18 px Douyin Sans CJK text inside 24 px Pirate buttons.
        private const float ChinesePirateButtonYOffset = -2f;
        public const int SpeechFontSize = 15;
        public static readonly Color SpeechColor = new Color32(24, 29, 35, 255);

        private static Font CjkFont => s_CjkFont != null ? s_CjkFont :
            s_CjkFont = Resources.Load<Font>("抖音美好体");

        public static void Pirate(Rect rect, string key, string english, bool hovered = false, bool centered = true,
            int tracking = -3)
        {
            string value = MutinyLocalization.Text(key, english);
            if (MutinyLocalization.UseOriginalFont)
            {
                MutinyBitmapFont.DrawPirateText(rect, value, hovered, centered, tracking);
                return;
            }
            DrawCjk(rect, value, hovered ? Color.yellow : Color.white,
                centered ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft, 18, true);
        }

        public static Rect ResolvePirateButtonTextRect(Rect buttonRect)
        {
            if (MutinyLocalization.UseOriginalFont)
                return buttonRect;
            buttonRect.y += ChinesePirateButtonYOffset;
            return buttonRect;
        }

        public static void PirateButton(Rect buttonRect, string key, string english, bool hovered = false,
            int tracking = -3)
        {
            Pirate(ResolvePirateButtonTextRect(buttonRect), key, english, hovered, true, tracking);
        }

        public static void Dangle(Rect rect, string key, string english, Color color,
            TextAnchor anchor = TextAnchor.MiddleCenter, int tracking = 0, int lineSpacing = 13)
        {
            string value = MutinyLocalization.Text(key, english);
            if (MutinyLocalization.UseOriginalFont)
            {
                MutinyBitmapFont.DrawDangleText(rect, value, color, anchor, tracking, lineSpacing);
                return;
            }
            DrawCjk(rect, value, color, anchor, Mathf.Max(12, lineSpacing - 1), false);
        }

        public static void Speech(Rect rect, string key, string english)
        {
            string value = MutinyLocalization.Text(key, english);
            if (MutinyLocalization.UseOriginalFont)
            {
                MutinyBitmapFont.DrawSpeechText(rect, value, TextAnchor.UpperLeft, 0, 13);
                return;
            }
            DrawReadableCjk(rect, value, SpeechColor, TextAnchor.UpperLeft, SpeechFontSize);
        }

        public static void Tooltip(Rect rect, string value)
        {
            if (MutinyLocalization.UseOriginalFont)
                MutinyBitmapFont.DrawDangleText(rect, value, Color.black, TextAnchor.MiddleCenter, -1);
            else
                DrawReadableCjk(rect, value, Color.black, TextAnchor.MiddleCenter, 12);
        }

        public static float MeasureSpeechHeight(string value, float width) =>
            CreateCjkStyle(SpeechColor, TextAnchor.UpperLeft, SpeechFontSize, false)
                .CalcHeight(new GUIContent(Normalize(value)), width);

        public static Vector2 MeasureTooltipSize(string value) =>
            CreateCjkStyle(Color.black, TextAnchor.MiddleCenter, 12, false).CalcSize(new GUIContent(value));

        private static string Normalize(string text) =>
            (text ?? string.Empty).Replace("||", "\n\n").Replace('|', '\n');

        private static GUIStyle CreateCjkStyle(Color color, TextAnchor anchor, int fontSize, bool bold)
        {
            var style = new GUIStyle
            {
                font = CjkFont,
                fontSize = fontSize,
                fontStyle = bold ? FontStyle.Bold : FontStyle.Normal,
                alignment = anchor,
                wordWrap = true,
                clipping = TextClipping.Clip,
                padding = new RectOffset(0, 0, 0, 0)
            };
            style.normal.textColor = color;
            return style;
        }

        private static void DrawReadableCjk(Rect rect, string text, Color color, TextAnchor anchor, int fontSize)
        {
            Color previousColor = GUI.color;
            Color previousContentColor = GUI.contentColor;
            GUI.color = Color.white;
            GUI.contentColor = Color.white;
            DrawCjk(rect, text, color, anchor, fontSize, false);
            GUI.color = previousColor;
            GUI.contentColor = previousContentColor;
        }

        private static void DrawCjk(Rect rect, string text, Color color, TextAnchor anchor, int fontSize, bool bold)
        {
            Font font = CjkFont;
            if (font == null)
            {
                Debug.LogError("[Localization] Bundled CJK font is missing.");
                return;
            }
            string normalized = Normalize(text);
            foreach (char character in normalized)
            {
                if (char.IsWhiteSpace(character) || font.HasCharacter(character))
                    continue;
                string id = MutinyLocalization.Code + ":U+" + ((int)character).ToString("X4");
                if (MissingGlyphs.Add(id))
                    Debug.LogWarning("[Localization] Missing glyph " + id);
            }
            GUIStyle style = CreateCjkStyle(color, anchor, fontSize, bold);
            GUI.Label(rect, normalized, style);
        }
    }
}
