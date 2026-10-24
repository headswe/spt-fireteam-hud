using System.Collections.Generic;
using System.Linq;
using EFT.CameraControl;
using FireteamHud.Core;
using UnityEngine;

namespace FireteamHud
{
    /// <summary>
    /// Battlefield-style teammate markers: a crisp coloured marker over each head, with the
    /// name and distance fading in only for the mate near your crosshair. The colours come
    /// from <see cref="SquadTracker.ColourOf"/>, the same source as the HUD.
    /// </summary>
    public class NametagRenderer : MonoBehaviour
    {
        const float NearScaleDistance = 5f;
        const float FarScaleDistance = 150f;
        const float FadeSpeed = 4f;       // opacity units per second when moving in or out of cover
        const float AssumedNeckHeight = 1.5f;

        public HudConfig Config;
        public SquadTracker Squad;

        bool _hiddenByKey;
        int _occlusionCursor;
        readonly Dictionary<string, bool> _occluded = new Dictionary<string, bool>();
        readonly Dictionary<string, float> _visibility = new Dictionary<string, float>();
        readonly Dictionary<int, GUIStyle> _nameStyles = new Dictionary<int, GUIStyle>();
        readonly Dictionary<int, GUIStyle> _smallStyles = new Dictionary<int, GUIStyle>();
        Font _font;
        bool _fontSearched;

        static readonly int OcclusionMask = LayerMask.GetMask("HighPolyCollider", "Terrain", "Player");
        static readonly int PlayerLayer = LayerMask.NameToLayer("Player");

        void Update()
        {
            if (Config.TagsToggleKey.Value.IsDown()) _hiddenByKey = !_hiddenByKey;
            if (!Squad.InRaid) { _occluded.Clear(); _visibility.Clear(); return; }

            var cam = SquadSource.GameCamera();
            var members = Squad.Roster.Members;
            if (cam == null || members.Count == 0) return;

            // One raycast per frame, round robin: cheap and still responsive for a squad.
            _occlusionCursor = (_occlusionCursor + 1) % members.Count;
            var target = members[_occlusionCursor];
            if (target.Status == MemberStatus.Alive) _occluded[target.Id] = IsOccluded(cam, Anchor(target));

            float step = Time.unscaledDeltaTime * FadeSpeed;
            foreach (var m in members)
            {
                _occluded.TryGetValue(m.Id, out var hidden);
                float goal = hidden ? Config.TagOccludedOpacity.Value : 1f;
                _visibility[m.Id] = _visibility.TryGetValue(m.Id, out var current)
                    ? NametagMath.MoveTowards(current, goal, step)
                    : goal;
            }
        }

        static bool IsOccluded(Camera cam, Vector3 anchor)
        {
            var from = cam.transform.position;
            var to = anchor - Vector3.up * 0.3f; // aim at the head, not the air above it
            var dir = to - from;
            float dist = dir.magnitude;
            if (dist < 0.5f) return false;
            return Physics.Raycast(from, dir / dist, out var hit, dist - 0.3f, OcclusionMask)
                && hit.collider.gameObject.layer != PlayerLayer;
        }

        /// <summary>World point the marker floats at: above the neck bone, or above the reported position.</summary>
        Vector3 Anchor(SquadMember m)
        {
            var player = Squad.PlayerFor(m);
            var neck = player != null && player.PlayerBones != null && player.PlayerBones.Neck != null
                ? player.PlayerBones.Neck.position
                : new Vector3(m.X, m.Y + AssumedNeckHeight, m.Z);
            return neck + Vector3.up * Config.TagHeight.Value;
        }

