using System.Numerics;
using Vortice.Direct2D1;
using YmmStopMotion.Core;
using YmmStopMotion.Rendering;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Player.Video;
using YukkuriMovieMaker.Plugin.Shape;

namespace YmmStopMotion
{
    /// <summary>
    /// 現在の再生位置に対応するコマ画像を描画する
    /// </summary>
    internal sealed class StopMotionShapeSource : IShapeSource
    {
        readonly IGraphicsDevicesAndContext devices;
        readonly StopMotionShapeParameter parameter;
        readonly FrameBitmapCache cache;

        ID2D1CommandList? commandList;

        // 前回描画した内容。変化がなければ CommandList を作り直さない
        ID2D1Bitmap? drawnBitmap;
        bool hasDrawn;

        bool disposed;

        public ID2D1Image Output => commandList ?? throw new InvalidOperationException($"{nameof(commandList)}がnullです。事前にUpdateを呼び出す必要があります。");

        public StopMotionShapeSource(IGraphicsDevicesAndContext devices, StopMotionShapeParameter parameter)
        {
            this.devices = devices;
            this.parameter = parameter;
            cache = new FrameBitmapCache(devices);
        }

        public void Update(TimelineItemSourceDescription timelineItemSourceDescription)
        {
            var frames = parameter.Frames;
            var index = FrameIndexCalculator.GetFrameIndex(
                timelineItemSourceDescription.ItemPosition.Frame,
                timelineItemSourceDescription.FPS,
                parameter.Fps,
                frames.Count,
                parameter.EndBehavior);

            var bitmap = index >= 0 ? cache.Get(frames[index]) : null;

            if (hasDrawn && ReferenceEquals(bitmap, drawnBitmap))
                return;

            var dc = devices.DeviceContext;

            //新規作成前に、前回のCommandListを必ず破棄する
            commandList?.Dispose();
            commandList = dc.CreateCommandList();

            dc.Target = commandList;
            dc.BeginDraw();
            dc.Clear(null);
            if (bitmap is not null)
            {
                // 図形は原点が中心になるように描画する
                var size = bitmap.Size;
                dc.DrawImage(bitmap, new Vector2(-size.Width / 2f, -size.Height / 2f));
            }
            dc.EndDraw();
            dc.Target = null;//Targetは必ずnullに戻す
            commandList.Close();//CommandListはEndDraw()の後に必ずClose()を呼ぶ

            drawnBitmap = bitmap;
            hasDrawn = true;
        }

        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;

            commandList?.Dispose();
            commandList = null;
            drawnBitmap = null;
            cache.Dispose();
        }
    }
}
