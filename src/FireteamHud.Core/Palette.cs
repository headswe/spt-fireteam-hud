using System;
using System.Collections.Generic;

namespace FireteamHud.Core
{
    public struct Rgba
    {
        public float R, G, B, A;
        public Rgba(float r, float g, float b, float a = 1f) { R = r; G = g; B = b; A = a; }
    }

    /// <summary>
    /// Player colours. The order is the Auto order: STHUD's four fireteam colours first, then
    /// the most distinct remaining ones, so a full Fika lobby still gets easy-to-tell colours.
    /// Indexes match the <c>SquadColour</c> dropdown (index + 1; 0 is Auto).
    /// </summary>
    public static class Palette
    {
        public static readonly Rgba Red = new Rgba(0.93f, 0.26f, 0.26f);
        public static readonly Rgba Green = new Rgba(0.30f, 0.85f, 0.35f);
        public static readonly Rgba Blue = new Rgba(0.30f, 0.55f, 1.00f);
        public static readonly Rgba Yellow = new Rgba(0.98f, 0.86f, 0.25f);
        public static readonly Rgba White = new Rgba(0.95f, 0.95f, 0.95f);
        public static readonly Rgba Dead = new Rgba(0.45f, 0.45f, 0.45f, 0.8f);

        public static readonly string[] Names =
        {
            "Red", "Green", "Blue", "Yellow", "Orange", "Purple", "Cyan", "Pink", "White", "Lime",
            "Magenta", "Sky", "Gold", "Teal", "Coral", "Lavender", "Mint", "Crimson", "Peach", "Olive"
        };

        public static readonly Rgba[] Colours =
        {
            Red, Green, Blue, Yellow,
            new Rgba(1.00f, 0.55f, 0.15f), // orange
            new Rgba(0.75f, 0.40f, 1.00f), // purple
            new Rgba(0.25f, 0.90f, 0.90f), // cyan
            new Rgba(1.00f, 0.45f, 0.75f), // pink
            White,
            new Rgba(0.70f, 1.00f, 0.20f), // lime
            new Rgba(1.00f, 0.20f, 1.00f), // magenta
            new Rgba(0.55f, 0.80f, 1.00f), // sky
            new Rgba(0.85f, 0.65f, 0.13f), // gold
            new Rgba(0.10f, 0.70f, 0.60f), // teal
            new Rgba(1.00f, 0.50f, 0.45f), // coral
            new Rgba(0.70f, 0.65f, 1.00f), // lavender
            new Rgba(0.60f, 1.00f, 0.75f), // mint
            new Rgba(0.75f, 0.05f, 0.25f), // crimson
            new Rgba(1.00f, 0.80f, 0.60f), // peach
            new Rgba(0.60f, 0.65f, 0.20f)  // olive
        };

        public static int Count => Colours.Length;

        /// <summary>Colour for the n-th teammate seen (0-based), wrapping around.</summary>
        public static Rgba ForIndex(int index)
        {
            if (index < 0) index = 0;
            return Colours[index % Colours.Length];
        }

        /// <summary>Palette index of a colour within <paramref name="tolerance"/> per channel, or -1.</summary>
        public static int IndexOf(Rgba c, float tolerance = 0.01f)
        {
            for (int i = 0; i < Colours.Length; i++)
            {
                var p = Colours[i];
                if (Math.Abs(p.R - c.R) <= tolerance && Math.Abs(p.G - c.G) <= tolerance && Math.Abs(p.B - c.B) <= tolerance)
                    return i;
            }
            return -1;
        }

        public const int Auto = -1;
        public const int Custom = -2;

        /// <summary>
        /// Resolves the colour slot for each member, in join order. <paramref name="picks"/>
        /// holds a palette index for a hand-picked colour, <see cref="Auto"/> or
        /// <see cref="Custom"/>. Auto members get the first palette colour nobody has picked
        /// or been given yet, so automatic colours never clash with chosen ones. Custom stays
        /// Custom. Only when all colours are taken does Auto fall back to join order.
        /// </summary>
        public static int[] Assign(IReadOnlyList<int> picks)
        {
            var result = new int[picks.Count];
            var used = new HashSet<int>();
            foreach (var p in picks)
                if (p >= 0) used.Add(p % Colours.Length);

            int cursor = 0;
            for (int i = 0; i < picks.Count; i++)
            {
                int pick = picks[i];
                if (pick != Auto) { result[i] = pick >= 0 ? pick % Colours.Length : pick; continue; }

                while (cursor < Colours.Length && used.Contains(cursor)) cursor++;
                if (cursor < Colours.Length)
                {
                    result[i] = cursor;
                    used.Add(cursor);
                }
                else
                {
                    result[i] = i % Colours.Length;
                }
            }
            return result;
        }
    }
}
