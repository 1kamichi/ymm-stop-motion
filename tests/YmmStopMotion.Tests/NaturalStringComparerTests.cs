using YmmStopMotion.Core;

namespace YmmStopMotion.Tests
{
    public class NaturalStringComparerTests
    {
        [Fact]
        public void Sorts_numbers_numerically()
        {
            string[] input = ["IMG_10.jpg", "IMG_2.jpg", "IMG_1.jpg", "IMG_100.jpg", "IMG_20.jpg"];
            var sorted = input.Order(NaturalStringComparer.Instance).ToArray();
            Assert.Equal(["IMG_1.jpg", "IMG_2.jpg", "IMG_10.jpg", "IMG_20.jpg", "IMG_100.jpg"], sorted);
        }

        [Fact]
        public void Zero_padded_names_keep_shooting_order()
        {
            string[] input = ["DSC0010.JPG", "DSC0009.JPG", "DSC0100.JPG", "DSC0001.JPG"];
            var sorted = input.Order(NaturalStringComparer.Instance).ToArray();
            Assert.Equal(["DSC0001.JPG", "DSC0009.JPG", "DSC0010.JPG", "DSC0100.JPG"], sorted);
        }

        [Fact]
        public void Ignores_case_for_letters()
        {
            Assert.True(NaturalStringComparer.Instance.Compare("a2", "B1") < 0);
        }

        [Fact]
        public void Prefix_comes_first()
        {
            Assert.True(NaturalStringComparer.Instance.Compare("take", "take1") < 0);
            Assert.True(NaturalStringComparer.Instance.Compare("take1", "take1a") < 0);
        }

        [Fact]
        public void Huge_numbers_do_not_overflow()
        {
            Assert.True(NaturalStringComparer.Instance.Compare("f99999999999999999999", "f100000000000000000000") < 0);
        }

        [Fact]
        public void Is_consistent()
        {
            var c = NaturalStringComparer.Instance;
            Assert.Equal(0, c.Compare("x1", "x1"));
            Assert.NotEqual(0, c.Compare("x1", "x01"));
            Assert.Equal(-Math.Sign(c.Compare("x1", "x01")), Math.Sign(c.Compare("x01", "x1")));
            Assert.True(c.Compare(null, "a") < 0);
        }
    }
}
