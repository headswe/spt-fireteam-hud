using System.Collections.Generic;
using BepInEx.Logging;
using FireteamHud.Core;
using UnityEngine;

namespace FireteamHud
{
    /// <summary>Draws the radar and name list with IMGUI from the shared <see cref="SquadTracker"/>.</summary>
    public class HudRenderer : MonoBehaviour
    {
        public HudConfig Config;
        public SquadTracker Squad;

        bool _hiddenByKey;
        GUIStyle _label;
        GUIStyle _labelRight;
        int _styleFontSize;

        SquadRoster _roster => Squad.Roster;
        SelfPose _self => Squad.Self;

        void Update()
        {
            if (Config.ToggleKey.Value.IsDown()) _hiddenByKey = !_hiddenByKey;
        }

        void OnGUI()
        {
            if (Event.current.type != EventType.Repaint) return;
            if (!Config.Enabled.Value || _hiddenByKey) return;
            bool preview = Config.Preview.Value;
            if (!preview && (!Squad.InRaid || _roster.Members.Count == 0)) return;
            if (!Config.ShowRadar.Value && !Config.ShowNameList.Value) return;

            float scale = Config.Scale.Value;
            float radarSize = Config.ShowRadar.Value ? Config.RadarSize.Value * scale : 0f;
            float listWidth = Config.ShowNameList.Value ? Config.NameListWidth.Value * scale : 0f;
            float gap = radarSize > 0 && listWidth > 0 ? 8f * scale : 0f;
            float rowHeight = Config.FontSize.Value * scale * 1.5f;

            var visible = VisibleMembers();
            float listHeight = listWidth > 0 ? Mathf.Max(1, visible.Count) * rowHeight + 8f * scale : 0f;
            float width = radarSize + gap + listWidth;
            float height = Mathf.Max(radarSize, listHeight);

            var area = HudMath.Place(Config.Anchor.Value, Config.OffsetX.Value, Config.OffsetY.Value,
                width, height, Screen.width, Screen.height);

            bool listLeft = Config.NameListSide.Value == ListSide.Left;
            float radarX = listLeft ? area.X + listWidth + gap : area.X;
            float listX = listLeft ? area.X : area.X + radarSize + gap;

            var saved = GUI.color;
            if (radarSize > 0) DrawRadar(new Rect(radarX, area.Y + (height - radarSize) / 2f, radarSize, radarSize), scale);
            if (listWidth > 0) DrawList(new Rect(listX, area.Y + (height - listHeight) / 2f, listWidth, listHeight), visible, rowHeight, scale);
            GUI.color = saved;
        }

        List<SquadMember> VisibleMembers()
        {
            var list = new List<SquadMember>();
            foreach (var m in _roster.Members)
                if (m.Status == MemberStatus.Alive || Config.ShowGone.Value) list.Add(m);
            return list;
        }

