//! FFI wrapper for harfrust text shaping library.
//!
//! This crate provides C-compatible functions that can be called from .NET
//! via P/Invoke. Objects are exposed as opaque pointers to allow .NET to
//! manage their lifecycle.

use std::ffi::CStr;
use std::os::raw::c_char;
use std::pin::Pin;

// =============================================================================
// FFI-safe structs (repr(C) for direct marshalling)
// =============================================================================

/// Glyph information after shaping.
#[repr(C)]
#[derive(Clone, Copy, Debug, Default)]
pub struct HarfRustGlyphInfo {
    /// The glyph ID in the font.
    pub glyph_id: u32,
    /// The cluster index (position in original text).
    pub cluster: u32,
}

/// Glyph positioning information after shaping.
#[repr(C)]
#[derive(Clone, Copy, Debug, Default)]
pub struct HarfRustGlyphPosition {
    /// Horizontal advance after drawing this glyph.
    pub x_advance: i32,
    /// Vertical advance after drawing this glyph.
    pub y_advance: i32,
    /// Horizontal offset for drawing.
    pub x_offset: i32,
    /// Vertical offset for drawing.
    pub y_offset: i32,
}

/// Text direction for shaping.
#[repr(C)]
#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub enum HarfRustDirection {
    /// Initial, unset direction.
    Invalid = 0,
    /// Left-to-right text.
    LeftToRight = 4,
    /// Right-to-left text.
    RightToLeft = 5,
    /// Top-to-bottom text.
    TopToBottom = 6,
    /// Bottom-to-top text.
    BottomToTop = 7,
}

impl From<harfrust::Direction> for HarfRustDirection {
    fn from(dir: harfrust::Direction) -> Self {
        match dir {
            harfrust::Direction::Invalid => HarfRustDirection::Invalid,
            harfrust::Direction::LeftToRight => HarfRustDirection::LeftToRight,
            harfrust::Direction::RightToLeft => HarfRustDirection::RightToLeft,
            harfrust::Direction::TopToBottom => HarfRustDirection::TopToBottom,
            harfrust::Direction::BottomToTop => HarfRustDirection::BottomToTop,
        }
    }
}

impl From<HarfRustDirection> for harfrust::Direction {
    fn from(dir: HarfRustDirection) -> Self {
        match dir {
            HarfRustDirection::Invalid => harfrust::Direction::Invalid,
            HarfRustDirection::LeftToRight => harfrust::Direction::LeftToRight,
            HarfRustDirection::RightToLeft => harfrust::Direction::RightToLeft,
            HarfRustDirection::TopToBottom => harfrust::Direction::TopToBottom,
            HarfRustDirection::BottomToTop => harfrust::Direction::BottomToTop,
        }
    }
}

/// OpenType feature for shaping.
#[repr(C)]
#[derive(Clone, Copy, Debug)]
pub struct HarfRustFeature {
    /// The feature tag (e.g. 'liga', 'kern').
    pub tag: u32,
    /// The value of the feature (0 = disabled, 1 = enabled, or other values).
    pub value: u32,
    /// The start index in the buffer to apply this feature.
    pub start: u32,
    /// The end index in the buffer to apply this feature (u32::MAX for end).
    pub end: u32,
}

/// Font variation settings.
#[repr(C)]
#[derive(Clone, Copy, Debug)]
pub struct HarfRustVariation {
    /// The variation tag (e.g. 'wght', 'wdth').
    pub tag: u32,
    /// The variation value (in design units).
    pub value: f32,
}

// =============================================================================
// Opaque wrapper types
// =============================================================================

/// Opaque wrapper around harfrust's UnicodeBuffer.
pub struct HarfRustBuffer {
    inner: harfrust::UnicodeBuffer,
}

/// Internal structure that holds font data and parsed structures.
/// Uses a two-phase construction to ensure proper lifetimes.
struct FontInner {
    data: Pin<Box<[u8]>>,
}

impl FontInner {
    fn new(data: Vec<u8>) -> Self {
        Self {
            data: Pin::new(data.into_boxed_slice()),
        }
    }
    
