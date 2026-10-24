using System.Collections.Generic;
using BepInEx.Logging;
using EFT;
using FireteamHud.Core;
using UnityEngine;

namespace FireteamHud
{
    /// <summary>
    /// Single source of squad state for the HUD and the nametags, so both always agree on
    /// who is in the squad, their slot and therefore their colour.
    /// </summary>
    public class SquadTracker : MonoBehaviour
    {
        const float RosterInterval = 0.1f;

        public HudConfig Config;
        public ManualLogSource Log;

        SquadSource _source;
        bool _wasPreview;
        readonly Dictionary<string, Color> _colours = new Dictionary<string, Color>();
        readonly List<int> _picks = new List<int>();
        float _nextRoster;

        public SquadRoster Roster { get; } = new SquadRoster();
        public SelfPose Self { get; private set; }
        public bool InRaid { get; private set; }
        public Player MainPlayer { get; private set; }

        static readonly string[] PreviewNames = { "Alpha", "Bravo", "Charlie", "Delta" };

        void Start() => _source = new SquadSource(Log);

        /// <summary>The live player object for a roster member, or null (preview dummies, players who left).</summary>
        public Player PlayerFor(SquadMember member) =>
            member != null && _source != null && _source.Players.TryGetValue(member.Id, out var p) && p != null ? p : null;

        /// <summary>Colour of a member as shown everywhere: dead grey, otherwise their resolved colour.</summary>
        public Color ColourOf(SquadMember member)
        {
            if (member.Status != MemberStatus.Alive) return ToColor(Palette.Dead);
            return _colours.TryGetValue(member.Id, out var c) ? c : Color.white;
        }

        /// <summary>
        /// Resolves everyone's colour from their dropdowns in one pass, so Auto colours avoid
        /// the ones picked by hand. Runs every frame; it is a handful of dictionary lookups.
        /// </summary>
        void ResolveColours()
        {
            var members = Roster.Members;
            _picks.Clear();
            foreach (var m in members) _picks.Add(Config.PickFor(m.Name));
            var slots = Palette.Assign(_picks);

            _colours.Clear();
            for (int i = 0; i < members.Count; i++)
            {
                var m = members[i];
                _colours[m.Id] = slots[i] == Palette.Custom
                    ? Config.CustomColourFor(m.Name, ToColor(Palette.ForIndex(m.JoinIndex)))
                    : ToColor(Palette.ForIndex(slots[i]));
            }
        }

        static Color ToColor(Rgba c) => new Color(c.R, c.G, c.B, c.A);

        void Update()
        {
            UpdateRoster();
            ResolveColours();
        }

        void UpdateRoster()
        {
            if (Config.Preview.Value != _wasPreview)
            {
                _wasPreview = Config.Preview.Value;
                Roster.Clear(); // don't leave dummies (or real mates) behind as "gone"
            }

            var player = _source.MainPlayer();
            MainPlayer = player;
            if (player == null)
            {
                if (InRaid) { Roster.Clear(); _source.Reset(); _source.Players.Clear(); }
                InRaid = false;
                Self = new SelfPose();
                if (Config.Preview.Value) UpdatePreview();
                return;
            }

            if (!InRaid) Roster.Clear(); // new raid, new roster
            InRaid = true;
            Self = _source.PoseOf(player);

            if (Config.Preview.Value) { UpdatePreview(); return; }
            if (Time.unscaledTime < _nextRoster) return;
            _nextRoster = Time.unscaledTime + RosterInterval;
            Roster.Update(_source.Teammates(player, Config.Source.Value));
        }

        /// <summary>
        /// Four dummies circling at different distances and heights: one above, one below and
        /// downed, one beyond radar range, and one that keeps dying.
        /// </summary>
        void UpdatePreview()
        {
            float t = Time.unscaledTime;
            float range = Config.Range.Value;
            var sightings = new List<MemberSighting>();
            for (int i = 0; i < PreviewNames.Length; i++)
            {
                float angle = t * (0.3f + 0.1f * i) + i * Mathf.PI / 2f;
                float dist = range * (0.25f + 0.3f * i);
                float height = i == 1 ? 6f : i == 2 ? -6f : 0f;
                bool alive = i != 3 || (int)(t / 4f) % 2 == 0;
                sightings.Add(new MemberSighting("preview-" + i, PreviewNames[i],
                    Self.Position.x + Mathf.Cos(angle) * dist,
                    Self.Position.y + height,
                    Self.Position.z + Mathf.Sin(angle) * dist,
                    alive)
                {
                    Downed = i == 2,
                    Health = alive ? 1f - 0.25f * i : 0f
                });
            }
            Roster.Update(sightings);
        }
    }
}
