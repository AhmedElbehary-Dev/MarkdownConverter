# Baselines (Scaffold)

This folder is reserved for reference outputs used to validate parity during migration.

Populate on a Windows baseline capture machine before cross-platform parity sign-off:

- `samples/ConversionSamples.pdf`
- `samples/ConversionSamples.docx`
- `samples/ConversionSamples.xlsx`
- Additional fixture outputs for the files in `tests/Fixtures/`

Metadata to record for each capture:

- OS version
- .NET SDK version
- Package versions
- wkhtmltopdf runtime version (`0.12.5`)

Note: binary baselines are intentionally not committed yet in this scaffold.