    fn data(&self) -> &[u8] {
        &self.data
    }
}

/// Opaque wrapper that owns font data and provides shaping capabilities.
pub struct HarfRustFont {
    inner: FontInner,
    // These are constructed after FontInner and borrow from it.
    // We use raw pointers to avoid lifetime issues in FFI.
    // SAFETY: font_ref and shaper_data point to data owned by inner.data
    // and are only valid while HarfRustFont is alive.
}

/// Opaque wrapper around harfrust's GlyphBuffer (shaping result).
pub struct HarfRustGlyphBuffer {
    inner: harfrust::GlyphBuffer,
    // Cache for FFI-safe glyph data
    infos_cache: Vec<HarfRustGlyphInfo>,
    positions_cache: Vec<HarfRustGlyphPosition>,
}

// =============================================================================
// Buffer functions
// =============================================================================

/// Creates a new empty buffer for text shaping.
#[no_mangle]
pub extern "C" fn harfrust_buffer_new() -> *mut HarfRustBuffer {
    let buffer = HarfRustBuffer {
        inner: harfrust::UnicodeBuffer::new(),
    };
    Box::into_raw(Box::new(buffer))
}

/// Adds a UTF-8 string to the buffer.
#[no_mangle]
pub unsafe extern "C" fn harfrust_buffer_add_str(
    buffer: *mut HarfRustBuffer,
    text: *const c_char,
) -> i32 {
    if buffer.is_null() {
        return -1;
    }
    if text.is_null() {
        return -2;
    }

    let c_str = unsafe { CStr::from_ptr(text) };
    let rust_str = match c_str.to_str() {
        Ok(s) => s,
        Err(_) => return -3,
    };

    let buffer_ref = unsafe { &mut *buffer };
    buffer_ref.inner.push_str(rust_str);

    0
}

/// Adds a UTF-16 string to the buffer.
#[no_mangle]
pub unsafe extern "C" fn harfrust_buffer_add_utf16(
    buffer: *mut HarfRustBuffer,
    text: *const u16,
    len: i32,
) -> i32 {
    if buffer.is_null() {
        return -1;
    }
    if text.is_null() || len < 0 {
        return -2;
    }

    let slice = unsafe { std::slice::from_raw_parts(text, len as usize) };
    let buffer_ref = unsafe { &mut *buffer };
    
    let mut cluster = 0;
    for c in std::char::decode_utf16(slice.iter().cloned()) {
        let ch = match c {
            Ok(ch) => ch,
            Err(_) => std::char::REPLACEMENT_CHARACTER,
        };
        
        let char_len = ch.len_utf16() as u32;
        // harfrust::UnicodeBuffer should have `add` method taking (char, cluster)
        buffer_ref.inner.add(ch, cluster);
        cluster += char_len;
    }

    0
}

/// Returns the number of characters currently in the buffer.
#[no_mangle]
pub unsafe extern "C" fn harfrust_buffer_len(buffer: *const HarfRustBuffer) -> i32 {
    if buffer.is_null() {
        return -1;
    }

    let buffer_ref = unsafe { &*buffer };
    buffer_ref.inner.len() as i32
}

/// Clears all content from the buffer, preparing it for reuse.
#[no_mangle]
pub unsafe extern "C" fn harfrust_buffer_clear(buffer: *mut HarfRustBuffer) {
    if buffer.is_null() {
        return;
    }

    let buffer_ref = unsafe { &mut *buffer };
    buffer_ref.inner.clear();
}

/// Frees a buffer previously created by `harfrust_buffer_new`.
#[no_mangle]
pub unsafe extern "C" fn harfrust_buffer_free(buffer: *mut HarfRustBuffer) {
    if !buffer.is_null() {
        unsafe { drop(Box::from_raw(buffer)) };
    }
}

// =============================================================================
// Buffer configuration functions (Phase 2)
// =============================================================================

