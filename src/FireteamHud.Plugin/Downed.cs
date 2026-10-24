using UnityEngine;

namespace FireteamHud
{
    /// <summary>Shared look for downed teammates (Fika revive), so HUD and nametags match.</summary>
    public static class Downed
    {
        public static readonly Color Red = new Color(1f, 0.22f, 0.2f, 1f);

        /// <summary>Red that pulses about once a second to draw the eye.</summary>
        public static Color Pulse(float opacity)
        {
            float k = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * Mathf.PI));
            var c = Red;
            c.a = k * opacity;
            return c;
        }
    }
}
