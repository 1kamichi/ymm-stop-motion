using System.Windows;
using YukkuriMovieMaker.Commons;

namespace YmmStopMotion.Views
{
    /// <summary>
    /// コマ画像の一覧（ImmutableList&lt;string&gt;）を編集するコントロールを表示する属性
    /// </summary>
    internal class FramesEditorAttribute : PropertyEditorAttribute2
    {
        public override FrameworkElement Create()
        {
            return new FramesEditor();
        }

        public override void SetBindings(FrameworkElement control, ItemProperty[] itemProperties)
        {
            if (control is not FramesEditor editor)
                return;
            editor.DataContext = new FramesEditorViewModel(itemProperties);
        }

        public override void ClearBindings(FrameworkElement control)
        {
            if (control is not FramesEditor editor)
                return;
            var vm = editor.DataContext as FramesEditorViewModel;
            vm?.Dispose();
            editor.DataContext = null;
        }
    }
}