/// Sets the text direction of the buffer.
#[no_mangle]
pub unsafe extern "C" fn harfrust_buffer_set_direction(
    buffer: *mut HarfRustBuffer,
    direction: HarfRustDirection,
) {
    if buffer.is_null() {
        return;
    }

    let buffer_ref = unsafe { &mut *buffer };
    buffer_ref.inner.set_direction(direction.into());
}

/// Gets the text direction of the buffer.
#[no_mangle]
pub unsafe extern "C" fn harfrust_buffer_get_direction(
    buffer: *const HarfRustBuffer,
) -> HarfRustDirection {
    if buffer.is_null() {
        return HarfRustDirection::Invalid;
    }

    let buffer_ref = unsafe { &*buffer };
    buffer_ref.inner.direction().into()
}

/// Sets the script of the buffer using an ISO 15924 tag (4 bytes as u32).
/// Example: "Latn" = 0x4C61746E
#[no_mangle]
pub unsafe extern "C" fn harfrust_buffer_set_script(buffer: *mut HarfRustBuffer, script_tag: u32) {
    if buffer.is_null() {
        return;
    }

    let buffer_ref = unsafe { &mut *buffer };
    let tag = harfrust::Tag::new(&script_tag.to_be_bytes());
    if let Some(script) = harfrust::Script::from_iso15924_tag(tag) {
        buffer_ref.inner.set_script(script);
    }
}

/// Gets the script of the buffer as an ISO 15924 tag (4 bytes as u32).
/// Returns 0 if no script is set.
#[no_mangle]
pub unsafe extern "C" fn harfrust_buffer_get_script(buffer: *const HarfRustBuffer) -> u32 {
    if buffer.is_null() {
        return 0;
    }

    let buffer_ref = unsafe { &*buffer };
    let tag = buffer_ref.inner.script().tag();
    u32::from_be_bytes(tag.into_bytes())
}

/// Sets the language of the buffer from a BCP 47 language tag string.
/// Example: "en", "en-US", "zh-Hans"
#[no_mangle]
pub unsafe extern "C" fn harfrust_buffer_set_language(
    buffer: *mut HarfRustBuffer,
    language: *const c_char,
) -> i32 {
    if buffer.is_null() {
        return -1;
    }
    if language.is_null() {
        return -2;
    }

    let c_str = unsafe { CStr::from_ptr(language) };
    let lang_str = match c_str.to_str() {
        Ok(s) => s,
        Err(_) => return -3,
    };

    let buffer_ref = unsafe { &mut *buffer };
    if let Ok(lang) = lang_str.parse::<harfrust::Language>() {
        buffer_ref.inner.set_language(lang);
        0
    } else {
        -4
    }
}

/// Guesses and sets the segment properties (direction, script, language)
/// based on the buffer contents.
#[no_mangle]
pub unsafe extern "C" fn harfrust_buffer_guess_segment_properties(buffer: *mut HarfRustBuffer) {
    if buffer.is_null() {
        return;
    }

    let buffer_ref = unsafe { &mut *buffer };
    buffer_ref.inner.guess_segment_properties();
}

// =============================================================================
// Font functions
// =============================================================================

/// Creates a font from raw font data (TTF/OTF bytes).
#[no_mangle]
pub unsafe extern "C" fn harfrust_font_from_data(data: *const u8, len: i32) -> *mut HarfRustFont {
    if data.is_null() || len <= 0 {
        return std::ptr::null_mut();
    }

    let slice = unsafe { std::slice::from_raw_parts(data, len as usize) };
    let data_vec = slice.to_vec();
    
    // Verify the font data is valid before accepting it
    if harfrust::FontRef::new(&data_vec).is_err() {
        return std::ptr::null_mut();
    }

    let inner = FontInner::new(data_vec);
    let wrapper = HarfRustFont { inner };
    Box::into_raw(Box::new(wrapper))
}

