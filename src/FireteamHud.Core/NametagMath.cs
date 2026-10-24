using System;

namespace FireteamHud.Core
{
    public struct EdgeIndicator
    {
        /// <summary>Where to draw the arrow, in GUI pixels (origin top-left, Y down).</summary>
        public Vec2 Position;
        /// <summary>Arrow rotation in degrees: 0 points up, positive turns clockwise.</summary>
        public float Angle;
    }

    public static class NametagMath
    {
        /// <summary>
        /// Text scale for a tag at a distance: full size up close, shrinking linearly to
        /// <paramref name="farScale"/> at <paramref name="far"/> metres and staying there,
        /// so distant names stay readable instead of shrinking to nothing.
        /// </summary>
        public static float ScaleForDistance(float distance, float near, float far, float farScale)
        {
            if (far <= near) return 1f;
            float t = Clamp01((distance - near) / (far - near));
            return 1f + (farScale - 1f) * t;
        }

        /// <summary>
        /// 1 when the tag is within <paramref name="radius"/> pixels of the crosshair, fading
        /// to 0 at twice that. Used to show full detail only for the mate you look at.
        /// </summary>
        public static float Focus(Vec2 position, Vec2 centre, float radius)
        {
            if (radius <= 0) return 1f;
            float dx = position.X - centre.X, dy = position.Y - centre.Y;
            float d = (float)Math.Sqrt(dx * dx + dy * dy);
            return 1f - Clamp01((d - radius) / radius);
        }

        /// <summary>
        /// Pins an off-screen target to the screen edge and points an arrow at it.
        /// </summary>
        /// <param name="position">Projected GUI position; may lie outside the screen.</param>
        /// <param name="behind">True when the target is behind the camera. Projection mirrors
        /// such points through the centre, so they are flipped back first.</param>
        public static EdgeIndicator ClampToEdge(Vec2 position, bool behind, float width, float height, float margin)
        {
            float cx = width / 2f, cy = height / 2f;
            float dx = position.X - cx, dy = position.Y - cy;
            if (behind)
            {
                dx = -dx;
                dy = -dy;
                // Straight behind: the direction is meaningless, point down ("turn around").
                if (Math.Abs(dx) < 1f && Math.Abs(dy) < 1f) dy = 1f;
            }

            float halfW = Math.Max(1f, cx - margin), halfH = Math.Max(1f, cy - margin);
            float sx = dx == 0 ? float.MaxValue : halfW / Math.Abs(dx);
            float sy = dy == 0 ? float.MaxValue : halfH / Math.Abs(dy);
            float s = Math.Min(sx, sy);
            // Targets behind us always go to the edge, even if the mirrored point is on screen.
            if (!behind && s > 1f) s = 1f;

            return new EdgeIndicator
            {
                Position = new Vec2(cx + dx * s, cy + dy * s),
                Angle = (float)(Math.Atan2(dx, -dy) * 180.0 / Math.PI)
            };
        }

        public static bool IsOnScreen(Vec2 position, float width, float height) =>
            position.X >= 0 && position.X <= width && position.Y >= 0 && position.Y <= height;

        /// <summary>Moves <paramref name="current"/> toward <paramref name="target"/> by at most <paramref name="maxDelta"/>.</summary>
        public static float MoveTowards(float current, float target, float maxDelta)
        {
            if (Math.Abs(target - current) <= maxDelta) return target;
            return current + Math.Sign(target - current) * maxDelta;
        }

        static float Clamp01(float v) => v < 0 ? 0 : v > 1 ? 1 : v;
    }
}
