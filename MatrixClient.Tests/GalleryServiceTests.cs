using MatrixClient.Services;
using MatrixClient.Tests.Helpers;

namespace MatrixClient.Tests;

public class GalleryServiceTests : IDisposable
{
    private readonly TempDir _webRoot = new();

    public void Dispose() => _webRoot.Dispose();

    private void Touch(string relativePath)
    {
        var full = Path.Combine(_webRoot.Path, "data", relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllBytes(full, []);
    }

    [Fact]
    public void GetCategories_ListsImagesPerCategorySorted()
    {
        Touch("retro/b.png");
        Touch("retro/a.bmp");
        Touch("food/apple.JPG");
        Touch("food/.DS_Store");
        Touch("food/readme.txt");
        Touch(".git/x.png");

        var categories = TestServices.Gallery(_webRoot.Path).GetCategories();

        Assert.Equal(["food", "retro"], categories.Keys);
        Assert.Equal(["food/apple.JPG"], categories["food"]);
        Assert.Equal(["retro/a.bmp", "retro/b.png"], categories["retro"]);
    }

    [Fact]
    public void GetCategories_EmptyWhenDataFolderMissing() =>
        Assert.Empty(TestServices.Gallery(_webRoot.Path).GetCategories());

    [Fact]
    public void GetImages_EmptyForUnknownCategory() =>
        Assert.Empty(TestServices.Gallery(_webRoot.Path).GetImages("nope"));

    [Fact]
    public void ResolvePath_ReturnsFullPathInsideDataFolder()
    {
        Touch("food/apple.png");

        var full = TestServices.Gallery(_webRoot.Path).ResolvePath("food/apple.png");

        Assert.Equal(Path.Combine(_webRoot.Path, "data", "food", "apple.png"), full);
    }

    [Fact]
    public void ResolvePath_RejectsTraversal() =>
        Assert.Throws<ArgumentException>(() => TestServices.Gallery(_webRoot.Path).ResolvePath("../x.png"));
}
