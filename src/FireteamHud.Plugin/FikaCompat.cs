using System;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;

namespace FireteamHud
{
    /// <summary>
    /// Talks to Fika through BepInEx (its config file and plugin instance) rather than its
    /// types, so Fika stays an optional, unreferenced dependency.
    /// </summary>
    public static class FikaCompat
    {
        const string FikaGuid = "com.fika.core";
        static readonly ConfigDefinition NamePlates = new ConfigDefinition("Coop | Name Plates", "Show Player Name Plates");

        static object _settings;
        static System.Reflection.PropertyInfo _allowProp;
        static float _nextCheck;
        static bool _allowed = true;

        /// <summary>Switches Fika's own name plates off so the two don't stack.</summary>
        public static void DisableFikaNamePlates(ManualLogSource log)
        {
            try
            {
                if (!Chainloader.PluginInfos.TryGetValue(FikaGuid, out var info) || info.Instance == null) return;
                var config = info.Instance.Config;
                if (!config.ContainsKey(NamePlates)) return;
                var entry = config[NamePlates];
                if (entry.BoxedValue is bool on && on)
                {
                    entry.BoxedValue = false;
                    log.LogInfo("Turned off Fika's built-in name plates (Fika > Coop | Name Plates).");
                }
            }
            catch (Exception e)
            {
                log.LogWarning("Could not change Fika's name plate setting: " + e.Message);
            }
        }

        /// <summary>
        /// False when the Fika host has disabled name plates for the raid
        /// (FikaPlugin.Instance.Settings.AllowNamePlates). Nametags respect that.
        /// Checked at most once a second.
        /// </summary>
        public static bool HostAllowsNamePlates(float now)
        {
            if (now < _nextCheck) return _allowed;
            _nextCheck = now + 1f;
            try
            {
                if (_settings == null)
                {
                    if (!Chainloader.PluginInfos.TryGetValue(FikaGuid, out var info) || info.Instance == null) return _allowed = true;
                    _settings = info.Instance.GetType().GetProperty("Settings")?.GetValue(info.Instance);
                    _allowProp = _settings?.GetType().GetProperty("AllowNamePlates");
                }
                _allowed = _allowProp == null || !(_allowProp.GetValue(_settings) is bool b) || b;
            }
            catch (Exception) { _allowed = true; }
            return _allowed;
        }
    }
}
