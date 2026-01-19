fn main() {
    csbindgen::Builder::default()
        .input_extern_file("src/lib.rs")
        .csharp_dll_name("harfrust_ffi")
        .csharp_namespace("HarfRust.Bindings")
        .csharp_class_name("NativeMethods")
        .generate_csharp_file("../net/HarfRust/Bindings/NativeMethods.g.cs")
        .unwrap();
}
