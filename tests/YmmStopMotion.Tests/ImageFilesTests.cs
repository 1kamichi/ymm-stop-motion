using YmmStopMotion.Core;

namespace YmmStopMotion.Tests
{
    public sealed class ImageFilesTests : IDisposable
    {
        readonly string root = Path.Combine(Path.GetTempPath(), "ymm-stop-motion-tests-" + Guid.NewGuid().ToString("N"));

        public ImageFilesTests()
        {
            Directory.CreateDirectory(root);
        }

        public void Dispose()
        {
            Directory.Delete(root, recursive: true);
        }

        string Touch(params string[] parts)
        {
            var path = Path.Combine([root, .. parts]);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllBytes(path, []);
            return path;
        }

        [Theory]
        [InlineData("a.png", true)]
        [InlineData("a.JPG", true)]
        [InlineData("a.jpeg", true)]
        [InlineData("a.txt", false)]
        [InlineData("a", false)]
        public void Detects_image_extensions(string name, bool expected)
        {
            Assert.Equal(expected, ImageFiles.IsImageFile(name));
        }

        [Fact]
        public void Enumerates_folder_in_natural_order_and_skips_non_images()
        {
            Touch("shot", "IMG_10.jpg");
            Touch("shot", "IMG_2.jpg");
            Touch("shot", "IMG_1.JPG");
            Touch("shot", "memo.txt");
            Touch("shot", "sub", "IMG_3.jpg"); // サブフォルダは対象外

            var names = ImageFiles.EnumerateDirectory(Path.Combine(root, "shot")).Select(Path.GetFileName);
            Assert.Equal(["IMG_1.JPG", "IMG_2.jpg", "IMG_10.jpg"], names);
        }

        [Fact]
        public void Expand_puts_folders_first_then_sorted_files()
        {
            var b10 = Touch("b10.png");
            var b2 = Touch("b2.png");
            var txt = Touch("note.txt");
            Touch("folder2", "x.png");
            Touch("folder10", "y.png");

            var result = ImageFiles.Expand([b10, Path.Combine(root, "folder10"), txt, b2, Path.Combine(root, "folder2"), Path.Combine(root, "missing.png")])
                .Select(Path.GetFileName);

            Assert.Equal(["x.png", "y.png", "b2.png", "b10.png"], result);
        }

        [Fact]
        public void Missing_directory_returns_empty()
        {
            Assert.Empty(ImageFiles.EnumerateDirectory(Path.Combine(root, "nope")));
        }
    }
}
