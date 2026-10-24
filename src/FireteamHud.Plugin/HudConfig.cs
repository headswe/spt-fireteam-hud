using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using BepInEx.Configuration;
using FireteamHud.Core;
using UnityEngine;

namespace FireteamHud
{
    public enum TeammateSource
    {
        /// <summary>Fika's list of human players when Fika is loaded, otherwise same group id.</summary>
        Auto,
        FikaHumans,
        SameGroup
    }

    public enum ListSide { Left, Right }

    /// <summary>Per-player colour choice. Named values follow <see cref="Palette.Names"/>.</summary>
    public enum SquadColour
    {
        Auto,
        Red, Green, Blue, Yellow, Orange, Purple, Cyan, Pink, White, Lime,
        Magenta, Sky, Gold, Teal, Coral, Lavender, Mint, Crimson, Peach, Olive,
        Custom
    }

    public enum MarkerStyle { Chevron, Diamond, Dot }

    public enum NameMode
    {
        /// <summary>Names fade in for the teammate near your crosshair (Battlefield style).</summary>
        WhenLooking,
        Always,
        Never
    }

    public enum OpticMode
    {
        /// <summary>Project tags through the scope's zoomed view, like Fika does.</summary>
        Show,
        Hide
    }

    /// <summary>
    /// All settings, editable live in the BepInEx Configuration Manager (F12 by default).
    /// </summary>
    public class HudConfig
    {
        readonly ConfigFile _file;
        const string PlayerSection = "6. Player colours";
        const string CustomSection = "6b. Custom colours";

        readonly Dictionary<string, ConfigEntry<SquadColour>> _choices = new Dictionary<string, ConfigEntry<SquadColour>>();
        readonly Dictionary<string, ConfigEntry<Color>> _customColours = new Dictionary<string, ConfigEntry<Color>>();

        public ConfigEntry<bool> Enabled;
        public ConfigEntry<KeyboardShortcut> ToggleKey;
        public ConfigEntry<bool> Preview;
        public ConfigEntry<TeammateSource> Source;

        public ConfigEntry<HudAnchor> Anchor;
        public ConfigEntry<int> OffsetX;
        public ConfigEntry<int> OffsetY;
        public ConfigEntry<float> Scale;
        public ConfigEntry<float> Opacity;
        public ConfigEntry<float> BackgroundOpacity;

        public ConfigEntry<bool> ShowRadar;
        public ConfigEntry<int> RadarSize;
        public ConfigEntry<float> Range;
        public ConfigEntry<int> DotSize;
        public ConfigEntry<bool> ShowOutOfRange;
        public ConfigEntry<float> ElevationThreshold;
        public ConfigEntry<bool> ShowSelf;
        public ConfigEntry<bool> ShowRangeRing;

        public ConfigEntry<bool> ShowNameList;
        public ConfigEntry<ListSide> NameListSide;
        public ConfigEntry<int> NameListWidth;
        public ConfigEntry<int> FontSize;
        public ConfigEntry<bool> ShowDistance;
        public ConfigEntry<bool> ShowGone;

        public ConfigEntry<Color> SelfColour;

        public ConfigEntry<bool> TagsEnabled;
        public ConfigEntry<KeyboardShortcut> TagsToggleKey;
        public ConfigEntry<bool> DisableFikaPlates;
        public ConfigEntry<MarkerStyle> TagMarker;
        public ConfigEntry<int> TagMarkerSize;
        public ConfigEntry<NameMode> TagNames;
        public ConfigEntry<bool> TagUppercase;
        public ConfigEntry<int> TagFontSize;
        public ConfigEntry<bool> TagDistance;
        public ConfigEntry<bool> TagHealthBar;
        public ConfigEntry<float> TagFocusRadius;
        public ConfigEntry<float> TagMaxDistance;
        public ConfigEntry<float> TagFarScale;
        public ConfigEntry<float> TagHeight;
        public ConfigEntry<float> TagOpacity;
        public ConfigEntry<float> TagOccludedOpacity;
        public ConfigEntry<float> TagAdsOpacity;
        public ConfigEntry<OpticMode> TagOptics;
        public ConfigEntry<float> TagNearFade;
        public ConfigEntry<bool> TagEdgeArrows;
        public ConfigEntry<int> TagEdgeMargin;

