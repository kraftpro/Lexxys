 # Lexxys.Testing

 Lightweight test and data‑generation utilities used across the Lexxys suite.

 This project provides deterministic and convenient random data generators, helpers, and small utilities
 that simplify writing unit tests, fuzz tests and sample data generators. It is intentionally small,
 dependency‑free and multi‑targeted so it can be consumed from legacy and modern test projects.

 ## Highlights

 - `Rand` — central random generator abstraction with pluggable implementations and deterministic seeding.
 - `RandItem<T>` — a small wrapper for value generators with a uniform `NextValue()` API.
 - `R` — a static collection of ready‑to‑use generators (digits, letters, ASCII, structured strings, pictures, etc.).
 - Utilities for loading embedded resources and composing generators for test data builders.

 ## Quick examples

 ```csharp
 // Make generation deterministic for repeatable tests
 Rand.Reset(42);

 // Generate a random integer in [0,100)
 int n = Rand.Int(0, 100);

 // Generate a lowercase string between 5 and 10 characters
 var generator = R.Lower(5, 10);
 string s = generator.NextValue();

 // Generate a 6-digit verification code
 string code = R.Digit(6).NextValue();

 // Use character generators directly
 char c = R.LetterChar.NextValue();
 ```

 ## API overview

 - `Rand` — static entrypoint. Methods like `Int`, `Bool`, `Bytes`, `Dec`, `Reset` and `Reset(IRand)` to plug custom RNG.
 - `RandItem<T>` — instances wrap a generator lambda; call `NextValue()` to get a new value.
 - `R` — provides a comprehensive set of predefined generators:
   - `R.Digit`, `R.DigitChar` — numeric strings and characters
   - `R.Lower`, `R.Upper`, `R.Letter`, `R.LetterOrDigit` — alphabetic and alphanumeric generators
   - `R.Ascii`, `R.AsciiChar` — printable ASCII content
   - `R.Str` — compose arbitrary character generators into strings
   - Additional helpers in `R.Text`, `R.Picture`, `R.LoadFile` for richer test data

 Refer to the XML documentation within source files for method signatures and behavior details.

 ## Best practices

 - Seed the generator in tests with `Rand.Reset(seed)` when reproducibility is required.
 - Prefer `RandItem<T>` generators for composing structured test data.
 - Add unit tests for any new generator to ensure deterministic behaviour across frameworks.

 ## Target frameworks

 - .NET 10
 - .NET 8
 - .NET Standard 2.0
 - .NET Framework 4.6.2

 ## Contributing

 Contributions are welcome. When adding new generators:

 1. Keep the API consistent with the rest of `R.*` helpers.
 2. Add deterministic unit tests under `test/`.
 3. Run the solution tests across target frameworks if possible.

 ## License

 The project is licensed under the MIT License. See the repository `LICENSE` file for details.
