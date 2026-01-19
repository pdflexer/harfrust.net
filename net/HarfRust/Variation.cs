namespace HarfRust;

/// <summary>
/// Variable font axis setting.
/// </summary>
public readonly struct Variation
{
    /// <summary>
    /// The variation axis tag as a 4-byte value.
    /// </summary>
    public readonly uint Tag;

    /// <summary>
    /// The variation value (in design units).
    /// </summary>
    public readonly float Value;

    /// <summary>
    /// Creates a variation from a 4-character tag string.
    /// </summary>
    public Variation(string tag, float value)
    {
        if (tag == null || tag.Length != 4)
            throw new ArgumentException("Tag must be exactly 4 characters.", nameof(tag));

        Tag = ((uint)tag[0] << 24) | ((uint)tag[1] << 16) | ((uint)tag[2] << 8) | (uint)tag[3];
        Value = value;
    }

    /// <summary>
    /// Creates a variation from a raw tag value.
    /// </summary>
    public Variation(uint tag, float value)
    {
        Tag = tag;
        Value = value;
    }

    /// <summary>Create a weight variation ('wght').</summary>
    public static Variation Weight(float value) => new("wght", value);

    /// <summary>Create a width variation ('wdth').</summary>
    public static Variation Width(float value) => new("wdth", value);

    /// <summary>Create a slant variation ('slnt').</summary>
    public static Variation Slant(float value) => new("slnt", value);

    /// <summary>Create an italic variation ('ital').</summary>
    public static Variation Italic(float value) => new("ital", value);

    /// <summary>Create an optical size variation ('opsz').</summary>
    public static Variation OpticalSize(float value) => new("opsz", value);
}
