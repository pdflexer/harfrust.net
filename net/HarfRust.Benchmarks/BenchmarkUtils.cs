using System;
using System.IO;

namespace HarfRust.Benchmarks;

public static class BenchmarkUtils
{
    private static byte[]? _cachedFontData;

    public static byte[] GetFontData()
    {
        if (_cachedFontData != null) return _cachedFontData;

        string? fontPath = null;
        
        // Check for specific system fonts based on OS
        var systemFonts = new[] {
            @"C:\Windows\Fonts\arial.ttf",             // Windows
            @"/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf", // Linux (typical)
            @"/Library/Fonts/Arial.ttf"                // macOS
        };

        foreach (var path in systemFonts)
        {
            if (File.Exists(path))
            {
                fontPath = path;
                break;
            }
        }
        
        if (fontPath == null)
        {
             // Fallback to searching relative paths if system font not found (e.g. CI environment)
            var searchPaths = new[]
            {
                "../../../../../../rust/tests/fonts/Hack-Regular.ttf",
                "../../../../rust/tests/fonts/Hack-Regular.ttf",
                "../../../rust/tests/fonts/Hack-Regular.ttf"
            };
            
            foreach (var path in searchPaths)
            {
                if (File.Exists(path))
                {
                    fontPath = Path.GetFullPath(path);
                    break;
                }
            }
        }
        
        if (fontPath == null || !File.Exists(fontPath))
        {
            throw new FileNotFoundException($"Could not find any suitable test font.");
        }

        _cachedFontData = File.ReadAllBytes(fontPath);
        return _cachedFontData;
    }
}