/// Creates a font from raw font data at a specific index (for font collections).
#[no_mangle]
pub unsafe extern "C" fn harfrust_font_from_data_index(
    data: *const u8,
    len: i32,
    index: u32,
) -> *mut HarfRustFont {
    if data.is_null() || len <= 0 {
        return std::ptr::null_mut();
    }

    let slice = unsafe { std::slice::from_raw_parts(data, len as usize) };
    let data_vec = slice.to_vec();
    
    // Verify the font data is valid before accepting it
    if harfrust::FontRef::from_index(&data_vec, index).is_err() {
        return std::ptr::null_mut();
    }

    let inner = FontInner::new(data_vec);
    let wrapper = HarfRustFont { inner };
    Box::into_raw(Box::new(wrapper))
}

/// Returns the font's units per em.
#[no_mangle]
pub unsafe extern "C" fn harfrust_font_units_per_em(font: *const HarfRustFont) -> i32 {
    if font.is_null() {
        return -1;
    }

    let font_wrapper = unsafe { &*font };
    let data = font_wrapper.inner.data();
    
    // Parse the font temporarily
    let font_ref = match harfrust::FontRef::new(data) {
        Ok(f) => f,
        Err(_) => return -1,
    };
    
    let shaper_data = harfrust::ShaperData::new(&font_ref);
    let shaper = shaper_data.shaper(&font_ref).build();
    shaper.units_per_em()
}

/// Frees a font previously created by `harfrust_font_from_data`.
#[no_mangle]
pub unsafe extern "C" fn harfrust_font_free(font: *mut HarfRustFont) {
    if !font.is_null() {
        unsafe { drop(Box::from_raw(font)) };
    }
}

// =============================================================================
// Shape function
// =============================================================================

/// Shapes text in a buffer using the given font.
#[no_mangle]
pub unsafe extern "C" fn harfrust_shape(
    font: *const HarfRustFont,
    buffer: *mut HarfRustBuffer,
) -> *mut HarfRustGlyphBuffer {
    if font.is_null() || buffer.is_null() {
        return std::ptr::null_mut();
    }

    let font_wrapper = unsafe { &*font };
    let mut buffer_box = unsafe { Box::from_raw(buffer) };
    let data = font_wrapper.inner.data();

    // Parse the font for this shaping operation
    let font_ref = match harfrust::FontRef::new(data) {
        Ok(f) => f,
        Err(_) => return std::ptr::null_mut(),
    };
    
    let shaper_data = harfrust::ShaperData::new(&font_ref);
    let shaper = shaper_data.shaper(&font_ref).build();

    // Guess segment properties only if direction is not explicitly set
    if buffer_box.inner.direction() == harfrust::Direction::Invalid {
        buffer_box.inner.guess_segment_properties();
    }

    // Perform shaping
    let glyph_buffer = shaper.shape(buffer_box.inner, &[]);

    // Convert to FFI-safe format
    let infos: Vec<HarfRustGlyphInfo> = glyph_buffer
        .glyph_infos()
        .iter()
        .map(|info| HarfRustGlyphInfo {
            glyph_id: info.glyph_id,
            cluster: info.cluster,
        })
        .collect();

    let positions: Vec<HarfRustGlyphPosition> = glyph_buffer
        .glyph_positions()
        .iter()
        .map(|pos| HarfRustGlyphPosition {
            x_advance: pos.x_advance,
            y_advance: pos.y_advance,
            x_offset: pos.x_offset,
            y_offset: pos.y_offset,
        })
        .collect();

    let wrapper = HarfRustGlyphBuffer {
        inner: glyph_buffer,
        infos_cache: infos,
        positions_cache: positions,
    };

    Box::into_raw(Box::new(wrapper))
}

