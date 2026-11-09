namespace YmmStopMotion.Core
{
    /// <summary>
    /// タイムライン上の位置から、表示すべきコマ番号を求める
    /// </summary>
    public static class FrameIndexCalculator
    {
        /// <summary>
        /// 浮動小数点誤差でコマの切り替わりが1フレーム遅れるのを防ぐための補正値
        /// </summary>
        const double Epsilon = 1e-9;

        /// <summary>
        /// 表示するコマ番号を返す
        /// </summary>
        /// <param name="itemFrame">アイテム先頭からのフレーム位置（プロジェクトのフレームレート基準）</param>
        /// <param name="projectFps">プロジェクトのフレームレート</param>
        /// <param name="animationFps">ストップモーションのフレームレート（1秒あたりのコマ数）</param>
        /// <param name="frameCount">コマ（画像）の枚数</param>
        /// <param name="endBehavior">最後のコマに達したあとの動作</param>
        /// <returns>コマ番号。表示できるコマがない場合は -1</returns>
        public static int GetFrameIndex(long itemFrame, int projectFps, double animationFps, int frameCount, StopMotionEndBehavior endBehavior)
        {
            if (frameCount <= 0)
                return -1;
            if (frameCount == 1 || itemFrame <= 0 || projectFps <= 0 || !(animationFps > 0) || double.IsInfinity(animationFps))
                return 0;

            var elapsed = Math.Floor(itemFrame * animationFps / projectFps + Epsilon);
            var step = elapsed >= long.MaxValue ? long.MaxValue : (long)elapsed;

            switch (endBehavior)
            {
                case StopMotionEndBehavior.Hold:
                    return (int)Math.Min(step, frameCount - 1);

                case StopMotionEndBehavior.PingPong:
                    {
                        // 0,1,...,n-1,n-2,...,1 を繰り返す
                        long period = 2L * frameCount - 2;
                        var position = step % period;
                        return (int)(position < frameCount ? position : period - position);
                    }

                case StopMotionEndBehavior.Loop:
                default:
                    return (int)(step % frameCount);
            }
        }

        /// <summary>
        /// 全コマを1回再生するのにかかる時間を返す
        /// </summary>
        public static TimeSpan GetDuration(int frameCount, double animationFps)
        {
            if (frameCount <= 0 || !(animationFps > 0) || double.IsInfinity(animationFps))
                return TimeSpan.Zero;
            return TimeSpan.FromSeconds(frameCount / animationFps);
        }
    }
}
