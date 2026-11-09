using System.ComponentModel.DataAnnotations;

namespace YmmStopMotion.Core
{
    /// <summary>
    /// 最後のコマまで再生し終えたあとの動作
    /// </summary>
    public enum StopMotionEndBehavior
    {
        [Display(Name = "ループ", Description = "最初のコマに戻って繰り返し再生します")]
        Loop,

        [Display(Name = "最後のコマで停止", Description = "最後のコマを表示したまま止まります")]
        Hold,

        [Display(Name = "往復", Description = "最後のコマまで再生したら逆順に戻ります")]
        PingPong,
    }
}