/// Shapes text in a buffer using the given font and OpenType features.
#[no_mangle]
pub unsafe extern "C" fn harfrust_shape_with_features(
    font: *const HarfRustFont,
    buffer: *mut HarfRustBuffer,
    features: *const HarfRustFeature,
    num_features: u32,
) -> *mut HarfRustGlyphBuffer {
    if font.is_null() || buffer.is_null() {
        return std::ptr::null_mut();
    }

    let font_wrapper = unsafe { &*font };
    let mut buffer_box = unsafe { Box::from_raw(buffer) };
    let data = font_wrapper.inner.data();

    // Parse the font for this shaping operation
    let font_ref = match harfrust::FontRef::new(data) {
        Ok(f) => f,
        Err(_) => return std::ptr::null_mut(),
    };
    
    let shaper_data = harfrust::ShaperData::new(&font_ref);
    let shaper = shaper_data.shaper(&font_ref).build();

    // Guess segment properties only if direction is not explicitly set
    if buffer_box.inner.direction() == harfrust::Direction::Invalid {
        buffer_box.inner.guess_segment_properties();
    }

    // Prepare features
    let mut rust_features = Vec::with_capacity(num_features as usize);
    if !features.is_null() && num_features > 0 {
        let feature_slice = std::slice::from_raw_parts(features, num_features as usize);
        for f in feature_slice {
            rust_features.push(harfrust::Feature {
                tag: harfrust::Tag::new(&f.tag.to_be_bytes()),
                value: f.value,
                start: f.start,
                end: f.end,
            });
        }
    }

    // Perform shaping
    let glyph_buffer = shaper.shape(buffer_box.inner, &rust_features);

    // Convert to FFI-safe format
    let infos: Vec<HarfRustGlyphInfo> = glyph_buffer
        .glyph_infos()
        .iter()
        .map(|info| HarfRustGlyphInfo {
            glyph_id: info.glyph_id,
            cluster: info.cluster,
        })
        .collect();

    let positions: Vec<HarfRustGlyphPosition> = glyph_buffer
        .glyph_positions()
        .iter()
        .map(|pos| HarfRustGlyphPosition {
            x_advance: pos.x_advance,
            y_advance: pos.y_advance,
            x_offset: pos.x_offset,
            y_offset: pos.y_offset,
        })
        .collect();

    let wrapper = HarfRustGlyphBuffer {
        inner: glyph_buffer,
        infos_cache: infos,
        positions_cache: positions,
    };

    Box::into_raw(Box::new(wrapper))
}

/// Shapes text in a buffer using the given font, features, and variable font settings.
#[no_mangle]
pub unsafe extern "C" fn harfrust_shape_full(
    font: *const HarfRustFont,
    buffer: *mut HarfRustBuffer,
    features: *const HarfRustFeature,
    num_features: u32,
    variations: *const HarfRustVariation,
    num_variations: u32,
) -> *mut HarfRustGlyphBuffer {
    if font.is_null() || buffer.is_null() {
        return std::ptr::null_mut();
    }

    let font_wrapper = unsafe { &*font };
    let mut buffer_box = unsafe { Box::from_raw(buffer) };
    let data = font_wrapper.inner.data();

    // Parse the font for this shaping operation
    let font_ref = match harfrust::FontRef::new(data) {
        Ok(f) => f,
        Err(_) => return std::ptr::null_mut(),
    };
    
    // Handle variable font instance
    let instance_opt = if !variations.is_null() && num_variations > 0 {
        let var_slice = std::slice::from_raw_parts(variations, num_variations as usize);
        
        let rust_variations: Vec<harfrust::Variation> = var_slice.iter().map(|v| {
            let tag = harfrust::Tag::new(&v.tag.to_be_bytes());
            (tag, v.value).into()
        }).collect();
        
        Some(harfrust::ShaperInstance::from_variations(&font_ref, rust_variations))
    } else {
        None
    };

    let shaper_data = harfrust::ShaperData::new(&font_ref);
    let mut builder = shaper_data.shaper(&font_ref);
    
    if let Some(inst) = &instance_opt {
        builder = builder.instance(Some(inst));
    }
    
    let shaper = builder.build();

    // Guess segment properties only if direction is not explicitly set
    if buffer_box.inner.direction() == harfrust::Direction::Invalid {
        buffer_box.inner.guess_segment_properties();
    }

    // Prepare features
    let mut rust_features = Vec::with_capacity(num_features as usize);
    if !features.is_null() && num_features > 0 {
        let feature_slice = std::slice::from_raw_parts(features, num_features as usize);
        for f in feature_slice {
            rust_features.push(harfrust::Feature {
                tag: harfrust::Tag::new(&f.tag.to_be_bytes()),
                value: f.value,
                start: f.start,
                end: f.end,
            });
        }
    }

    // Perform shaping
    let glyph_buffer = shaper.shape(buffer_box.inner, &rust_features);

    // Convert to FFI-safe format
    let infos: Vec<HarfRustGlyphInfo> = glyph_buffer
        .glyph_infos()
        .iter()
        .map(|info| HarfRustGlyphInfo {
            glyph_id: info.glyph_id,
            cluster: info.cluster,
        })
        .collect();

    let positions: Vec<HarfRustGlyphPosition> = glyph_buffer
        .glyph_positions()
        .iter()
        .map(|pos| HarfRustGlyphPosition {
            x_advance: pos.x_advance,
            y_advance: pos.y_advance,
            x_offset: pos.x_offset,
            y_offset: pos.y_offset,
        })
        .collect();

    let wrapper = HarfRustGlyphBuffer {
        inner: glyph_buffer,
        infos_cache: infos,
        positions_cache: positions,
    };

    Box::into_raw(Box::new(wrapper))
}

