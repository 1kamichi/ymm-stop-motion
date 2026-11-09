using YmmStopMotion.Core;

namespace YmmStopMotion.Tests
{
    public class FrameIndexCalculatorTests
    {
        [Theory]
        // 60fps のプロジェクトで 12fps → 5 フレームごとにコマが進む
        [InlineData(0, 0)]
        [InlineData(4, 0)]
        [InlineData(5, 1)]
        [InlineData(9, 1)]
        [InlineData(10, 2)]
        public void Advances_one_frame_every_projectFps_over_animationFps(long itemFrame, int expected)
        {
            Assert.Equal(expected, FrameIndexCalculator.GetFrameIndex(itemFrame, 60, 12, 100, StopMotionEndBehavior.Loop));
        }

        [Fact]
        public void Non_integer_ratio_does_not_drift()
        {
            // 30fps のプロジェクトで 8fps：3.75 フレームごと
            var indices = Enumerable.Range(0, 31).Select(f => FrameIndexCalculator.GetFrameIndex(f, 30, 8, 1000, StopMotionEndBehavior.Loop)).ToArray();
            Assert.Equal(0, indices[0]);
            Assert.Equal(0, indices[3]);
            Assert.Equal(1, indices[4]);
            Assert.Equal(2, indices[8]);   // 7.5 → 8 で 2コマ目
            Assert.Equal(4, indices[15]);  // 15 フレーム = 0.5 秒 = 4 コマ
            Assert.Equal(8, indices[30]);  // 1 秒 = 8 コマ
        }

        [Fact]
        public void Exact_boundary_is_not_lost_to_floating_point_error()
        {
            // 0.1 * 30 / 3 などは誤差で 0.999... になりやすい
            Assert.Equal(1, FrameIndexCalculator.GetFrameIndex(3, 30, 10, 100, StopMotionEndBehavior.Hold));
            Assert.Equal(7, FrameIndexCalculator.GetFrameIndex(70, 30, 3, 100, StopMotionEndBehavior.Hold));
        }

        [Theory]
        [InlineData(StopMotionEndBehavior.Loop, new[] { 0, 1, 2, 0, 1, 2, 0 })]
        [InlineData(StopMotionEndBehavior.Hold, new[] { 0, 1, 2, 2, 2, 2, 2 })]
        [InlineData(StopMotionEndBehavior.PingPong, new[] { 0, 1, 2, 1, 0, 1, 2 })]
        public void End_behavior(StopMotionEndBehavior behavior, int[] expected)
        {
            // プロジェクト fps とコマの fps が同じなら 1 フレーム = 1 コマ
            var actual = Enumerable.Range(0, expected.Length).Select(f => FrameIndexCalculator.GetFrameIndex(f, 30, 30, 3, behavior)).ToArray();
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(StopMotionEndBehavior.Loop)]
        [InlineData(StopMotionEndBehavior.Hold)]
        [InlineData(StopMotionEndBehavior.PingPong)]
        public void Single_frame_always_shows_first(StopMotionEndBehavior behavior)
        {
            Assert.Equal(0, FrameIndexCalculator.GetFrameIndex(12345, 30, 12, 1, behavior));
        }

        [Fact]
        public void No_frames_returns_minus_one()
        {
            Assert.Equal(-1, FrameIndexCalculator.GetFrameIndex(10, 30, 12, 0, StopMotionEndBehavior.Loop));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(double.NaN)]
        [InlineData(double.PositiveInfinity)]
        public void Invalid_fps_shows_first_frame(double fps)
        {
            Assert.Equal(0, FrameIndexCalculator.GetFrameIndex(100, 30, fps, 10, StopMotionEndBehavior.Loop));
        }

        [Fact]
        public void Very_long_timeline_does_not_overflow()
        {
            var index = FrameIndexCalculator.GetFrameIndex(long.MaxValue / 2, 60, 240, 7, StopMotionEndBehavior.Loop);
            Assert.InRange(index, 0, 6);
        }

        [Fact]
        public void Duration()
        {
            Assert.Equal(TimeSpan.FromSeconds(2), FrameIndexCalculator.GetDuration(24, 12));
            Assert.Equal(TimeSpan.Zero, FrameIndexCalculator.GetDuration(0, 12));
            Assert.Equal(TimeSpan.Zero, FrameIndexCalculator.GetDuration(10, 0));
        }
    }
}
