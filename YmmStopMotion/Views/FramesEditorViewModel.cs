using System.Collections.Immutable;
using System.ComponentModel;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using YmmStopMotion.Core;
using YukkuriMovieMaker.Commons;

namespace YmmStopMotion.Views
{
    /// <summary>
    /// コマ画像一覧の1行分
    /// </summary>
    internal sealed record FrameListItem(int Number, string Path)
    {
        public string FileName { get; } = System.IO.Path.GetFileName(Path);
        public bool Exists { get; } = File.Exists(Path);
    }

    internal sealed class FramesEditorViewModel : Bindable, IDisposable
    {
        const int PreviewDecodeWidth = 240;

        // ダイアログで最後に開いたフォルダ（YMM4 の起動中は保持する）
        static string? lastDirectory;

        readonly INotifyPropertyChanged item;
        readonly ItemProperty[] properties;

        public event EventHandler? BeginEdit;
        public event EventHandler? EndEdit;

        public IReadOnlyList<FrameListItem> Items { get => items; private set => Set(ref items, value); }
        IReadOnlyList<FrameListItem> items = [];

        public int SelectedIndex
        {
            get => selectedIndex;
            set
            {
                if (selectedIndex == value)
                    return;
                Set(ref selectedIndex, value);
                UpdatePreview();
                RaiseCanExecuteChanged();
            }
        }
        int selectedIndex = -1;

        public ImageSource? Preview { get => preview; private set => Set(ref preview, value); }
        ImageSource? preview;

        public string Summary { get => summary; private set => Set(ref summary, value); }
        string summary = "";

        public ActionCommand AddFilesCommand { get; }
        public ActionCommand AddFolderCommand { get; }
        public ActionCommand RemoveCommand { get; }
        public ActionCommand MoveUpCommand { get; }
        public ActionCommand MoveDownCommand { get; }
        public ActionCommand SortCommand { get; }
        public ActionCommand ClearCommand { get; }

        IEnumerable<ActionCommand> Commands => [AddFilesCommand, AddFolderCommand, RemoveCommand, MoveUpCommand, MoveDownCommand, SortCommand, ClearCommand];

        public FramesEditorViewModel(ItemProperty[] properties)
        {
            this.properties = properties;

            item = (INotifyPropertyChanged)properties[0].PropertyOwner;
            item.PropertyChanged += Item_PropertyChanged;

            AddFilesCommand = new ActionCommand(_ => true, _ => AddFiles());
            AddFolderCommand = new ActionCommand(_ => true, _ => AddFolder());
            RemoveCommand = new ActionCommand(
                _ => IsValidIndex(SelectedIndex),
                _ =>
                {
                    var index = SelectedIndex;
                    var paths = CurrentPaths().RemoveAt(index);
                    Commit(paths, Math.Min(index, paths.Count - 1));
                });
            MoveUpCommand = new ActionCommand(
                _ => IsValidIndex(SelectedIndex) && SelectedIndex > 0,
                _ => Move(SelectedIndex, SelectedIndex - 1));
            MoveDownCommand = new ActionCommand(
                _ => IsValidIndex(SelectedIndex) && SelectedIndex < Items.Count - 1,
                _ => Move(SelectedIndex, SelectedIndex + 1));
            SortCommand = new ActionCommand(
                _ => Items.Count > 1,
                _ => Commit([.. ImageFiles.SortByFileName(CurrentPaths())], 0));
            ClearCommand = new ActionCommand(
                _ => Items.Count > 0,
                _ => Commit([], -1));

            UpdateItems();
        }

        /// <summary>
        /// ファイル・フォルダのパスを選択位置の後ろに追加する（ドラッグ＆ドロップ用）
        /// </summary>
        public void AddPaths(IEnumerable<string> paths)
        {
            var files = ImageFiles.Expand(paths);
            if (files.Count == 0)
                return;

            var current = CurrentPaths();
            var insertAt = IsValidIndex(SelectedIndex) ? SelectedIndex + 1 : current.Count;
            Commit(current.InsertRange(insertAt, files), insertAt);
        }

        void AddFiles()
        {
            var dialog = new OpenFileDialog
            {
                Title = "コマ画像を選択",
                Filter = ImageFiles.DialogFilter,
                Multiselect = true,
            };
            if (GetInitialDirectory() is { } dir)
                dialog.InitialDirectory = dir;
            if (dialog.ShowDialog() != true)
                return;

            lastDirectory = Path.GetDirectoryName(dialog.FileNames.FirstOrDefault());
            AddPaths(dialog.FileNames);
        }

