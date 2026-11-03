using System;
using UnityEngine;

namespace DreadMistQOL
{
    /// <summary>Small IMGUI building blocks plus a dark, high-contrast skin so the panel is easy to read.</summary>
    internal static class Ui
    {
        private static GUISkin _skin;
        private static GUIStyle _hint;

        public static GUISkin Skin
        {
            get
            {
                if (_skin == null) Build();
                return _skin;
            }
        }

        public static GUIStyle Hint
        {
            get
            {
                if (_skin == null) Build();
                return _hint;
            }
        }

        private static Texture2D Tex(Color c)
        {
            var t = new Texture2D(1, 1);
            t.SetPixel(0, 0, c);
            t.Apply();
            t.hideFlags = HideFlags.HideAndDontSave;
            return t;
        }

        private static void Paint(GUIStyle st, Texture2D normal, Texture2D hover, Texture2D active, Color text)
        {
            st.normal.background = normal; st.onNormal.background = normal;
            st.hover.background = hover; st.onHover.background = hover;
            st.active.background = active; st.onActive.background = active;
            st.focused.background = normal; st.onFocused.background = normal;
            st.normal.textColor = text; st.onNormal.textColor = text;
            st.hover.textColor = text; st.onHover.textColor = text;
            st.active.textColor = text; st.onActive.textColor = text;
            st.focused.textColor = text; st.onFocused.textColor = text;
        }

        private static void Build()
        {
            // Must run inside OnGUI (GUI.skin is only available there).
            _skin = UnityEngine.Object.Instantiate(GUI.skin);

            Color white = new Color(0.96f, 0.97f, 0.99f, 1f);
            Texture2D bg = Tex(new Color(0.07f, 0.08f, 0.10f, 0.98f));
            Texture2D box = Tex(new Color(0.14f, 0.16f, 0.20f, 1f));
            Texture2D btn = Tex(new Color(0.22f, 0.26f, 0.33f, 1f));
            Texture2D btnHover = Tex(new Color(0.30f, 0.38f, 0.50f, 1f));
            Texture2D btnActive = Tex(new Color(0.18f, 0.48f, 0.38f, 1f));
            Texture2D field = Tex(new Color(0.03f, 0.04f, 0.05f, 1f));

            Paint(_skin.window, bg, bg, bg, white);
            _skin.window.fontSize = 15;
            _skin.window.fontStyle = FontStyle.Bold;
            _skin.window.padding = new RectOffset(12, 12, 30, 12);

            _skin.label.fontSize = 14;
            _skin.label.normal.textColor = white;
            _skin.label.wordWrap = true;

            Paint(_skin.button, btn, btnHover, btnActive, white);
            _skin.button.fontSize = 14;
            _skin.button.padding = new RectOffset(8, 8, 6, 6);
            _skin.button.margin = new RectOffset(3, 3, 3, 3);

            Paint(_skin.textField, field, field, field, white);
            _skin.textField.fontSize = 14;
            _skin.textField.padding = new RectOffset(6, 6, 5, 5);

            Paint(_skin.box, box, box, box, white);
            _skin.box.fontSize = 14;
            _skin.box.fontStyle = FontStyle.Bold;
            _skin.box.alignment = TextAnchor.MiddleLeft;
            _skin.box.padding = new RectOffset(8, 8, 5, 5);

            _skin.toggle.fontSize = 14;
            _skin.toggle.normal.textColor = white;
            _skin.toggle.onNormal.textColor = white;
            _skin.toggle.hover.textColor = white;
            _skin.toggle.onHover.textColor = white;

            _hint = new GUIStyle(_skin.label) { fontSize = 12, wordWrap = true };
            _hint.normal.textColor = new Color(0.72f, 0.78f, 0.86f, 1f);
        }

        /// <summary>A button with a one-line plain-English description underneath. Returns true when clicked.</summary>
        public static bool Action(string label, string description)
        {
            bool clicked = GUILayout.Button(label);
            GUILayout.Label(description, Hint);
            return clicked;
        }

        public static string TextField(string label, string value, float width = 100f)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(130f));
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

        public string Draw(string[] options, float listHeight = 170f)
        {
            if (options == null || options.Length == 0)
            {
                GUILayout.Label("(nothing to choose)");
                return null;
            }
            if (Index < 0 || Index >= options.Length) Index = 0;

            if (GUILayout.Button((_open ? "^ " : "v ") + options[Index], GUILayout.MinWidth(220f)))
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
