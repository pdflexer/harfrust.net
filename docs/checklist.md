
## Testing Checklist

For each phase, ensure:

- [ ] Rust unit tests with real font data pass
- [ ] Null pointer safety tests pass
- [ ] .NET wrapper builds for all target frameworks
- [ ] .NET unit tests pass
- [ ] Memory is properly freed (no leaks)
- [ ] Double-dispose is safe
- [ ] Consumed objects throw on reuse
