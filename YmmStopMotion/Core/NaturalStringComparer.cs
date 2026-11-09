namespace YmmStopMotion.Core
{
    /// <summary>
    /// 文字列中の数字を数値として比較する（"IMG_2" &lt; "IMG_10"）。
    /// カメラの連番ファイルを撮影順に並べるために使用する。
    /// </summary>
    public sealed class NaturalStringComparer : IComparer<string?>
    {
        public static NaturalStringComparer Instance { get; } = new();

        public int Compare(string? x, string? y)
        {
            if (ReferenceEquals(x, y))
                return 0;
            if (x is null)
                return -1;
            if (y is null)
                return 1;

            int ix = 0, iy = 0;
            while (ix < x.Length && iy < y.Length)
            {
                if (char.IsAsciiDigit(x[ix]) && char.IsAsciiDigit(y[iy]))
                {
                    var startX = ix;
                    var startY = iy;
                    while (ix < x.Length && char.IsAsciiDigit(x[ix])) ix++;
                    while (iy < y.Length && char.IsAsciiDigit(y[iy])) iy++;

                    var result = CompareDigits(x.AsSpan(startX, ix - startX), y.AsSpan(startY, iy - startY));
                    if (result != 0)
                        return result;
                }
                else
                {
                    var result = string.Compare(x, ix, y, iy, 1, StringComparison.OrdinalIgnoreCase);
                    if (result != 0)
                        return result;
                    ix++;
                    iy++;
                }
            }

            var lengthResult = (x.Length - ix).CompareTo(y.Length - iy);
            if (lengthResult != 0)
                return lengthResult;

            // 大文字小文字・ゼロ埋めの違いしかない場合も順序を安定させる
            return string.CompareOrdinal(x, y);
        }

        static int CompareDigits(ReadOnlySpan<char> a, ReadOnlySpan<char> b)
        {
            var trimmedA = a.TrimStart('0');
            var trimmedB = b.TrimStart('0');
            if (trimmedA.Length != trimmedB.Length)
                return trimmedA.Length.CompareTo(trimmedB.Length);
            var result = trimmedA.SequenceCompareTo(trimmedB);
            if (result != 0)
                return result;
            // 数値として等しい場合はゼロ埋めが短いほうを先にする（"1" < "01"）
            return a.Length.CompareTo(b.Length);
        }
    }
}