// =============================================================================
// Glyph buffer functions
// =============================================================================

/// Returns the number of glyphs in the glyph buffer.
#[no_mangle]
pub unsafe extern "C" fn harfrust_glyph_buffer_len(buffer: *const HarfRustGlyphBuffer) -> i32 {
    if buffer.is_null() {
        return -1;
    }

    let buffer_ref = unsafe { &*buffer };
    buffer_ref.inner.len() as i32
}

/// Returns a pointer to the glyph info array.
#[no_mangle]
pub unsafe extern "C" fn harfrust_glyph_buffer_get_infos(
    buffer: *const HarfRustGlyphBuffer,
) -> *const HarfRustGlyphInfo {
    if buffer.is_null() {
        return std::ptr::null();
    }

    let buffer_ref = unsafe { &*buffer };
    buffer_ref.infos_cache.as_ptr()
}

/// Returns a pointer to the glyph position array.
#[no_mangle]
pub unsafe extern "C" fn harfrust_glyph_buffer_get_positions(
    buffer: *const HarfRustGlyphBuffer,
) -> *const HarfRustGlyphPosition {
    if buffer.is_null() {
        return std::ptr::null();
    }

    let buffer_ref = unsafe { &*buffer };
    buffer_ref.positions_cache.as_ptr()
}

/// Clears the glyph buffer and returns a new unicode buffer for reuse.
#[no_mangle]
pub unsafe extern "C" fn harfrust_glyph_buffer_into_buffer(
    buffer: *mut HarfRustGlyphBuffer,
) -> *mut HarfRustBuffer {
    if buffer.is_null() {
        return std::ptr::null_mut();
    }

    let buffer_box = unsafe { Box::from_raw(buffer) };
    let unicode_buffer = buffer_box.inner.clear();

    let wrapper = HarfRustBuffer {
        inner: unicode_buffer,
    };
    Box::into_raw(Box::new(wrapper))
}

/// Frees a glyph buffer previously created by `harfrust_shape`.
#[no_mangle]
pub unsafe extern "C" fn harfrust_glyph_buffer_free(buffer: *mut HarfRustGlyphBuffer) {
    if !buffer.is_null() {
        unsafe { drop(Box::from_raw(buffer)) };
    }
}

// =============================================================================
// Tests
// =============================================================================

#[cfg(test)]
mod tests {
    use super::*;
    use std::ffi::CString;

    #[test]
    fn test_buffer_lifecycle() {
        unsafe {
            let buffer = harfrust_buffer_new();
            assert!(!buffer.is_null());

            let text = CString::new("Hello, world!").unwrap();
            let result = harfrust_buffer_add_str(buffer, text.as_ptr());
            assert_eq!(result, 0);

            let len = harfrust_buffer_len(buffer);
            assert_eq!(len, 13);

            harfrust_buffer_clear(buffer);
            let len = harfrust_buffer_len(buffer);
            assert_eq!(len, 0);

            harfrust_buffer_free(buffer);
        }
    }

