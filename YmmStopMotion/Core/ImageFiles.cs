using System.IO;

namespace YmmStopMotion.Core
{
    /// <summary>
    /// 取り込み対象の画像ファイルの判定と列挙
    /// </summary>
    public static class ImageFiles
    {
        /// <summary>
        /// Windows Imaging Component (WIC) で標準的に読み込める拡張子
        /// </summary>
        public static IReadOnlyList<string> Extensions { get; } =
        [
            ".png", ".jpg", ".jpeg", ".jpe", ".jfif", ".bmp", ".gif", ".tif", ".tiff", ".webp", ".heic", ".heif",
        ];

        /// <summary>
        /// ファイルダイアログ用のフィルタ文字列
        /// </summary>
        public static string DialogFilter { get; } =
            $"画像ファイル|{string.Join(";", Extensions.Select(x => "*" + x))}|すべてのファイル|*.*";

        public static bool IsImageFile(string path)
        {
            var extension = Path.GetExtension(path);
            return Extensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// ファイルとフォルダのパス一覧を、取り込む画像ファイルの一覧に展開する。
        /// エクスプローラーからのドロップなどでは渡される順序が撮影順とは限らないため、
        /// フォルダ（名前順、中身も名前順）→ ファイル（名前順）の順に並べる。
        /// 名前順は "IMG_2" &lt; "IMG_10" となる自然順。
        /// </summary>
        public static IReadOnlyList<string> Expand(IEnumerable<string> paths)
        {
            var list = paths.ToList();
            var directories = list.Where(Directory.Exists).OrderBy(x => x, NaturalStringComparer.Instance);
            var files = list.Where(x => File.Exists(x) && IsImageFile(x)).Select(Path.GetFullPath);

            return
            [
                .. directories.SelectMany(EnumerateDirectory),
                .. SortByFileName(files),
            ];
        }

        /// <summary>
        /// フォルダ直下の画像ファイルを名前順（自然順）で列挙する
        /// </summary>
        public static IReadOnlyList<string> EnumerateDirectory(string directory)
        {
            try
            {
                return
                [
                    .. Directory.EnumerateFiles(directory)
                        .Where(IsImageFile)
                        .Select(Path.GetFullPath)
                        .OrderBy(Path.GetFileName, NaturalStringComparer.Instance)
                ];
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                return [];
            }
        }

        /// <summary>
        /// ファイル名で自然順に並べ替える
        /// </summary>
        public static IReadOnlyList<string> SortByFileName(IEnumerable<string> paths) =>
            [.. paths.OrderBy(Path.GetFileName, NaturalStringComparer.Instance).ThenBy(x => x, NaturalStringComparer.Instance)];
    }
}
