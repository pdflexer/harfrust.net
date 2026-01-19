using HarfRust.Bindings;

namespace HarfRust.Tests;

/// <summary>
/// Test fixture using the FFI (native) backend.
/// </summary>
public class FfiBackendFixture : BackendFixture
{
    public override IHarfRustBackend Backend => NativeBackend.Instance;
}