    #[test]
    fn test_null_safety() {
        unsafe {
            assert_eq!(
                harfrust_buffer_add_str(std::ptr::null_mut(), std::ptr::null()),
                -1
            );
            assert_eq!(harfrust_buffer_len(std::ptr::null()), -1);
            harfrust_buffer_clear(std::ptr::null_mut());
            harfrust_buffer_free(std::ptr::null_mut());

            assert!(harfrust_font_from_data(std::ptr::null(), 0).is_null());
            assert_eq!(harfrust_font_units_per_em(std::ptr::null()), -1);
            harfrust_font_free(std::ptr::null_mut());

            assert!(harfrust_shape(std::ptr::null(), std::ptr::null_mut()).is_null());
            assert_eq!(harfrust_glyph_buffer_len(std::ptr::null()), -1);
            assert!(harfrust_glyph_buffer_get_infos(std::ptr::null()).is_null());
            assert!(harfrust_glyph_buffer_get_positions(std::ptr::null()).is_null());
            assert!(harfrust_glyph_buffer_into_buffer(std::ptr::null_mut()).is_null());
            harfrust_glyph_buffer_free(std::ptr::null_mut());
        }
    }
    
    #[test]
    fn test_shape_with_font() {
        // Try to load a system font for testing
        let font_paths = [
            r"C:\Windows\Fonts\arial.ttf",
            r"C:\Windows\Fonts\segoeui.ttf",
            r"C:\Windows\Fonts\tahoma.ttf",
        ];
        
        let font_data = font_paths.iter()
            .find_map(|path| std::fs::read(path).ok())
            .expect("No system font found for testing");
        
        // Test harfrust directly first
        let font_ref = harfrust::FontRef::new(&font_data).expect("Font parse failed");
        let shaper_data = harfrust::ShaperData::new(&font_ref);
        let shaper = shaper_data.shaper(&font_ref).build();
        
        let mut buffer = harfrust::UnicodeBuffer::new();
        buffer.push_str("Hello");
        buffer.guess_segment_properties();  // Required to set direction!
        
        let result = shaper.shape(buffer, &[]);
        assert_eq!(result.len(), 5);
    }
    
    #[test]
    fn test_buffer_direction() {
        unsafe {
            let buffer = harfrust_buffer_new();
            
            // Default should be Invalid
            assert_eq!(harfrust_buffer_get_direction(buffer), HarfRustDirection::Invalid);
            
            // Set and get direction
            harfrust_buffer_set_direction(buffer, HarfRustDirection::RightToLeft);
            assert_eq!(harfrust_buffer_get_direction(buffer), HarfRustDirection::RightToLeft);
            
            harfrust_buffer_set_direction(buffer, HarfRustDirection::LeftToRight);
            assert_eq!(harfrust_buffer_get_direction(buffer), HarfRustDirection::LeftToRight);
            
            harfrust_buffer_free(buffer);
        }
    }
    
    #[test]
    fn test_buffer_script() {
        unsafe {
            let buffer = harfrust_buffer_new();
            
            // Set Latin script (0x4C61746E = "Latn")
            let latn_tag = u32::from_be_bytes(*b"Latn");
            harfrust_buffer_set_script(buffer, latn_tag);
            assert_eq!(harfrust_buffer_get_script(buffer), latn_tag);
            
            // Set Arabic script (0x41726162 = "Arab")
            let arab_tag = u32::from_be_bytes(*b"Arab");
            harfrust_buffer_set_script(buffer, arab_tag);
            assert_eq!(harfrust_buffer_get_script(buffer), arab_tag);
            
            harfrust_buffer_free(buffer);
        }
    }
    
    #[test]
    fn test_buffer_language() {
        unsafe {
            let buffer = harfrust_buffer_new();
            
            // Set English language
            let lang = CString::new("en").unwrap();
            let result = harfrust_buffer_set_language(buffer, lang.as_ptr());
            assert_eq!(result, 0);
            
            // Set more specific language tag
            let lang = CString::new("en-US").unwrap();
            let result = harfrust_buffer_set_language(buffer, lang.as_ptr());
            assert_eq!(result, 0);
            
            harfrust_buffer_free(buffer);
        }
    }
    
