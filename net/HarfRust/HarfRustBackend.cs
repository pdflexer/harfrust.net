using HarfRust.Bindings;

namespace HarfRust;

/// <summary>
/// Provides access to the current HarfRust backend.
/// </summary>
public static class HarfRustBackend
{
    private static readonly AsyncLocal<IHarfRustBackend?> _current = new();

    /// <summary>
    /// Gets or sets the current backend for the current async context.
    /// Defaults to the Native backend if not set.
    /// </summary>
    public static IHarfRustBackend Current
    {
        get => _current.Value ?? NativeBackend.Instance;
        set => _current.Value = value;
    }
}
