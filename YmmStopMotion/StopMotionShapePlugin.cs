using YukkuriMovieMaker.Plugin.Shape;
using YukkuriMovieMaker.Project;

namespace YmmStopMotion
{
    /// <summary>
    /// 図形アイテムの「図形の種類」に「ストップモーション」を追加するプラグイン。
    /// 撮影した画像を1コマずつ並べ、指定したフレームレートで切り替えて表示する。
    /// </summary>
    public class StopMotionShapePlugin : IShapePlugin
    {
        public string Name => "ストップモーション";

        // exo（AviUtl）出力には対応しない
        public bool IsExoShapeSupported => false;
        public bool IsExoMaskSupported => false;

        public IShapeParameter CreateShapeParameter(SharedDataStore? sharedData)
        {
            return new StopMotionShapeParameter(sharedData);
        }
    }
}
