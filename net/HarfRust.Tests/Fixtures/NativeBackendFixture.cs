using HarfRust.Bindings;

namespace HarfRust.Tests;

/// <summary>
/// Test fixture using the Native backend.
/// </summary>
public class NativeBackendFixture : BackendFixture
{
    public override IHarfRustBackend Backend => NativeBackend.Instance;
}
