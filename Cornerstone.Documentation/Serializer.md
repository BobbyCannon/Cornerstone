# JSON Serializer

`Cornerstone.Serialization.Serializer` is a thin façade over **System.Text.Json** with project-wide defaults (camelCase, compact output, shared converters and type-info resolvers).

## Defaults

| Call | Behavior |
|------|----------|
| `value.ToJson()` | Compact JSON, camelCase property names |
| `json.FromJson<T>()` | Deserialize with the same global options |
| `Serializer.SerializationOptions` | The single shared bag — do **not** mutate for one-offs |

Register app-specific source-gen contexts at startup **before** the first `ToJson` / `FromJson` (and before caching `CreateOptions` results):

```csharp
Serializer.AddTypeInfoResolvers(MyAppSerializerContext.Default);
```

After the global options have been used (or `Lock()`), the resolver chain cannot be modified.

## When you need different settings

Indentation, snake_case, PascalCase, extra converters, and similar knobs require a **fork** of the options bag. System.Text.Json caches contract metadata per `JsonSerializerOptions` instance, so the efficient pattern is:

1. **Clone** the global defaults once.
2. **Change only** the settings you care about.
3. **Reuse** that instance (static field) on hot paths.

You do **not** need to re-list every default by hand.

### `CreateOptions`

```csharp
var options = Serializer.CreateOptions(o =>
{
    o.WriteIndented = true;
    o.PropertyNamingPolicy = null;       // PascalCase = CLR property names
    o.DictionaryKeyPolicy = null;
});

var json = value.ToJson(options);
var again = json.FromJson<MyType>(options);
```

Other common deltas:

```csharp
// Snake case (API-style)
Serializer.CreateOptions(o =>
{
    o.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
    o.DictionaryKeyPolicy = JsonNamingPolicy.SnakeCaseLower;
});

// Indent only (keep global camelCase)
Serializer.CreateOptions(o => o.WriteIndented = true);
```

### Efficiency

| Pattern | Use when |
|---------|----------|
| `static readonly` options from `CreateOptions` | Same shape used often (HTTP responses, repeated saves) |
| `CreateOptions` at the call site | Rare / interactive (e.g. one Save click) |
| Mutating `SerializationOptions` | **Never** for one-offs — races other callers and breaks after `Lock()` |

```csharp
// Good: one clone for the app lifetime
private static readonly JsonSerializerOptions IconCacheJsonOptions = Serializer.CreateOptions(o =>
{
    o.WriteIndented = true;
    o.PropertyNamingPolicy = null;
    o.DictionaryKeyPolicy = null;
});

// Avoid on hot paths: rebuilds options + warms metadata every time
return value.ToJson(Serializer.CreateOptions(o => o.WriteIndented = true));
```

Call `CreateOptions` **after** `AddTypeInfoResolvers` so the clone includes app resolvers.

### Writing files

Prefer streaming for large payloads (e.g. icon caches with long geometry strings):

```csharp
Serializer.ToJsonFile(path, value, IconCacheJsonOptions);
// optional: force indent on the writer even if options.WriteIndented is false
Serializer.ToJsonFile(path, value, options, indented: true);
```

`ToJsonFile` uses `Utf8JsonWriter` and avoids building a giant intermediate `string` before encoding to disk.

### ASP.NET Core

To align host JSON with Cornerstone defaults:

```csharp
.AddJsonOptions(x => Serializer.ApplyOptions(x.JsonSerializerOptions));
```

For a **different** host policy (e.g. indented diagnostics only), build a fork with `CreateOptions` and assign fields onto the host options, or use a dedicated options instance for that pipeline.

## Source-generated contexts and naming

If a type is served by a source-generated `JsonSerializerContext` in the resolver chain, property names may be fixed at generation time. Changing `PropertyNamingPolicy` on a cloned options instance might **not** rename those properties.

When casing must be exact:

- Prefer a plain DTO resolved via reflection / `DefaultJsonTypeInfoResolver`, or
- Declare a context with `[JsonSourceGenerationOptions(PropertyNamingPolicy = ...)]`, or
- Use `[JsonPropertyName]` on the model.

## Example: BecomeEpic.Icons project cache

Caches under `Caches/{Project}.json` are human-edited in git. Save uses a **static** fork: indented + PascalCase, written with `ToJsonFile`.

## Summary

| Goal | Approach |
|------|----------|
| Normal app JSON | `ToJson()` / `FromJson<T>()` |
| One-off indent / naming / converters | `CreateOptions` → cache if hot → `ToJson(value, options)` |
| Large file | `ToJsonFile` |
| Host alignment | `ApplyOptions` |
| Global default change | Edit static constructor of `Serializer` only |