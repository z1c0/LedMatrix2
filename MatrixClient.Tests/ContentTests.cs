using MatrixClient.Models;
using MatrixClient.Tests.Helpers;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace MatrixClient.Tests;

// Guards the checked-in content (rules.yaml, gallery images) that the app loads at runtime
public class ContentTests
{
    private static readonly string[] DynamicModes = ["clock", "calendar", "weather"];

    private static readonly string DataDir = Path.Combine(RepoPaths.MatrixClientDir, "wwwroot", "data");

    private static AutoSendConfig LoadRules() =>
        AutoSendConfig.FromYaml(File.ReadAllText(Path.Combine(RepoPaths.MatrixClientDir, "rules.yaml")));

    public static TheoryData<string> GalleryImages()
    {
        var data = new TheoryData<string>();
        foreach (var f in Directory.GetFiles(DataDir, "*", SearchOption.AllDirectories)
                     .Where(f => !Path.GetFileName(f).StartsWith('.')))
            data.Add(Path.GetRelativePath(DataDir, f));
        return data;
    }

    [Theory]
    [MemberData(nameof(GalleryImages))]
    public void GalleryImage_Is32x32(string relativePath)
    {
        using var img = Image.Load<Rgba32>(Path.Combine(DataDir, relativePath));

        Assert.Equal((32, 32), (img.Width, img.Height));
    }

    [Fact]
    public void Rules_AreLoadedFromYaml()
    {
        var config = LoadRules();

        Assert.True(config.IntervalMinutes > 0);
        Assert.NotEmpty(config.Rules);
    }

    [Fact]
    public void Rules_PassStartupValidation() =>
        Assert.Empty(LoadRules().Validate(DataDir));

    [Fact]
    public void Rules_ReferenceExistingCategories()
    {
        foreach (var rule in LoadRules().Rules)
            foreach (var category in rule.AllCategories)
            {
                var exists = DynamicModes.Contains(category) ||
                             Directory.EnumerateFiles(Path.Combine(DataDir, category)).Any();
                Assert.True(exists, $"{rule.Name}: category '{category}' has no images");
            }
    }
}
