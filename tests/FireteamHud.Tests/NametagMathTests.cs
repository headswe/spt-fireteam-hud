using FireteamHud.Core;
using Xunit;

namespace FireteamHud.Tests
{
    public class NametagMathTests
    {
        [Theory]
        [InlineData(0, 1f)]
        [InlineData(5, 1f)]
        [InlineData(55, 0.75f)]
        [InlineData(105, 0.5f)]
        [InlineData(500, 0.5f)]
        public void ScaleShrinksThenHolds(float distance, float expected) =>
            Assert.Equal(expected, NametagMath.ScaleForDistance(distance, 5, 105, 0.5f), 3);

        [Theory]
        [InlineData(960, 540, 1f)]   // on the crosshair
        [InlineData(1060, 540, 1f)]  // inside the radius
        [InlineData(1110, 540, 0.5f)]
        [InlineData(1500, 540, 0f)]
        public void FocusFadesOutsideRadius(float x, float y, float expected) =>
            Assert.Equal(expected, NametagMath.Focus(new Vec2(x, y), new Vec2(960, 540), 100), 3);

        [Fact]
        public void ClampToEdge_RightOfScreen()
        {
            var e = NametagMath.ClampToEdge(new Vec2(3000, 540), false, 1920, 1080, 40);
            Assert.Equal(1880, e.Position.X, 2);
            Assert.Equal(540, e.Position.Y, 2);
            Assert.Equal(90, e.Angle, 2);
        }

        [Fact]
        public void ClampToEdge_AboveScreenPointsUp()
        {
            var e = NametagMath.ClampToEdge(new Vec2(960, -500), false, 1920, 1080, 40);
            Assert.Equal(40, e.Position.Y, 2);
            Assert.Equal(0, e.Angle, 2);
        }

        [Fact]
        public void ClampToEdge_BehindIsMirroredAndPushedToEdge()
        {
            // A mate behind and to the left projects (mirrored) to the right of centre.
            var e = NametagMath.ClampToEdge(new Vec2(1100, 540), true, 1920, 1080, 40);
            Assert.Equal(40, e.Position.X, 2);
            Assert.Equal(-90, e.Angle, 2);

            var straightBehind = NametagMath.ClampToEdge(new Vec2(960, 540), true, 1920, 1080, 40);
            Assert.Equal(1040, straightBehind.Position.Y, 2);
            Assert.Equal(180, System.Math.Abs(straightBehind.Angle), 2);
        }

        [Fact]
        public void MoveTowardsStepsAndStops()
        {
            Assert.Equal(0.5f, NametagMath.MoveTowards(0, 1, 0.5f));
            Assert.Equal(1f, NametagMath.MoveTowards(0.9f, 1, 0.5f));
            Assert.Equal(0.4f, NametagMath.MoveTowards(1, 0.4f, 0.75f), 3);
        }

        [Fact]
        public void RosterCarriesDownedAndHealth()
        {
            var roster = new SquadRoster();
            roster.Update(new[] { new MemberSighting("a", "Alpha", 0, 0, 0, true) { Downed = true, Health = 0.3f } });
            Assert.True(roster.Members[0].Downed);
            Assert.Equal(0.3f, roster.Members[0].Health);

            roster.Update(new MemberSighting[0]);
            Assert.False(roster.Members[0].Downed);
        }
    }
}