        void DrawRadar(Rect rect, float scale)
        {
            float opacity = Config.Opacity.Value;
            float radius = rect.width / 2f;
            var centre = rect.center;

            if (Config.BackgroundOpacity.Value > 0)
            {
                GUI.color = new Color(0, 0, 0, Config.BackgroundOpacity.Value);
                GUI.DrawTexture(rect, Textures.Disc);
            }
            GUI.color = new Color(1, 1, 1, 0.35f * opacity);
            GUI.DrawTexture(rect, Textures.ThinRing);
            if (Config.ShowRangeRing.Value)
            {
                GUI.color = new Color(1, 1, 1, 0.15f * opacity);
                GUI.DrawTexture(Square(centre, radius), Textures.ThinRing);
            }

            float dot = Config.DotSize.Value * scale;
            // Keep dots inside the rim.
            float usable = radius - dot / 2f;

            foreach (var m in _roster.Members)
            {
                if (m.Status == MemberStatus.Gone) continue;
                var p = HudMath.Project(m.X - _self.Position.x, m.Z - _self.Position.z, _self.Yaw, Config.Range.Value, usable);
                if (p.Clamped && !Config.ShowOutOfRange.Value) continue;

                var pos = new Vector2(centre.x + p.Offset.X, centre.y + p.Offset.Y);
                var colour = Squad.ColourOf(m);
                colour.a *= opacity;
                GUI.color = colour;

                float size = p.Clamped ? dot * 0.85f : dot;
                GUI.DrawTexture(Square(pos, size), p.Clamped ? Textures.Ring : Textures.Disc);
                if (m.Downed)
                {
                    GUI.color = Downed.Pulse(opacity);
                    GUI.DrawTexture(Square(pos, size * 1.9f), Textures.Ring);
                    GUI.color = colour;
                }

                float threshold = Config.ElevationThreshold.Value;
                if (threshold > 0 && m.Status == MemberStatus.Alive)
                {
                    var elevation = HudMath.ElevationOf(m.Y - _self.Position.y, threshold);
                    float tri = size * 0.6f;
                    if (elevation == Elevation.Above)
                        GUI.DrawTexture(new Rect(pos.x - tri / 2f, pos.y - size / 2f - tri - 1f, tri, tri), Textures.TriangleUp);
                    else if (elevation == Elevation.Below)
                        GUI.DrawTexture(new Rect(pos.x - tri / 2f, pos.y + size / 2f + 1f, tri, tri), Textures.TriangleDown);
                }
            }

            if (Config.ShowSelf.Value)
            {
                var self = Config.SelfColour.Value;
                self.a *= opacity;
                GUI.color = self;
                float s = dot * 1.1f;
                GUI.DrawTexture(Square(centre, s), Textures.TriangleUp);
            }
        }

        void DrawList(Rect rect, List<SquadMember> members, float rowHeight, float scale)
        {
            EnsureStyles(Mathf.RoundToInt(Config.FontSize.Value * scale));
            float opacity = Config.Opacity.Value;
            float pad = 4f * scale;

            if (Config.BackgroundOpacity.Value > 0)
            {
                GUI.color = new Color(0, 0, 0, Config.BackgroundOpacity.Value);
                GUI.DrawTexture(rect, Textures.Pixel);
            }

            float y = rect.y + pad;
            float swatch = rowHeight * 0.45f;
            foreach (var m in members)
            {
                bool alive = m.Status == MemberStatus.Alive;
                var colour = Squad.ColourOf(m);
                colour.a *= opacity;

                GUI.color = colour;
                GUI.DrawTexture(new Rect(rect.x + pad, y + (rowHeight - swatch) / 2f, swatch, swatch), Textures.Disc);

                var nameRect = new Rect(rect.x + pad * 2 + swatch, y, rect.width - pad * 3 - swatch, rowHeight);
                string suffix = m.Status == MemberStatus.Dead ? "KIA" : m.Status == MemberStatus.Gone ? "—" : null;
                if (suffix == null && Config.ShowDistance.Value)
                {
                    float dx = m.X - _self.Position.x, dz = m.Z - _self.Position.z;
                    suffix = HudMath.FormatDistance(Mathf.Sqrt(dx * dx + dz * dz));
                }

                Shadowed(nameRect, m.Name, _label, colour);
                if (m.Downed) Shadowed(nameRect, "DOWNED", _labelRight, Downed.Pulse(opacity));
                else if (suffix != null) Shadowed(nameRect, suffix, _labelRight, colour);
                y += rowHeight;
            }
        }

        void Shadowed(Rect rect, string text, GUIStyle style, Color colour)
        {
            GUI.color = new Color(0, 0, 0, colour.a * 0.8f);
            GUI.Label(new Rect(rect.x + 1, rect.y + 1, rect.width, rect.height), text, style);
            GUI.color = colour;
            GUI.Label(rect, text, style);
        }

        void EnsureStyles(int fontSize)
        {
            if (_label != null && _styleFontSize == fontSize) return;
            _styleFontSize = fontSize;
            _label = new GUIStyle(GUI.skin.label)
            {
                fontSize = fontSize,
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip,
                wordWrap = false,
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0)
            };
            _label.normal.textColor = Color.white;
            _labelRight = new GUIStyle(_label) { alignment = TextAnchor.MiddleRight };
        }

        static Rect Square(Vector2 centre, float size) => new Rect(centre.x - size / 2f, centre.y - size / 2f, size, size);

        static Color ToColor(Rgba c) => new Color(c.R, c.G, c.B, c.A);
    }
}
