using System;
using UnityEngine;

namespace DreadMistQOL
{
    /// <summary>Small IMGUI building blocks (IMGUI has no built-in dropdown).</summary>
    internal static class Ui
    {
        private static GUIStyle _hint;

        public static GUIStyle Hint
        {
            get
            {
                if (_hint == null)
                {
                    _hint = new GUIStyle(GUI.skin.label) { wordWrap = true, fontSize = 11 };
                    _hint.normal.textColor = new Color(0.75f, 0.75f, 0.75f);
                }
                return _hint;
            }
        }

        /// <summary>A button with a one-line plain-English description underneath. Returns true when clicked.</summary>
        public static bool Action(string label, string description)
        {
            bool clicked = GUILayout.Button(label);
            GUILayout.Label(description, Hint);
            return clicked;
        }

        public static string TextField(string label, string value, float width = 90f)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(120f));
            value = GUILayout.TextField(value ?? "", GUILayout.Width(width));
            GUILayout.EndHorizontal();
            return value;
        }

        public static int ParseInt(string s, int fallback, int min, int max)
        {
            int v;
            if (!int.TryParse((s ?? "").Trim(), out v)) v = fallback;
            return Math.Max(min, Math.Min(max, v));
        }

        public static float ParseFloat(string s, float fallback)
        {
            float v;
            if (!float.TryParse((s ?? "").Trim().Replace(',', '.'),
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out v))
                v = fallback;
            return v;
        }
    }

    /// <summary>Click to open a scrolling list, click an entry to choose it.</summary>
    internal sealed class Dropdown
    {
        public int Index;
        private bool _open;
        private Vector2 _scroll;

        public string Draw(string[] options, float listHeight = 150f)
        {
            if (options == null || options.Length == 0)
            {
                GUILayout.Label("(nothing to choose)");
                return null;
            }
            if (Index < 0 || Index >= options.Length) Index = 0;

            if (GUILayout.Button((_open ? "^ " : "v ") + options[Index], GUILayout.MinWidth(200f)))
                _open = !_open;

            if (_open)
            {
                _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.Height(listHeight));
                for (int i = 0; i < options.Length; i++)
                {
                    if (GUILayout.Button(options[i]))
                    {
                        Index = i;
                        _open = false;
                    }
                }
                GUILayout.EndScrollView();
            }
            return options[Index];
        }
    }
}
