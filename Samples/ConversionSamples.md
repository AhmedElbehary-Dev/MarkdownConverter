# Conversion Validation Samples

Use these markdown snippets when exercising the new pipeline to confirm the renders keep semantic styling for headings, inline formatting, lists, blockquotes, code, and tables (PDF/Word use styled elements, Excel outputs tables + notes).

## Sample 1 — Feature-rich English snippet
```
# Heading Level 1
## Heading Level 2
Some paragraph text with **bold**, *italic*, and `inline code` alongside a [link](https://example.com) for the hyperlink test.

- Bullet list item
  - Nested bullet
- [ ] Task list entry (should render as unchecked)
1. First numbered item
2. Second numbered item

> Blockquote keeps the muted text + left accent bar styling.

```csharp
Console.WriteLine("Code block should be monospace with padding and background.");
```

| Header | Description |
| ------ | ----------- |
| Alpha | First row |
| Beta  | Second row with **inline bold** |
```

## Sample 2 — Mixed English and Arabic content
```
### Section مختلط
هذا الفقرة تحتوي على **نص غامق** و*نص مائل* و`رمز مضمن`.

> اقتباس مع نص عربي يؤكد الحدود واللون المستعمل.

| العمود الأول | العمود الثاني |
| ------------ | ------------ |
| قيمة ١       | قيمة ٢       |
| سطر إضافي    | مثال        |
```

## Sample 3 — Table-heavy and notes for Excel verification
```
#### Inventory Table
| Product | Qty | Status |
| ------- | --- | ------ |
| Widget  | 24  | ✅ In stock |
| Gizmo   | 0   | ⚠️ Backorder |

Paragraph paragraphs can later appear in the Notes sheet when Excel exports the rest of the markdown.
```

