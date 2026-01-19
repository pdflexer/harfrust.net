using HarfRust.Bindings;

namespace HarfRust.Bindings;

/// <summary>
/// FFI (native) backend for HarfRust using P/Invoke.
/// </summary>
public sealed class NativeBackend : IHarfRustBackend
{
    /// <summary>
    /// Singleton instance of the FFI backend.
    /// </summary>
    public static NativeBackend Instance { get; } = new();

    /// <inheritdoc />
    public IBackendBuffer CreateBuffer() => new NativeBuffer();

    /// <inheritdoc />
    public IBackendFont CreateFont(byte[] data) => new NativeFont(data);

    /// <inheritdoc />
    public IBackendFont CreateFont(byte[] data, uint index) => new NativeFont(data, index);
}
