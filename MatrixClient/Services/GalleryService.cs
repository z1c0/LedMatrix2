namespace MatrixClient.Services;

// The image gallery under wwwroot/data: one folder per category, one image per file
public class GalleryService(IWebHostEnvironment env)
{
    private static readonly HashSet<string> ImageExtensions =
        new([".png", ".bmp", ".jpg", ".jpeg", ".gif", ".webp"], StringComparer.OrdinalIgnoreCase);

    public string DataRoot => Path.Combine(env.WebRootPath, "data");

    public SortedDictionary<string, string[]> GetCategories()
    {
        var categories = new SortedDictionary<string, string[]>(StringComparer.Ordinal);
        if (!Directory.Exists(DataRoot)) return categories;

        foreach (var dir in Directory.GetDirectories(DataRoot).Where(d => !IsHidden(d)))
            categories[Path.GetFileName(dir)] = GetImages(Path.GetFileName(dir));
        return categories;
    }

    // Gallery paths ("category/file.png") of all images in a category, sorted; empty if it doesn't exist
    public string[] GetImages(string category)
    {
        var dir = Path.Combine(DataRoot, category);
        if (!Directory.Exists(dir)) return [];

        return Directory.GetFiles(dir)
            .Where(f => !IsHidden(f) && ImageExtensions.Contains(Path.GetExtension(f)))
            .Select(f => $"{category}/{Path.GetFileName(f)}")
            .Order(StringComparer.Ordinal)
            .ToArray();
    }

    // Maps a gallery path to disk, refusing anything outside the data folder
    public string ResolvePath(string galleryPath)
    {
        var dataRoot = Path.GetFullPath(DataRoot) + Path.DirectorySeparatorChar;
        var fullPath = Path.GetFullPath(Path.Combine(dataRoot, galleryPath));

        if (!fullPath.StartsWith(dataRoot, StringComparison.Ordinal))
            throw new ArgumentException($"Invalid image path: {galleryPath}");
        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"Image not found: {galleryPath}");

        return fullPath;
    }

    private static bool IsHidden(string path) => Path.GetFileName(path).StartsWith('.');
}