        public HudConfig(ConfigFile file)
        {
            _file = file;

            const string general = "1. General";
            Enabled = file.Bind(general, "Enabled", true, "Show the HUD.");
            ToggleKey = file.Bind(general, "Toggle key", new KeyboardShortcut(KeyCode.Home), "Shows or hides the HUD in raid.");
            Preview = file.Bind(general, "Preview", false,
                "Shows four dummy teammates, also in the menus, so you can position and style the HUD. Turn off before playing.");
            Source = file.Bind(general, "Teammates", TeammateSource.Auto,
                "Who counts as your squad. Auto uses Fika's human players and falls back to players with your group id.");

            const string layout = "2. Layout";
            Anchor = file.Bind(layout, "Anchor", HudAnchor.BottomCenter, "Screen corner or edge the HUD is attached to.");
            OffsetX = file.Bind(layout, "Offset X", 0, new ConfigDescription("Pixels right (+) or left (-) of the anchor.",
                new AcceptableValueRange<int>(-4000, 4000)));
            OffsetY = file.Bind(layout, "Offset Y", -90, new ConfigDescription("Pixels down (+) or up (-) from the anchor.",
                new AcceptableValueRange<int>(-4000, 4000)));
            Scale = file.Bind(layout, "Scale", 1f, new ConfigDescription("Overall size multiplier.",
                new AcceptableValueRange<float>(0.5f, 3f)));
            Opacity = file.Bind(layout, "Opacity", 0.9f, new ConfigDescription("Opacity of dots and text.",
                new AcceptableValueRange<float>(0.1f, 1f)));
            BackgroundOpacity = file.Bind(layout, "Background opacity", 0.3f, new ConfigDescription("0 for no background.",
                new AcceptableValueRange<float>(0f, 1f)));

            const string radar = "3. Radar";
            ShowRadar = file.Bind(radar, "Show radar", true, "Heading-up radar with a dot per teammate.");
            RadarSize = file.Bind(radar, "Size", 150, new ConfigDescription("Diameter in pixels before scaling.",
                new AcceptableValueRange<int>(60, 600)));
            Range = file.Bind(radar, "Range (m)", 40f, new ConfigDescription("Distance from the centre to the edge of the radar.",
                new AcceptableValueRange<float>(5f, 500f)));
            DotSize = file.Bind(radar, "Dot size", 10, new ConfigDescription("Teammate dot diameter in pixels before scaling.",
                new AcceptableValueRange<int>(4, 40)));
            ShowOutOfRange = file.Bind(radar, "Show out of range", true, "Pin teammates beyond the range to the edge as hollow rings.");
            ElevationThreshold = file.Bind(radar, "Elevation arrows (m)", 3f, new ConfigDescription(
                "Draw an up or down arrow on a dot when the height difference exceeds this. 0 disables.",
                new AcceptableValueRange<float>(0f, 50f)));
            ShowSelf = file.Bind(radar, "Show self", true, "Arrow in the centre showing where you look.");
            ShowRangeRing = file.Bind(radar, "Show half-range ring", true, "Faint ring at half the range.");

            const string list = "4. Name list";
            ShowNameList = file.Bind(list, "Show name list", true, "Coloured list of teammates beside the radar.");
            NameListSide = file.Bind(list, "Side", ListSide.Right, "Which side of the radar the list goes on.");
            NameListWidth = file.Bind(list, "Width", 170, new ConfigDescription("List width in pixels before scaling.",
                new AcceptableValueRange<int>(80, 600)));
            FontSize = file.Bind(list, "Font size", 13, new ConfigDescription("Before scaling.", new AcceptableValueRange<int>(8, 40)));
            ShowDistance = file.Bind(list, "Show distance", true, "Distance to each teammate.");
            ShowGone = file.Bind(list, "Show dead and gone", true, "Keep teammates who died or left in the list, greyed out.");

            SelfColour = file.Bind("5. Colours", "Self", Color.white, "Colour of your own arrow.");

            const string tags = "7. Nametags";
            TagsEnabled = file.Bind(tags, "Enabled", true,
                "Markers and names over teammates, in the same colours as the HUD. Hidden when the Fika host disables name plates.");
            TagsToggleKey = file.Bind(tags, "Toggle key", new KeyboardShortcut(KeyCode.End), "Shows or hides the nametags.");
            DisableFikaPlates = file.Bind(tags, "Turn off Fika name plates", true,
                "Switches Fika's own name plates off at startup so they don't stack with these. Turn Fika's back on under Fika > Coop | Name Plates if you remove this plugin.");
            TagMarker = file.Bind(tags, "Marker", MarkerStyle.Chevron, "Shape over each teammate's head.");
            TagMarkerSize = file.Bind(tags, "Marker size", 16, new ConfigDescription("Pixels at close range.",
                new AcceptableValueRange<int>(6, 64)));
            TagNames = file.Bind(tags, "Names", NameMode.WhenLooking,
                "WhenLooking shows the name and distance only for the teammate near your crosshair; everyone else is just a marker.");
            TagUppercase = file.Bind(tags, "Uppercase names", true, "Easier to read at a glance.");
            TagFontSize = file.Bind(tags, "Font size", 14, new ConfigDescription("Pixels at close range.",
                new AcceptableValueRange<int>(8, 40)));
            TagDistance = file.Bind(tags, "Show distance", true, "Distance under the name.");
            TagHealthBar = file.Bind(tags, "Show health bar", false, "Thin health bar under the name.");
            TagFocusRadius = file.Bind(tags, "Focus radius", 0.12f, new ConfigDescription(
                "How close to the crosshair a teammate must be for their name to show, as a fraction of screen height.",
                new AcceptableValueRange<float>(0.02f, 0.5f)));
            TagMaxDistance = file.Bind(tags, "Max distance (m)", 500f, new ConfigDescription("Beyond this no tag is drawn.",
                new AcceptableValueRange<float>(20f, 2000f)));
            TagFarScale = file.Bind(tags, "Far scale", 0.65f, new ConfigDescription(
                "Size at 150 m and beyond relative to close range. Tags never shrink further, so they stay readable.",
                new AcceptableValueRange<float>(0.3f, 1f)));
            TagHeight = file.Bind(tags, "Height above head (m)", 0.45f, new ConfigDescription("Where the marker floats.",
                new AcceptableValueRange<float>(0f, 2f)));
            TagOpacity = file.Bind(tags, "Opacity", 1f, new ConfigDescription("", new AcceptableValueRange<float>(0.1f, 1f)));
            TagOccludedOpacity = file.Bind(tags, "Behind cover opacity", 0.5f, new ConfigDescription(
                "Opacity when a wall is between you and the teammate. Fades smoothly instead of popping. 0 hides them.",
                new AcceptableValueRange<float>(0f, 1f)));
            TagAdsOpacity = file.Bind(tags, "Aiming opacity", 0.6f, new ConfigDescription("Multiplier while aiming down sights.",
                new AcceptableValueRange<float>(0f, 1f)));
            TagOptics = file.Bind(tags, "In magnified optics", OpticMode.Show, "Show tags through zoomed scopes, or hide them.");
            TagNearFade = file.Bind(tags, "Fade within (m)", 3f, new ConfigDescription(
                "Tags fade out when a teammate is this close, so they don't cover their face.",
                new AcceptableValueRange<float>(0f, 15f)));
            TagEdgeArrows = file.Bind(tags, "Off-screen arrows", true, "Arrows at the screen edge pointing to teammates out of view.");
            TagEdgeMargin = file.Bind(tags, "Off-screen arrow margin", 70, new ConfigDescription("Distance of the arrows from the screen edge in pixels.",
                new AcceptableValueRange<int>(10, 400)));
        }

