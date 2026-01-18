namespace HarfRust;

/// <summary>
/// Defines a variable font axis setting.
/// </summary>
public struct Variation
{
    /// <summary>
    /// The axis tag (e.g. "wght', "wdth").
    /// </summary>
    public uint Tag { get; set; }

    /// <summary>
    /// The value for the axis.
    /// </summary>
    public float Value { get; set; }

    /// <summary>
    /// Creates a new variation setting.
    /// </summary>
    /// <param name="tag">The 4-character axis tag (e.g. "wght").</param>
    /// <param name="value">The value.</param>
    public Variation(string tag, float value)
    {
        Tag = HarfRustBuffer.CreateScriptTag(tag);
        Value = value;
    }

    /// <summary>
    /// Creates a Weight ("wght") variation.
    /// </summary>
    public static Variation Weight(float value) => new Variation("wght", value);

    /// <summary>
    /// Creates a Width ("wdth") variation.
    /// </summary>
    public static Variation Width(float value) => new Variation("wdth", value);

    /// <summary>
    /// Creates a Slant ("slnt") variation.
    /// </summary>
    public static Variation Slant(float value) => new Variation("slnt", value);

    /// <summary>
    /// Creates an Optical Size ("opsz") variation.
    /// </summary>
    public static Variation OpticalSize(float value) => new Variation("opsz", value);

    /// <summary>
    /// Creates an Italic ("ital") variation.
    /// </summary>
    public static Variation Italic(float value) => new Variation("ital", value);
}
