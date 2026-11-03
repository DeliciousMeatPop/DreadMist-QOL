using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace DreadMistQOL
{
    /// <summary>
    /// Optional cheat panel. Every button calls the game's own console command handler
    /// (QFSW.QC.ConsoleCommands), so behaviour is identical to typing the command with ~.
    /// Nothing here runs unless Plugin.EnableCheatPanel is true.
    /// </summary>
    internal sealed class CheatPanel
    {
        private sealed class ItemEntry
        {
            public string Key;
            public string Name;
            public string Lower;
        }

        private static readonly string[] TabNames = { "Items", "Progress", "Perks", "Character", "World", "Commands" };

        private Rect _window = new Rect(40f, 40f, 680f, 740f);
        private Rect _bar = new Rect(40f, 40f, 420f, 64f);
        private bool _minimized;

        public bool Visible;

        public void Expand() { _minimized = false; }
        private int _tab;
        private string _status = "Ready. Load a game first, then pick an action.";

        // Items tab
        private List<ItemEntry> _items;
        private string _search = "";
        private ItemEntry _selected;
        private string _itemQty = "1";
        private string _coinQty = "1000";
        private Vector2 _itemScroll;

        // Progress tab
        private readonly Dropdown _statDrop = new Dropdown();
        private string _xpAmount = "100";
        private string _levelValue = "10";
        private string _allXp = "100";

        // Perks tab
        private readonly Dropdown _perkGroupDrop = new Dropdown();
        private string _perkGroupShown;
        private List<Dreadmist.Definitions.PerkDefinition> _perks = new List<Dreadmist.Definitions.PerkDefinition>();
        private Dreadmist.Definitions.PerkDefinition _perkSelected;
        private Vector2 _perkScroll;
        private static string[] _perkGroupNames;

        // Character tab
        private string _hygiene = "100";
        private string _fame = "10";
        private string _honor = "10";
        private bool _confirmKill;

        // World tab
        private float _timeOfDay = 0.5f;

        private Vector2 _tabScroll;

        public void Draw()
        {
            GUISkin previous = GUI.skin;
            GUI.skin = Ui.Skin; // our dark, readable skin only applies to our windows
            try
            {
                if (_minimized)
                    _bar = GUILayout.Window(0x51AA, _bar, DrawBar, "DreadMist QOL");
                else
                    _window = GUILayout.Window(0x51A9, _window, DrawWindow, "DreadMist QOL - optional cheat panel");
            }
            finally
            {
                GUI.skin = previous;
            }
        }

        /// <summary>Compact strip shown while minimized: stays on screen but covers almost nothing.</summary>
        private void DrawBar(int id)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(Shorten(_status, 46));
            if (GUILayout.Button("Open", GUILayout.Width(60f))) _minimized = false;
            if (GUILayout.Button("X", GUILayout.Width(32f))) Visible = false;
            GUILayout.EndHorizontal();
            GUI.DragWindow(new Rect(0f, 0f, 10000f, 24f));
        }

        private static string Shorten(string t, int max)
        {
            if (t == null) return "";
            return t.Length <= max ? t : t.Substring(0, max - 1) + "...";
        }

        private void DrawWindow(int id)
        {
            // Window buttons in the title bar: minimize and close.
            if (GUI.Button(new Rect(_window.width - 74f, 5f, 32f, 22f), "_")) _minimized = true;
            if (GUI.Button(new Rect(_window.width - 38f, 5f, 32f, 22f), "X")) Visible = false;

            _tab = GUILayout.Toolbar(_tab, TabNames);
            GUILayout.Label("Status: " + _status, Ui.Hint);

            _tabScroll = GUILayout.BeginScrollView(_tabScroll);
            switch (_tab)
            {
                case 0: DrawItems(); break;
                case 1: DrawProgress(); break;
                case 2: DrawPerks(); break;
                case 3: DrawCharacter(); break;
                case 4: DrawWorld(); break;
                case 5: DrawCommands(); break;
            }
            GUILayout.EndScrollView();

            GUILayout.Label("Actions affect the first selected character. Drag the title bar to move. _ minimizes, X closes (hotkey reopens).", Ui.Hint);
            GUI.DragWindow(new Rect(0f, 0f, 10000f, 26f));
        }

        // ---------------------------------------------------------------- helpers

        private static bool HasCharacter()
        {
            return Dreadmist.PlayerManager.Exists && Dreadmist.PlayerManager.Instance.SelectedEntities.Count > 0;
        }

        /// <summary>Runs one action with error handling, status text and a log line.</summary>
        private void Run(string what, Action action, bool needsCharacter = true)
        {
            try
            {
                if (needsCharacter && !HasCharacter())
                {
                    _status = "No character selected. Load a game and select a character first.";
                    return;
                }
                action();
                _status = "Done: " + what;
                Plugin.Log.LogInfo("Cheat panel ran: " + what);
            }
            catch (Exception ex)
            {
                Exception e = (ex is TargetInvocationException && ex.InnerException != null) ? ex.InnerException : ex;
                _status = "FAILED: " + what + " (" + e.Message + ") - see BepInEx\\LogOutput.log";
                Plugin.Log.LogError("Cheat panel failed: " + what + "\n" + e);
            }
        }

        // ---------------------------------------------------------------- Items

        private void EnsureItems()
        {
            if (_items != null) return;
            _items = new List<ItemEntry>();
            try
            {
                foreach (var def in Dreadmist.ResourceManager.Instance.ItemDefinitions)
                {
                    string name = null;
                    try { name = def.DisplayNameLocalized; } catch { /* localization not ready */ }
                    if (string.IsNullOrEmpty(name)) name = def.Key;
                    _items.Add(new ItemEntry { Key = def.Key, Name = name, Lower = (name + " " + def.Key).ToLowerInvariant() });
                }
                _items = _items.OrderBy(i => i.Name, StringComparer.OrdinalIgnoreCase).ToList();
                Plugin.Log.LogInfo("Cheat panel loaded " + _items.Count + " item definitions.");
            }
            catch (Exception e)
            {
                _items = null; // try again next time (e.g. still on the main menu)
                _status = "Item list not available yet (is a game loaded?): " + e.Message;
            }
        }

        private void DrawItems()
        {
            GUILayout.Label("Give an item", GUI.skin.box);
            EnsureItems();
            if (_items == null) return;

            _search = Ui.TextField("Search", _search, 260f);
            string q = (_search ?? "").Trim().ToLowerInvariant();
            var matches = _items.Where(i => q.Length == 0 || i.Lower.Contains(q)).Take(80).ToList();

            _itemScroll = GUILayout.BeginScrollView(_itemScroll, GUILayout.Height(170f));
            foreach (var m in matches)
            {
                if (GUILayout.Button(m.Name + "   [" + m.Key + "]", GUILayout.ExpandWidth(true)))
                    _selected = m;
            }
            GUILayout.EndScrollView();
            if (matches.Count == 80) GUILayout.Label("Showing the first 80 matches. Type more to narrow it down.", Ui.Hint);

            GUILayout.Label("Selected: " + (_selected == null ? "(none)" : _selected.Name + "  [" + _selected.Key + "]"));
            _itemQty = Ui.TextField("Quantity", _itemQty);
            if (GUILayout.Button("Give item"))
            {
                if (_selected == null) _status = "Pick an item from the list first.";
                else
                {
                    int qty = Ui.ParseInt(_itemQty, 1, 1, 100000);
                    string key = _selected.Key;
                    Run("GiveItem " + key + " " + qty, () => QFSW.QC.ConsoleCommands.GiveItem(key, qty));
                }
            }
            GUILayout.Label("Adds the chosen item (and quantity) to the selected character's inventory. Same as: GiveItem <key> <quantity>.", Ui.Hint);

            GUILayout.Space(8f);
            GUILayout.Label("Coins", GUI.skin.box);
            _coinQty = Ui.TextField("Amount", _coinQty);
            if (Ui.Action("Give coins", "Adds that many coins. Same as: GiveCoin <amount>."))
            {
                int amount = Ui.ParseInt(_coinQty, 1000, 1, 100000000);
                Run("GiveCoin " + amount, () => QFSW.QC.ConsoleCommands.GiveCoin(amount));
            }
            if (Ui.Action("Give Dev Orb (storage relief)", "Gives Misc_DevOrb. The developer described it as helping with carrying weight/space; exact effect not yet verified here."))
                Run("GiveItem Misc_DevOrb", () =>
                {
                    if (!Dreadmist.ResourceManager.Instance.CheckForItemDefinition("Misc_DevOrb"))
                        throw new Exception("Misc_DevOrb does not exist in this game version");
                    QFSW.QC.ConsoleCommands.GiveItem("Misc_DevOrb", 1);
                });
            if (Ui.Action("Give test items", "Gives the developer's test item list (Starting_Player_Testing). Same as: GiveTestItems."))
                Run("GiveTestItems", () => QFSW.QC.ConsoleCommands.GiveTestItems());
        }

        // ---------------------------------------------------------------- Progress

        private void DrawProgress()
        {
            GUILayout.Label("Attribute or skill", GUI.skin.box);
            string stat = _statDrop.Draw(Names.AttributesAndSkills, 160f);
            if (stat == null) return;

            _xpAmount = Ui.TextField("Experience", _xpAmount);
            if (Ui.Action("Add experience", "Adds experience to the chosen attribute or skill (can cause level-ups and perk points). Same as: AddExperience <name> <amount>."))
            {
                float amount = Ui.ParseFloat(_xpAmount, 100f);
                Run("AddExperience " + stat + " " + amount, () => QFSW.QC.ConsoleCommands.AddExperience(stat, amount));
            }

            _levelValue = Ui.TextField("Level", _levelValue);
            if (Ui.Action("Set level", "Sets the chosen attribute or skill to exactly this level. Same as: SetLevel <name> <level>."))
            {
                float level = Ui.ParseFloat(_levelValue, 10f);
                Run("SetLevel " + stat + " " + level, () => QFSW.QC.ConsoleCommands.SetLevel(stat, level));
            }

            GUILayout.Space(8f);
            GUILayout.Label("Everything at once", GUI.skin.box);
            _allXp = Ui.TextField("Experience each", _allXp);
            float all = Ui.ParseFloat(_allXp, 100f);
            if (Ui.Action("Add to all attributes", "Adds experience to all 7 attributes. Same as: AddExperienceAttributeAll <amount>."))
                Run("AddExperienceAttributeAll " + all, () => QFSW.QC.ConsoleCommands.AddExperienceAttributeAll(all));
            if (Ui.Action("Add to all skills", "Adds experience to every skill. Same as: AddExperienceSkillAll <amount>."))
                Run("AddExperienceSkillAll " + all, () => QFSW.QC.ConsoleCommands.AddExperienceSkillAll(all));
            if (Ui.Action("Add to everything", "Adds experience to all attributes and skills. Same as: AddExperienceAll <amount>."))
                Run("AddExperienceAll " + all, () => QFSW.QC.ConsoleCommands.AddExperienceAll(all));
        }

        // ---------------------------------------------------------------- Perks

        private static string[] PerkGroups()
        {
            if (_perkGroupNames == null)
            {
                // The enum's name/namespace isn't needed: take its type from PerkDefinition.Type.
                Type t = typeof(Dreadmist.Definitions.PerkDefinition).GetField("Type").FieldType;
                _perkGroupNames = Enum.GetNames(t);
            }
            return _perkGroupNames;
        }

        private void LoadPerks(string group)
        {
            _perks = new List<Dreadmist.Definitions.PerkDefinition>();
            _perkSelected = null;
            try
            {
                Type t = typeof(Dreadmist.Definitions.PerkDefinition).GetField("Type").FieldType;
                object value = Enum.Parse(t, group);
                var method = typeof(Dreadmist.ResourceManager).GetMethod("GetPerkDefinitionList");
                var list = method.Invoke(Dreadmist.ResourceManager.Instance, new[] { value }) as List<Dreadmist.Definitions.PerkDefinition>;
                if (list != null) _perks = list;
            }
            catch (Exception e)
            {
                _status = "Could not load perks for " + group + ": " + e.Message;
                Plugin.Log.LogError(e.ToString());
            }
        }

        private void DrawPerks()
        {
            GUILayout.Label("Perks for", GUI.skin.box);
            string group = _perkGroupDrop.Draw(PerkGroups(), 160f);
            if (group == null) return;
            if (group != _perkGroupShown)
            {
                _perkGroupShown = group;
                LoadPerks(group);
            }

            _perkScroll = GUILayout.BeginScrollView(_perkScroll, GUILayout.Height(170f));
            foreach (var p in _perks)
            {
                string label;
                try { label = p.DisplayNameLocalized; } catch { label = p.Key; }
                if (GUILayout.Button(label + "   [" + p.Key + "]")) _perkSelected = p;
            }
            GUILayout.EndScrollView();
            if (_perks.Count == 0) GUILayout.Label("No perks found for this group (or no game loaded).", Ui.Hint);

            GUILayout.Label("Selected: " + (_perkSelected == null ? "(none)" : _perkSelected.Key));
            if (GUILayout.Button("Give perk (one level)"))
            {
                if (_perkSelected == null) _status = "Pick a perk from the list first.";
                else
                {
                    string key = _perkSelected.Key;
                    Run("GivePerk " + key, () => QFSW.QC.ConsoleCommands.GivePerk(key));
                }
            }
            GUILayout.Label("Raises this perk by one level without spending a perk point and without checking the level requirement. Same as: GivePerk <key>.", Ui.Hint);
        }

        // ---------------------------------------------------------------- Character

        private void DrawCharacter()
        {
            if (Ui.Action("Revive all", "Revives every dead player character. Same as: ReviveAll."))
                Run("ReviveAll", () => QFSW.QC.ConsoleCommands.ReviveAll(), false);
            if (Ui.Action("Restore all stats", "Refills health, stamina, hunger and similar for your characters. Same as: RestoreAllStats."))
                Run("RestoreAllStats", () => QFSW.QC.ConsoleCommands.RestoreAllStats(), false);
            if (Ui.Action("Clear status effects", "Removes the selected character's active status effects. Same as: ClearStatusEffects."))
                Run("ClearStatusEffects", () => QFSW.QC.ConsoleCommands.ClearStatusEffects());

            GUILayout.Space(6f);
            _hygiene = Ui.TextField("Hygiene", _hygiene);
            if (Ui.Action("Set hygiene", "Sets hygiene to this value (default 100). Same as: SetHygiene <value>."))
            {
                float v = Ui.ParseFloat(_hygiene, 100f);
                Run("SetHygiene " + v, () => QFSW.QC.ConsoleCommands.SetHygiene(v));
            }

            _fame = Ui.TextField("Fame", _fame);
            if (Ui.Action("Add fame", "Adds (or with a minus sign removes) fame. Same as: AddFame <value>."))
            {
                float v = Ui.ParseFloat(_fame, 10f);
                Run("AddFame " + v, () => QFSW.QC.ConsoleCommands.AddFame(v), false);
            }
            _honor = Ui.TextField("Honor", _honor);
            if (Ui.Action("Add honor", "Adds (or removes) honor. Same as: AddHonor <value>."))
            {
                float v = Ui.ParseFloat(_honor, 10f);
                Run("AddHonor " + v, () => QFSW.QC.ConsoleCommands.AddHonor(v), false);
            }

            GUILayout.Space(10f);
            GUILayout.Label("Harmful actions (for testing)", GUI.skin.box);
            _confirmKill = GUILayout.Toggle(_confirmKill, "I understand these hurt my character");
            GUI.enabled = _confirmKill;
            if (Ui.Action("Kill character", "Kills the selected character. Same as: Unalive."))
                Run("Unalive", () => QFSW.QC.ConsoleCommands.Unalive(), false);
            if (Ui.Action("Add bleeding", "Adds the bleeding injury. Same as: AddInjuryBleeding."))
                Run("AddInjuryBleeding", () => QFSW.QC.ConsoleCommands.AddInjuryBleeding());
            if (Ui.Action("Add broken bone", "Adds a broken bone. Same as: AddInjuryBrokenBone."))
                Run("AddInjuryBrokenBone", () => QFSW.QC.ConsoleCommands.AddInjuryBrokenBone());
            if (Ui.Action("Add concussion", "Adds a concussion. Same as: AddInjuryConcussion."))
                Run("AddInjuryConcussion", () => QFSW.QC.ConsoleCommands.AddInjuryConcussion());
            GUI.enabled = true;
        }

        // ---------------------------------------------------------------- World

        private void DrawWorld()
        {
            if (Ui.Action("Unlock all recipes", "Unlocks every crafting recipe. Same as: UnlockAllRecipes."))
                Run("UnlockAllRecipes", () => QFSW.QC.ConsoleCommands.UnlockAllRecipes(), false);
            if (Ui.Action("Unlock all construction", "Unlocks every construction option. Same as: UnlockAllConstruction."))
                Run("UnlockAllConstruction", () => QFSW.QC.ConsoleCommands.UnlockAllConstruction(), false);
            if (Ui.Action("Instant grow", "Instantly grows planted crops. Same as: InstaGrow."))
                Run("InstaGrow", () => QFSW.QC.ConsoleCommands.InstaGrow(), false);
            if (Ui.Action("Teleport party here", "Moves your party to where the camera is looking. Same as: Teleport."))
                Run("Teleport", () => QFSW.QC.ConsoleCommands.Teleport(), false);
            if (Ui.Action("Toggle free camera", "Switches the free camera on or off. Same as: FreeCamera."))
                Run("FreeCamera", () => QFSW.QC.ConsoleCommands.FreeCamera(), false);

            GUILayout.Space(8f);
            GUILayout.Label("Time", GUI.skin.box);
            if (Ui.Action("Skip a day", "Advances the clock by one day. Same as: SkipDay."))
                Run("SkipDay", () => QFSW.QC.ConsoleCommands.SkipDay(), false);
            if (Ui.Action("Skip a week", "Advances the clock by one week. Same as: SkipWeek."))
                Run("SkipWeek", () => QFSW.QC.ConsoleCommands.SkipWeek(), false);
            if (Ui.Action("Set time to noon", "Sets the time of day to noon. Same as: SetTimeNoon."))
                Run("SetTimeNoon", () => QFSW.QC.ConsoleCommands.SetTimeNoon(), false);
            if (Ui.Action("Set time to midnight", "Sets the time of day to midnight. Same as: SetTimeMidNight."))
                Run("SetTimeMidNight", () => QFSW.QC.ConsoleCommands.SetTimeMidNight(), false);

            GUILayout.Label("Time of day: " + (_timeOfDay * 100f).ToString("0") + "% (0 = start of day, 100 = end)");
            _timeOfDay = GUILayout.HorizontalSlider(_timeOfDay, 0f, 1f);
            if (Ui.Action("Apply time of day", "Sets the time of day to the slider position. Same as: SetTimeOfDayPercentage <0..1>."))
            {
                float t = _timeOfDay;
                Run("SetTimeOfDayPercentage " + t.ToString("0.00"), () => QFSW.QC.ConsoleCommands.SetTimeOfDayPercentage(t), false);
            }

            GUILayout.Space(8f);
            GUILayout.Label("Weather", GUI.skin.box);
            if (Ui.Action("Normal weather", "Returns weather to the game's normal cycle. Same as: SetWeatherNormal."))
                Run("SetWeatherNormal", () => QFSW.QC.ConsoleCommands.SetWeatherNormal(), false);
            if (Ui.Action("Clear sky", "Forces clear weather. Same as: SetWeatherClear."))
                Run("SetWeatherClear", () => QFSW.QC.ConsoleCommands.SetWeatherClear(), false);
            if (Ui.Action("Cloudy", "Forces cloudy weather. Same as: SetWeatherCloudy."))
                Run("SetWeatherCloudy", () => QFSW.QC.ConsoleCommands.SetWeatherCloudy(), false);
            if (Ui.Action("Rain", "Forces rain. Same as: SetWeatherRain."))
                Run("SetWeatherRain", () => QFSW.QC.ConsoleCommands.SetWeatherRain(), false);
        }

        // ---------------------------------------------------------------- Commands (generic)

        private sealed class CmdEntry
        {
            public MethodInfo Method;
            public string Name;
            public ParameterInfo[] Params;
            public string[] Args;
        }

        private List<CmdEntry> _cmds;
        private string _cmdSearch = "";

        private void EnsureCommands()
        {
            if (_cmds != null) return;
            _cmds = new List<CmdEntry>();
            var methods = typeof(QFSW.QC.ConsoleCommands).GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
            foreach (var m in methods.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
            {
                var ps = m.GetParameters();
                var args = ps.Select(p => (p.HasDefaultValue && p.DefaultValue != null)
                    ? Convert.ToString(p.DefaultValue, CultureInfo.InvariantCulture) : "").ToArray();
                _cmds.Add(new CmdEntry { Method = m, Name = m.Name, Params = ps, Args = args });
            }
            Plugin.Log.LogInfo("Cheat panel found " + _cmds.Count + " console commands.");
        }

        private static object Convert1(string text, ParameterInfo p)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                if (p.HasDefaultValue) return p.DefaultValue;
                throw new Exception("'" + p.Name + "' needs a value");
            }
            if (p.ParameterType == typeof(string)) return text.Trim();
            return Convert.ChangeType(text.Trim().Replace(',', '.'), p.ParameterType, CultureInfo.InvariantCulture);
        }

        private void DrawCommands()
        {
            GUILayout.Label("Every console command the game has. Fill in the boxes (defaults are pre-filled) and click the command name.", Ui.Hint);
            EnsureCommands();
            _cmdSearch = Ui.TextField("Search", _cmdSearch, 260f);
            string q = (_cmdSearch ?? "").Trim().ToLowerInvariant();

            foreach (var c in _cmds)
            {
                if (q.Length > 0 && !c.Name.ToLowerInvariant().Contains(q)) continue;

                GUILayout.BeginHorizontal();
                if (GUILayout.Button(c.Name, GUILayout.Width(230f)))
                {
                    var entry = c;
                    string summary = c.Name + " " + string.Join(" ", c.Args);
                    Run(summary.Trim(), () =>
                    {
                        var values = new object[entry.Params.Length];
                        for (int i = 0; i < values.Length; i++) values[i] = Convert1(entry.Args[i], entry.Params[i]);
                        entry.Method.Invoke(null, values);
                    }, false);
                }
                for (int i = 0; i < c.Params.Length; i++)
                {
                    GUILayout.Label(c.Params[i].Name + ":", GUILayout.Width(110f));
                    c.Args[i] = GUILayout.TextField(c.Args[i] ?? "", GUILayout.Width(100f));
                }
                GUILayout.EndHorizontal();
            }
        }
    }
}
