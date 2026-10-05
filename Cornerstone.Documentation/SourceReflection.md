# Source reflection

Source reflection is Cornerstone’s compile-time stand-in for `System.Reflection`. The point is Native AOT: attributed types must be constructible and their members readable or writable without runtime member lookup.

Mark a type with `[SourceReflection]`, or register a type you do not own with `[assembly: SourceReflectionType<T>]`. The generator emits compiled lambdas (`new T(...)`, get/set, and `UnsafeAccessor` for private constructors). Construction and member access on that path must stay AOT-safe. If a member cannot be emitted as compiled code, skip it rather than calling `GetConstructor`, `Invoke`, or other trim-unsafe APIs from generated SourceReflection.
