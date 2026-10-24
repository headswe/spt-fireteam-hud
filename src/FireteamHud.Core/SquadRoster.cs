using System.Collections.Generic;

namespace FireteamHud.Core
{
    public enum MemberStatus
    {
        Alive,
        /// <summary>Seen dead.</summary>
        Dead,
        /// <summary>No longer in the raid: extracted, disconnected, or died out of sight.</summary>
        Gone
    }

    /// <summary>What the game reports about a teammate this frame.</summary>
    public struct MemberSighting
    {
        public string Id;
        public string Name;
        public float X, Y, Z;
        public bool Alive;
        /// <summary>Fika revive: down and bleeding out, waiting for a teammate.</summary>
        public bool Downed;
        /// <summary>0..1 total health, or -1 when unknown.</summary>
        public float Health;

        public MemberSighting(string id, string name, float x, float y, float z, bool alive)
        {
            Id = id; Name = name; X = x; Y = y; Z = z; Alive = alive; Downed = false; Health = -1f;
        }
    }

    public class SquadMember
    {
        public string Id { get; internal set; }
        public string Name { get; internal set; }
        /// <summary>Order the member joined the roster; drives the default colour and list order.</summary>
        public int JoinIndex { get; internal set; }
        public float X { get; internal set; }
        public float Y { get; internal set; }
        public float Z { get; internal set; }
        public MemberStatus Status { get; internal set; }
        public bool Downed { get; internal set; }
        /// <summary>0..1 total health, or -1 when unknown.</summary>
        public float Health { get; internal set; } = -1f;
    }

    /// <summary>
    /// Keeps a stable list of teammates for one raid, so people keep their slot and colour
    /// and stay listed (greyed out) after they die or leave.
    /// </summary>
    public class SquadRoster
    {
        readonly Dictionary<string, SquadMember> _byId = new Dictionary<string, SquadMember>();
        readonly List<SquadMember> _members = new List<SquadMember>();
        readonly HashSet<string> _seen = new HashSet<string>();

        public IReadOnlyList<SquadMember> Members => _members;

        public void Update(IEnumerable<MemberSighting> sightings)
        {
            _seen.Clear();
            foreach (var s in sightings)
            {
                if (string.IsNullOrEmpty(s.Id) || !_seen.Add(s.Id)) continue;
                if (!_byId.TryGetValue(s.Id, out var m))
                {
                    m = new SquadMember { Id = s.Id, JoinIndex = _members.Count };
                    _byId[s.Id] = m;
                    _members.Add(m);
                }
                m.Name = string.IsNullOrEmpty(s.Name) ? s.Id : s.Name;
                m.X = s.X; m.Y = s.Y; m.Z = s.Z;
                m.Status = s.Alive ? MemberStatus.Alive : MemberStatus.Dead;
                m.Downed = s.Alive && s.Downed;
                m.Health = s.Health;
            }

            foreach (var m in _members)
            {
                if (!_seen.Contains(m.Id) && m.Status == MemberStatus.Alive)
                {
                    m.Status = MemberStatus.Gone;
                    m.Downed = false;
                }
            }
        }

        public void Clear()
        {
            _byId.Clear();
            _members.Clear();
        }
    }
}