    #[test]
    fn test_explicit_direction_not_overridden() {
        // Try to load a system font for testing
        let font_paths = [
            r"C:\Windows\Fonts\arial.ttf",
            r"C:\Windows\Fonts\segoeui.ttf",
            r"C:\Windows\Fonts\tahoma.ttf",
        ];
        
        let font_data = font_paths.iter()
            .find_map(|path| std::fs::read(path).ok())
            .expect("No system font found for testing");
        
        unsafe {
            let font = harfrust_font_from_data(font_data.as_ptr(), font_data.len() as i32);
            let buffer = harfrust_buffer_new();
            
            // Set RTL direction explicitly
            harfrust_buffer_set_direction(buffer, HarfRustDirection::RightToLeft);
            
            let text = CString::new("Hello").unwrap();
            harfrust_buffer_add_str(buffer, text.as_ptr());
            
            // Shape should work (direction is set, won't call guess)
            let glyph_buffer = harfrust_shape(font, buffer);
            assert!(!glyph_buffer.is_null());
            assert_eq!(harfrust_glyph_buffer_len(glyph_buffer), 5);
            
            harfrust_glyph_buffer_free(glyph_buffer);
            harfrust_font_free(font);
        }
    }

    #[test]
    fn test_shape_with_features() {
        // Try to load a system font for testing
        let font_paths = [
            r"C:\Windows\Fonts\calibri.ttf",
            r"C:\Windows\Fonts\arial.ttf",
        ];
        
        let font_data = font_paths.iter()
            .find_map(|path| std::fs::read(path).ok())
            .expect("No system font found for testing");
            
        unsafe {
            let font = harfrust_font_from_data(font_data.as_ptr(), font_data.len() as i32);
            let buffer = harfrust_buffer_new();
            
            let text = CString::new("fi").unwrap();
            harfrust_buffer_add_str(buffer, text.as_ptr());
            
            // Disable ligatures: liga = 0
            let features = [
                HarfRustFeature {
                    tag: u32::from_be_bytes(*b"liga"),
                    value: 0,
                    start: 0,
                    end: u32::MAX,
                }
            ];
            
            let glyph_buffer = harfrust_shape_with_features(
                font,
                buffer,
                features.as_ptr(),
                features.len() as u32
            );
            
            assert!(!glyph_buffer.is_null());
            
            // Just verify it produced something
            let len = harfrust_glyph_buffer_len(glyph_buffer);
            assert!(len > 0);
            
            harfrust_glyph_buffer_free(glyph_buffer);
            harfrust_font_free(font);
        }
    }

    #[test]
    fn test_shape_with_variations() {
        // Try to load a system font for testing
        let font_paths = [
            r"C:\Windows\Fonts\calibri.ttf",
            r"C:\Windows\Fonts\arial.ttf",
        ];
        
        let font_data = font_paths.iter()
            .find_map(|path| std::fs::read(path).ok())
            .expect("No system font found for testing");
            
        unsafe {
            let font = harfrust_font_from_data(font_data.as_ptr(), font_data.len() as i32);
            let buffer = harfrust_buffer_new();
            
            let text = CString::new("Hello").unwrap();
            harfrust_buffer_add_str(buffer, text.as_ptr());
            
            // Set variation: wght = 700.0 (Bold)
            let variations = [
                HarfRustVariation {
                    tag: u32::from_be_bytes(*b"wght"),
                    value: 700.0,
                }
            ];
            
            let glyph_buffer = harfrust_shape_full(
                font,
                buffer,
                std::ptr::null(), // No features
                0,
                variations.as_ptr(),
                variations.len() as u32
            );
            
            assert!(!glyph_buffer.is_null());
            
            // Just verify it produced something
            let len = harfrust_glyph_buffer_len(glyph_buffer);
            assert!(len > 0);
            
            harfrust_glyph_buffer_free(glyph_buffer);
            harfrust_font_free(font);
        }
    }
}