        void AddFolder()
        {
            var dialog = new OpenFolderDialog
            {
                Title = "コマ画像が入ったフォルダを選択（フォルダ内の画像を名前順に追加します）",
                Multiselect = false,
            };
            if (GetInitialDirectory() is { } dir)
                dialog.InitialDirectory = dir;
            if (dialog.ShowDialog() != true)
                return;

            lastDirectory = dialog.FolderName;
            AddPaths([dialog.FolderName]);
        }

        string? GetInitialDirectory()
        {
            var selected = IsValidIndex(SelectedIndex) ? Items[SelectedIndex].Path : Items.LastOrDefault()?.Path;
            var dir = selected is not null ? Path.GetDirectoryName(selected) : lastDirectory;
            return dir is not null && Directory.Exists(dir) ? dir : null;
        }

        void Move(int from, int to)
        {
            var paths = CurrentPaths();
            var path = paths[from];
            Commit(paths.RemoveAt(from).Insert(to, path), to);
        }

        /// <summary>
        /// 選択中のすべてのアイテムに値を設定する（Undo/Redo のため BeginEdit/EndEdit で囲む）
        /// </summary>
        void Commit(ImmutableList<string> paths, int newSelectedIndex)
        {
            BeginEdit?.Invoke(this, EventArgs.Empty);
            foreach (var property in properties)
                property.SetValue(paths);
            EndEdit?.Invoke(this, EventArgs.Empty);

            UpdateItems();
            SelectedIndex = newSelectedIndex;
        }

        ImmutableList<string> CurrentPaths() => properties[0].GetValue<ImmutableList<string>>() ?? [];

        bool IsValidIndex(int index) => index >= 0 && index < Items.Count;

        private void Item_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == properties[0].PropertyInfo.Name
                || e.PropertyName == nameof(StopMotionShapeParameter.Fps))
            {
                UpdateItems();
            }
        }

        void UpdateItems()
        {
            var paths = CurrentPaths();
            if (!Items.Select(x => x.Path).SequenceEqual(paths))
            {
                var selected = SelectedIndex;
                Items = [.. paths.Select((path, i) => new FrameListItem(i + 1, path))];

                // Undo などで一覧が変わったときに選択位置が範囲外にならないようにする
                var newIndex = Items.Count == 0 ? -1 : Math.Clamp(selected, 0, Items.Count - 1);
                if (newIndex == SelectedIndex)
                    UpdatePreview();
                else
                    SelectedIndex = newIndex;
            }
            UpdateSummary();
            RaiseCanExecuteChanged();
        }

        void UpdateSummary()
        {
            var count = Items.Count;
            if (count == 0)
            {
                Summary = "画像が追加されていません。下のボタンかドラッグ＆ドロップで追加してください。";
                return;
            }

            var missing = Items.Count(x => !x.Exists);
            var text = $"{count} コマ";
            if (item is StopMotionShapeParameter parameter)
            {
                var duration = FrameIndexCalculator.GetDuration(count, parameter.Fps);
                text += $"　{parameter.Fps:0.##} fps で {duration.TotalSeconds:0.##} 秒";
            }
            if (missing > 0)
                text += $"　（見つからない画像: {missing} 件）";
            Summary = text;
        }

        void UpdatePreview()
        {
            Preview = IsValidIndex(SelectedIndex) ? LoadPreview(Items[SelectedIndex].Path) : null;
        }

        static BitmapImage? LoadPreview(string path)
        {
            try
            {
                if (!File.Exists(path))
                    return null;

                // ファイルをロックしないよう、メモリに読み込んでから縮小デコードする
                using var stream = new MemoryStream(File.ReadAllBytes(path));
                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.DecodePixelWidth = PreviewDecodeWidth;
                image.StreamSource = stream;
                image.EndInit();
                image.Freeze();
                return image;
            }
            catch
            {
                return null;
            }
        }

        void RaiseCanExecuteChanged()
        {
            // コンストラクタ内でコマンド生成前に呼ばれる場合がある
            if (AddFilesCommand is null)
                return;
            foreach (var command in Commands)
                command.RaiseCanExecuteChanged();
        }

        public void Dispose()
        {
            item.PropertyChanged -= Item_PropertyChanged;
        }
    }
}
