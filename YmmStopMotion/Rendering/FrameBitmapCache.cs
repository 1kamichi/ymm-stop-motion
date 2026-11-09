using System.IO;
using Vortice.Direct2D1;
using Vortice.WIC;
using YukkuriMovieMaker.Commons;
using D2DAlphaMode = Vortice.DCommon.AlphaMode;
using D2DPixelFormat = Vortice.DCommon.PixelFormat;
using DxgiFormat = Vortice.DXGI.Format;
using WicPixelFormat = Vortice.WIC.PixelFormat;

namespace YmmStopMotion.Rendering
{
    /// <summary>
    /// コマ画像を読み込んで ID2D1Bitmap として保持する LRU キャッシュ。
    /// 再生のたびにデコードし直さないよう、使用メモリの上限まで保持する。
    /// </summary>
    internal sealed class FrameBitmapCache : IDisposable
    {
        /// <summary>
        /// キャッシュするビットマップの合計サイズの上限（バイト）
        /// </summary>
        const long DefaultBudgetBytes = 512L * 1024 * 1024;

        /// <summary>
        /// 読み込みに失敗したファイルを記録しておく最大数
        /// </summary>
        const int MaxFailedEntries = 1024;

        readonly IGraphicsDevicesAndContext devices;
        readonly long budgetBytes;
        readonly IWICImagingFactory wicFactory;

        readonly Dictionary<string, LinkedListNode<Entry>> entries = new(StringComparer.OrdinalIgnoreCase);
        readonly LinkedList<Entry> lru = new();
        readonly Dictionary<string, FileStamp> failed = new(StringComparer.OrdinalIgnoreCase);
        long totalBytes;
        bool disposed;

        public FrameBitmapCache(IGraphicsDevicesAndContext devices, long budgetBytes = DefaultBudgetBytes)
        {
            this.devices = devices;
            this.budgetBytes = budgetBytes;
            wicFactory = new IWICImagingFactory();
        }

        /// <summary>
        /// 画像を取得する。読み込めない場合は null。
        /// ファイルが更新されていた場合（撮り直しで上書きした場合など）は読み込み直す。
        /// </summary>
        public ID2D1Bitmap? Get(string path)
        {
            ObjectDisposedException.ThrowIf(disposed, this);

            if (string.IsNullOrEmpty(path))
                return null;

            var stamp = FileStamp.Read(path);
            if (stamp is null)
                return null;

            if (entries.TryGetValue(path, out var node))
            {
                if (node.Value.Stamp == stamp.Value)
                {
                    lru.Remove(node);
                    lru.AddFirst(node);
                    return node.Value.Bitmap;
                }
                Remove(node);
            }

            if (failed.TryGetValue(path, out var failedStamp) && failedStamp == stamp.Value)
                return null;

            var bitmap = Load(path);
            if (bitmap is null)
            {
                if (failed.Count >= MaxFailedEntries)
                    failed.Clear();
                failed[path] = stamp.Value;
                return null;
            }
            failed.Remove(path);

            var pixelSize = bitmap.PixelSize;
            var bytes = (long)pixelSize.Width * pixelSize.Height * 4;
            var newNode = lru.AddFirst(new Entry(path, stamp.Value, bitmap, bytes));
            entries[path] = newNode;
            totalBytes += bytes;

            Trim();
            return bitmap;
        }

        ID2D1Bitmap? Load(string path)
        {
            try
            {
                // 撮影ソフトが書き込み中でも読めるよう、共有モードを緩めて開く
                using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                using var wicStream = wicFactory.CreateStream(stream);
                using var decoder = wicFactory.CreateDecoderFromStream(wicStream, DecodeOptions.CacheOnLoad);
                using var frame = decoder.GetFrame(0);
                using var converter = wicFactory.CreateFormatConverter();
                converter.Initialize(frame, WicPixelFormat.Format32bppPBGRA, BitmapDitherType.None, null, 0, BitmapPaletteType.Custom);

                // 画像ファイルの DPI 設定に左右されず、1px = 1px で表示されるよう 96dpi で作成する
                var properties = new BitmapProperties1(
                    new D2DPixelFormat(DxgiFormat.B8G8R8A8_UNorm, D2DAlphaMode.Premultiplied),
                    96f,
                    96f,
                    BitmapOptions.None);
                return devices.DeviceContext.CreateBitmapFromWicBitmap(converter, properties);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 上限を超えた分を古いものから破棄する。直近に使ったもの1枚は必ず残す。
        /// </summary>
        void Trim()
        {
            while (totalBytes > budgetBytes && lru.Count > 1 && lru.Last is { } last)
                Remove(last);
        }

        void Remove(LinkedListNode<Entry> node)
        {
            lru.Remove(node);
            entries.Remove(node.Value.Path);
            totalBytes -= node.Value.Bytes;
            node.Value.Bitmap.Dispose();
        }

        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;

            foreach (var entry in lru)
                entry.Bitmap.Dispose();
            lru.Clear();
            entries.Clear();
            failed.Clear();
            totalBytes = 0;
            wicFactory.Dispose();
        }

        sealed record Entry(string Path, FileStamp Stamp, ID2D1Bitmap Bitmap, long Bytes);

        readonly record struct FileStamp(DateTime LastWriteTimeUtc, long Length)
        {
            public static FileStamp? Read(string path)
            {
                try
                {
                    var info = new FileInfo(path);
                    if (!info.Exists)
                        return null;
                    return new FileStamp(info.LastWriteTimeUtc, info.Length);
                }
                catch
                {
                    return null;
                }
            }
        }
    }
}
