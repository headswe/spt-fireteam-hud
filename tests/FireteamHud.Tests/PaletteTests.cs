using System.Linq;
using FireteamHud.Core;
using Xunit;

namespace FireteamHud.Tests
{
    public class PaletteTests
    {
        const int A = Palette.Auto, C = Palette.Custom;

        [Fact]
        public void AutoFillsInJoinOrder()
        {
            Assert.Equal(new[] { 0, 1, 2, 3 }, Palette.Assign(new[] { A, A, A, A }));
        }

        [Fact]
        public void AutoSkipsColoursPickedByHand()
        {
            // Second player picked Red (0); the autos must not also get red.
            Assert.Equal(new[] { 1, 0, 2 }, Palette.Assign(new[] { A, 0, A }));
        }

        [Fact]
        public void CustomStaysCustomAndTakesNoSlot()
        {
            Assert.Equal(new[] { C, 0, 1 }, Palette.Assign(new[] { C, A, A }));
        }

        [Fact]
        public void TwoHandPicksMayShare()
        {
            Assert.Equal(new[] { 4, 4, 0 }, Palette.Assign(new[] { 4, 4, A }));
        }

        [Fact]
        public void FallsBackToJoinOrderWhenEverythingIsTaken()
        {
            var picks = Enumerable.Range(0, Palette.Count).Concat(new[] { A }).ToArray();
            var slots = Palette.Assign(picks);
            Assert.Equal(0, slots[Palette.Count]); // the 21st player wraps to the first colour
        }

        [Fact]
        public void AllPaletteColoursAreDistinct()
        {
            for (int i = 0; i < Palette.Count; i++)
                Assert.Equal(i, Palette.IndexOf(Palette.Colours[i]));
        }

        [Fact]
        public void IndexOfFindsOldDefaultsWithinRounding()
        {
            // Old config files store colours as 8-bit hex, so values come back rounded.
            var orange = new Rgba(255 / 255f, 140 / 255f, 38 / 255f);
            Assert.Equal(4, Palette.IndexOf(orange));
            Assert.Equal(-1, Palette.IndexOf(new Rgba(0.1f, 0.2f, 0.3f)));
        }
    }
}
