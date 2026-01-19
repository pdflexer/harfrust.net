namespace HarfRust.Tests;

/// <summary>
/// Base fixture providing shared resources for backend tests.
/// </summary>
public abstract class BackendFixture : IDisposable
{
    private static byte[]? _testFontData;
    
    /// <summary>
    /// Gets the backend instance for this fixture.
    /// </summary>
    public abstract IHarfRustBackend Backend { get; }
    
    /// <summary>
    /// Gets test font data (cached across tests).
    /// </summary>
    public byte[] GetTestFontData()
    {
        if (_testFontData != null)
            return _testFontData;
            
        var possiblePaths = new[]
        {
            // Windows
            @"C:\Windows\Fonts\arial.ttf",
            @"C:\Windows\Fonts\segoeui.ttf",
            @"C:\Windows\Fonts\tahoma.ttf",
            // Linux
            "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf",
            "/usr/share/fonts/truetype/liberation/LiberationSans-Regular.ttf",
            "/usr/share/fonts/truetype/freefont/FreeSans.ttf",
            // macOS
            "/Library/Fonts/Arial.ttf",
            "/System/Library/Fonts/Supplemental/Arial.ttf", 
            "/System/Library/Fonts/Helvetica.ttc"
        };
        
        foreach (var path in possiblePaths)
        {
            if (File.Exists(path))
            {
                _testFontData = File.ReadAllBytes(path);
                return _testFontData;
            }
        }
        
        throw new InvalidOperationException("No system font available for testing");
    }

    public virtual void Dispose() { }
}