        void OnGUI()
        {
            if (Event.current.type != EventType.Repaint) return;
            if (!Config.TagsEnabled.Value || _hiddenByKey || !Squad.InRaid) return;
            if (!FikaCompat.HostAllowsNamePlates(Time.unscaledTime)) return;

            var cam = SquadSource.GameCamera();
            if (cam == null) return;

            // Aiming and magnified optics, as Fika's WorldToScreen handles them.
            var pwa = Squad.MainPlayer != null ? Squad.MainPlayer.ProceduralWeaponAnimation : null;
            bool aiming = pwa != null && pwa.IsAiming;
            Camera opticCam = null;
            if (aiming && IsZoomedOptic(pwa))
            {
                if (Config.TagOptics.Value == OpticMode.Hide) return;
                var manager = CameraManager.Instance;
                opticCam = manager != null && manager.OpticCameraManager != null ? manager.OpticCameraManager.Camera : null;
            }

            float opacity = Config.TagOpacity.Value * (aiming ? Config.TagAdsOpacity.Value : 1f);
            if (opacity <= 0.01f) return;

            float w = Screen.width, h = Screen.height;
            var centre = new Vec2(w / 2f, h / 2f);
            var saved = GUI.color;
            var camPos = cam.transform.position;
            var proj = opticCam != null ? opticCam : cam;

            // Draw far tags first so near ones sit on top.
            foreach (var m in Squad.Roster.Members
                         .Where(x => x.Status == MemberStatus.Alive)
                         .OrderByDescending(x => (Anchor(x) - camPos).sqrMagnitude))
            {
                var anchor = Anchor(m);
                float distance = Vector3.Distance(camPos, anchor);
                if (distance > Config.TagMaxDistance.Value) continue;

                var colour = Squad.ColourOf(m);
                float vis = _visibility.TryGetValue(m.Id, out var v) ? v : 1f;
                float alpha = opacity * vis * NearFade(distance);

                var vp = proj.WorldToViewportPoint(anchor);
                bool behind = vp.z <= 0f;
                Vec2 gui = opticCam != null
                    ? new Vec2(centre.X + (vp.x - 0.5f) * opticCam.pixelRect.max.x, centre.Y - (vp.y - 0.5f) * opticCam.pixelRect.max.y)
                    : new Vec2(vp.x * w, (1f - vp.y) * h);

                if (behind || !NametagMath.IsOnScreen(gui, w, h))
                {
                    if (Config.TagEdgeArrows.Value && opticCam == null)
                        DrawEdgeArrow(NametagMath.ClampToEdge(gui, behind, w, h, Config.TagEdgeMargin.Value), m, colour, opacity, distance);
                    continue;
                }
                if (alpha <= 0.01f) continue;

                float scale = NametagMath.ScaleForDistance(distance, NearScaleDistance, FarScaleDistance, Config.TagFarScale.Value);
                float focus = NametagMath.Focus(gui, centre, Config.TagFocusRadius.Value * h);
                DrawTag(new Vector2(gui.X, gui.Y), m, colour, alpha, scale, focus, distance);
            }
            GUI.color = saved;
        }

        float NearFade(float distance)
        {
            float d = Config.TagNearFade.Value;
            if (d <= 0f) return 1f;
            return Mathf.Clamp01((distance - d * 0.5f) / (d * 0.5f));
        }

        static bool IsZoomedOptic(EFT.Animations.ProceduralWeaponAnimation pwa)
        {
            if (pwa.CurrentScope == null || !pwa.CurrentScope.IsOptic) return false;
            var sight = pwa.CurrentAimingMod;
            if (sight == null) return false;
            float zoom = sight.ScopeZoomValue > 1f ? sight.ScopeZoomValue : sight.GetCurrentOpticZoom();
            return zoom > 1f;
        }

        void DrawTag(Vector2 pos, SquadMember m, Color colour, float alpha, float scale, float focus, float distance)
        {
            float marker = Config.TagMarkerSize.Value * scale;

            if (m.Downed)
            {
                // Revive marker: pulsing red cross in a ring, always with its label.
                var red = Downed.Pulse(alpha);
                DrawOutlined(Textures.Plus, pos, marker * 1.1f, red);
                GUI.color = red;
                GUI.DrawTexture(Square(pos, marker * 2f), Textures.Ring);
            }
            else
            {
                var markerColour = colour;
                markerColour.a = alpha;
                DrawOutlined(MarkerTexture(), pos, marker, markerColour);
            }

            var mode = Config.TagNames.Value;
            bool showName = m.Downed || mode == NameMode.Always || (mode == NameMode.WhenLooking && focus > 0.01f);
            if (!showName) return;

            float textAlpha = alpha * (m.Downed || mode == NameMode.Always ? 1f : focus);
            if (textAlpha <= 0.01f) return;

            int fontSize = Mathf.Max(8, Mathf.RoundToInt(Config.TagFontSize.Value * scale));
            var nameStyle = Style(_nameStyles, fontSize, FontStyle.Bold);
            var smallStyle = Style(_smallStyles, Mathf.Max(7, Mathf.RoundToInt(fontSize * 0.78f)), FontStyle.Normal);

            bool healthBar = Config.TagHealthBar.Value && m.Health >= 0f && !m.Downed;
            bool subLine = Config.TagDistance.Value || m.Downed;
            string name = Config.TagUppercase.Value ? (m.Name ?? "").ToUpperInvariant() : m.Name;
            float lineH = fontSize * 1.3f;
            float smallH = smallStyle.fontSize * 1.25f;
            float barStep = 5f * scale;
            const float boxW = 400f;

            // Stack upward from the marker: name, then health bar, then distance line.
            float y = pos.y - marker / 2f - 2f - lineH - (healthBar ? barStep : 0f) - (subLine ? smallH : 0f);

            var nameColour = colour;
            nameColour.a = textAlpha;
            Text(new Rect(pos.x - boxW / 2f, y, boxW, lineH), name, nameStyle, nameColour);
            y += lineH;

            if (healthBar)
            {
                float barW = Mathf.Max(30f, 46f * scale), barH = Mathf.Max(2f, 3f * scale);
                var bg = new Rect(pos.x - barW / 2f, y, barW, barH);
                GUI.color = new Color(0f, 0f, 0f, 0.55f * textAlpha);
                GUI.DrawTexture(new Rect(bg.x - 1, bg.y - 1, bg.width + 2, bg.height + 2), Textures.Pixel);
                var fill = Color.Lerp(new Color(0.95f, 0.25f, 0.2f), new Color(0.35f, 0.9f, 0.4f), m.Health);
                fill.a = textAlpha;
                GUI.color = fill;
                GUI.DrawTexture(new Rect(bg.x, bg.y, barW * m.Health, barH), Textures.Pixel);
                y += barStep;
            }

            if (m.Downed)
            {
                Text(new Rect(pos.x - boxW / 2f, y, boxW, smallH),
                    "DOWNED  " + HudMath.FormatDistance(distance), smallStyle, Downed.Pulse(textAlpha));
            }
            else if (Config.TagDistance.Value)
            {
                Text(new Rect(pos.x - boxW / 2f, y, boxW, smallH),
                    HudMath.FormatDistance(distance), smallStyle, new Color(1f, 1f, 1f, 0.85f * textAlpha));
            }
        }

