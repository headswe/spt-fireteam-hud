using BepInEx;

namespace FireteamHud
{
    [BepInPlugin(Guid, "Fireteam HUD", Version)]
    public class Plugin : BaseUnityPlugin
    {
        public const string Guid = "com.headswe.fireteamhud";
        public const string Version = "0.2.0";

        HudConfig _config;

        void Awake()
        {
            var config = _config = new HudConfig(Config);
            Textures.Create();

            // One tracker feeds both views, so the HUD and the nametags always use the same colours.
            var squad = gameObject.AddComponent<SquadTracker>();
            squad.Config = config;
            squad.Log = Logger;

            var hud = gameObject.AddComponent<HudRenderer>();
            hud.Config = config;
            hud.Squad = squad;

            var tags = gameObject.AddComponent<NametagRenderer>();
            tags.Config = config;
            tags.Squad = squad;

            Logger.LogInfo("Fireteam HUD loaded. Settings are under F12 > Fireteam HUD.");
        }

        void Start()
        {
            // All plugins have finished Awake by now, so Fika's config exists.
            if (_config.DisableFikaPlates.Value) FikaCompat.DisableFikaNamePlates(Logger);
        }
    }
}