        /// <summary>
        /// A player's colour choice as a palette index, <see cref="Palette.Auto"/> or
        /// <see cref="Palette.Custom"/>. Each new nickname gets a dropdown under
        /// "Player colours" the first time you raid together.
        /// </summary>
        public int PickFor(string nickname)
        {
            var choice = Choice(nickname).Value;
            if (choice == SquadColour.Auto) return Palette.Auto;
            if (choice == SquadColour.Custom) return Palette.Custom;
            return (int)choice - 1;
        }

        /// <summary>The free-form colour used when a player's choice is Custom.</summary>
        public Color CustomColourFor(string nickname, Color fallback)
        {
            var key = SanitiseKey(nickname);
            if (!_customColours.TryGetValue(key, out var entry))
            {
                entry = _file.Bind(CustomSection, key, fallback,
                    "Used when " + nickname + "'s colour under Player colours is set to Custom.");
                _customColours[key] = entry;
            }
            return entry.Value;
        }

        ConfigEntry<SquadColour> Choice(string nickname)
        {
            var key = SanitiseKey(nickname);
            if (_choices.TryGetValue(key, out var entry)) return entry;

            var def = new ConfigDefinition(PlayerSection, key);
            var initial = SquadColour.Auto;

            // Version 0.2 stored a raw colour here. Keep what the player saw: a palette colour
            // becomes that named choice, anything else becomes Custom with the same value.
            if (TakeLegacyColour(def, out var legacy))
            {
                int index = Palette.IndexOf(new Rgba(legacy.r, legacy.g, legacy.b, legacy.a));
                if (index >= 0) initial = (SquadColour)(index + 1);
                else
                {
                    initial = SquadColour.Custom;
                    CustomColourFor(nickname, legacy);
                }
            }

            entry = _file.Bind(def, initial, new ConfigDescription(
                "Colour for " + nickname + " on the HUD and nametags. Auto takes the next colour nobody has picked; Custom uses the colour under Custom colours."));
            _choices[key] = entry;
            return entry;
        }

        /// <summary>
        /// Reads and removes a not-yet-bound value saved in the old colour format, so binding
        /// the new dropdown to the same key doesn't fail to parse it.
        /// </summary>
        bool TakeLegacyColour(ConfigDefinition def, out Color colour)
        {
            colour = default;
            try
            {
                var prop = typeof(ConfigFile).GetProperty("OrphanedEntries", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (!(prop?.GetValue(_file) is Dictionary<ConfigDefinition, string> orphans)) return false;
                if (!orphans.TryGetValue(def, out var raw) || string.IsNullOrEmpty(raw)) return false;
                if (Enum.IsDefined(typeof(SquadColour), raw.Trim())) return false; // already the new format
                if (!ColorUtility.TryParseHtmlString("#" + raw.Trim().TrimStart('#'), out colour)) return false;
                orphans.Remove(def);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>BepInEx config keys can't contain = \n \t \\ " ' [ ].</summary>
        static string SanitiseKey(string name)
        {
            if (string.IsNullOrEmpty(name)) return "Unknown";
            var sb = new StringBuilder(name.Length);
            foreach (var c in name) sb.Append("=\n\t\\\"'[]".IndexOf(c) >= 0 ? '_' : c);
            return sb.ToString().Trim();
        }
    }
}