        void DrawEdgeArrow(EdgeIndicator edge, SquadMember m, Color colour, float opacity, float distance)
        {
            float size = Config.TagMarkerSize.Value * 1.1f;
            var pos = new Vector2(edge.Position.X, edge.Position.Y);
            var tint = m.Downed ? Downed.Pulse(opacity) : colour;
            if (!m.Downed) tint.a = opacity;

            var matrix = GUI.matrix;
            GUIUtility.RotateAroundPivot(edge.Angle, pos);
            GUI.color = new Color(0f, 0f, 0f, 0.6f * tint.a);
            GUI.DrawTexture(Square(pos, size * 1.35f), Textures.TriangleUp);
            GUI.color = tint;
            GUI.DrawTexture(Square(pos, size), Textures.TriangleUp);
            GUI.matrix = matrix;

            // Distance on the inner side of the arrow.
            var small = Style(_smallStyles, Mathf.Max(7, Mathf.RoundToInt(Config.TagFontSize.Value * 0.75f)), FontStyle.Normal);
            float rad = edge.Angle * Mathf.Deg2Rad;
            var inward = new Vector2(-Mathf.Sin(rad), Mathf.Cos(rad)) * size * 1.3f;
            var labelPos = pos + inward;
            Text(new Rect(labelPos.x - 60f, labelPos.y - small.fontSize * 0.65f, 120f, small.fontSize * 1.3f),
                HudMath.FormatDistance(distance), small, new Color(tint.r, tint.g, tint.b, 0.9f * opacity));
        }

        Texture2D MarkerTexture()
        {
            switch (Config.TagMarker.Value)
            {
                case MarkerStyle.Diamond: return Textures.Diamond;
                case MarkerStyle.Dot: return Textures.Disc;
                default: return Textures.Chevron;
            }
        }

        /// <summary>Shape with a soft dark outline so it reads on bright skies and dark interiors alike.</summary>
        static void DrawOutlined(Texture2D tex, Vector2 pos, float size, Color colour)
        {
            GUI.color = new Color(0f, 0f, 0f, 0.65f * colour.a);
            GUI.DrawTexture(Square(pos, size * 1.4f), tex);
            GUI.color = colour;
            GUI.DrawTexture(Square(pos, size), tex);
        }

        /// <summary>Text with a four-way dark outline.</summary>
        static void Text(Rect rect, string text, GUIStyle style, Color colour)
        {
            GUI.color = new Color(0f, 0f, 0f, 0.75f * colour.a);
            GUI.Label(new Rect(rect.x - 1, rect.y, rect.width, rect.height), text, style);
            GUI.Label(new Rect(rect.x + 1, rect.y, rect.width, rect.height), text, style);
            GUI.Label(new Rect(rect.x, rect.y - 1, rect.width, rect.height), text, style);
            GUI.Label(new Rect(rect.x, rect.y + 1, rect.width, rect.height), text, style);
            GUI.color = colour;
            GUI.Label(rect, text, style);
        }

        GUIStyle Style(Dictionary<int, GUIStyle> cache, int size, FontStyle fontStyle)
        {
            if (cache.TryGetValue(size, out var style)) return style;
            style = new GUIStyle(GUI.skin.label)
            {
                fontSize = size,
                fontStyle = fontStyle,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = false,
                clipping = TextClipping.Overflow,
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0)
            };
            var font = GameFont();
            if (font != null) style.font = font;
            style.normal.textColor = Color.white;
            cache[size] = style;
            return style;
        }

        /// <summary>Tarkov's Bender font if a legacy copy is loaded, else Unity's default.</summary>
        Font GameFont()
        {
            if (_fontSearched) return _font;
            _fontSearched = true;
            _font = Resources.FindObjectsOfTypeAll<Font>()
                .FirstOrDefault(f => f != null && f.name.IndexOf("Bender", System.StringComparison.OrdinalIgnoreCase) >= 0);
            return _font;
        }

        static Rect Square(Vector2 centre, float size) => new Rect(centre.x - size / 2f, centre.y - size / 2f, size, size);
    }
}
