# Token text filter

`TokenTextFilter` is a small, stateless helper for loose list lookups. Put it on `PresentationList.FilterCheck` (or any other predicate) when a filter box should match **several words across several fields**.

It is not a service and does not need dependency injection. There is no clock, lifecycle, or host state.

## Rules

- Empty or whitespace-only filter text matches everything.
- The filter is split on whitespace. Every token must match.
- A token matches if it is a case-insensitive **substring** of **any** haystack you pass (name, aisle, note, and so on).
- Tokens can hit different fields. `honey pie` matches a row whose name is Honeycrisp Apples and whose note is for pie.

`wheat bakery` matches **Whole Wheat Bread** in Bakery. The same query would fail a single-substring `IndexOf` on the whole filter string.

## Usage

```csharp
FilterCheck = item => TokenTextFilter.Matches(FilterText, item.Name, item.Aisle, item.Note);
```

Overloads cover one, two, or three haystacks. For more fields, pass `IReadOnlyList<string>`.

When `FilterText` changes, call `RefreshFilter()` on the list.

## What it is not

- Not structured query syntax (`channel:`, `type:`). Bus history keeps its own parser.
- Not fuzzy / edit-distance matching.
- Not punctuation tokenization. `honey-pie` is one token until you split it yourself.

## Sample

The Sample app tab **Token Text Filter** shows a filter box over a grocery list. Type `honey pie` or `wheat bakery` to see the AND-across-fields behavior.
