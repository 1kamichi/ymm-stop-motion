using System.Windows;
using System.Windows.Controls;
using YukkuriMovieMaker.Commons;

namespace YmmStopMotion.Views
{
    /// <summary>
    /// コマ画像一覧の編集コントロール。
    /// IPropertyEditorControl を実装し、値の変更前後に BeginEdit / EndEdit を通知する（Undo/Redo 対応）。
    /// </summary>
    public partial class FramesEditor : UserControl, IPropertyEditorControl
    {
        public event EventHandler? BeginEdit;
        public event EventHandler? EndEdit;

        public FramesEditor()
        {
            InitializeComponent();
            DataContextChanged += FramesEditor_DataContextChanged;
        }

        private void FramesEditor_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is FramesEditorViewModel oldVm)
            {
                oldVm.BeginEdit -= ViewModel_BeginEdit;
                oldVm.EndEdit -= ViewModel_EndEdit;
            }
            if (e.NewValue is FramesEditorViewModel newVm)
            {
                newVm.BeginEdit += ViewModel_BeginEdit;
                newVm.EndEdit += ViewModel_EndEdit;
            }
        }

        private void ViewModel_BeginEdit(object? sender, EventArgs e) => BeginEdit?.Invoke(this, e);

        private void ViewModel_EndEdit(object? sender, EventArgs e) => EndEdit?.Invoke(this, e);

        private void Root_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
        }

        private void Root_Drop(object sender, DragEventArgs e)
        {
            if (DataContext is not FramesEditorViewModel vm)
                return;
            if (e.Data.GetData(DataFormats.FileDrop) is not string[] paths)
                return;

            vm.AddPaths(paths);
            e.Handled = true;
        }
    }
}
