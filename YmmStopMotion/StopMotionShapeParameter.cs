using System.Collections.Immutable;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using YmmStopMotion.Core;
using YmmStopMotion.Views;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Controls;
using YukkuriMovieMaker.Exo;
using YukkuriMovieMaker.Player.Video;
using YukkuriMovieMaker.Plugin.Shape;
using YukkuriMovieMaker.Project;

namespace YmmStopMotion
{
    internal class StopMotionShapeParameter(SharedDataStore? sharedData) : ShapeParameterBase(sharedData)
    {
        public const double DefaultFps = 12;

        /// <summary>
        /// コマとして表示する画像ファイルのフルパス（表示順）
        /// </summary>
        [Display(GroupName = "ストップモーション", Name = "", Description = "コマとして表示する画像。上から順に表示されます")]
        [FramesEditor(PropertyEditorSize = PropertyEditorSize.FullWidth)]
        public ImmutableList<string> Frames { get => frames; set => Set(ref frames, value ?? []); }
        ImmutableList<string> frames = [];

        /// <summary>
        /// 1秒あたりに表示するコマ数
        /// </summary>
        [Display(GroupName = "ストップモーション", Name = "フレームレート", Description = "1秒あたりに表示するコマ数（コマ/秒）")]
        [TextBoxSlider("F1", "fps", 1, 30)]
        [DefaultValue(DefaultFps)]
        [Range(0.1, 240)]
        public double Fps { get => fps; set => Set(ref fps, value); }
        double fps = DefaultFps;

        /// <summary>
        /// 最後のコマまで再生したあとの動作
        /// </summary>
        [Display(GroupName = "ストップモーション", Name = "再生終了後", Description = "最後のコマまで再生したあとの動作")]
        [EnumComboBox]
        public StopMotionEndBehavior EndBehavior { get => endBehavior; set => Set(ref endBehavior, value); }
        StopMotionEndBehavior endBehavior = StopMotionEndBehavior.Loop;

        //必ず引数なしのコンストラクタを定義する（プロジェクトファイルの読み込みに必要）
        public StopMotionShapeParameter() : this(null)
        {
        }

        public override IEnumerable<string> CreateMaskExoFilter(int keyFrameIndex, ExoOutputDescription desc, ShapeMaskExoOutputDescription shapeMaskDesc) => [];

        public override IEnumerable<string> CreateShapeItemExoFilter(int keyFrameIndex, ExoOutputDescription desc) => [];

        public override IShapeSource CreateShapeSource(IGraphicsDevicesAndContext devices)
        {
            return new StopMotionShapeSource(devices, this);
        }

        protected override IEnumerable<IAnimatable> GetAnimatables() => [];

        protected override void LoadSharedData(SharedDataStore store)
        {
            var data = store.Load<SharedData>();
            if (data is null)
                return;
            data.CopyTo(this);
        }

        protected override void SaveSharedData(SharedDataStore store)
        {
            store.Save(new SharedData(this));
        }

        /// <summary>
        /// 図形の種類を切り替えたときに設定を復元するための一時保存用クラス
        /// </summary>
        public class SharedData
        {
            public ImmutableList<string> Frames { get; set; } = [];
            public double Fps { get; set; } = DefaultFps;
            public StopMotionEndBehavior EndBehavior { get; set; } = StopMotionEndBehavior.Loop;

            public SharedData()
            {
            }

            public SharedData(StopMotionShapeParameter parameter)
            {
                Frames = parameter.Frames;
                Fps = parameter.Fps;
                EndBehavior = parameter.EndBehavior;
            }

            public void CopyTo(StopMotionShapeParameter parameter)
            {
                parameter.Frames = Frames;
                parameter.Fps = Fps;
                parameter.EndBehavior = EndBehavior;
            }
        }
    }
}
