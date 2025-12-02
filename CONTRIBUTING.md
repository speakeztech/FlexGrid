# Contributing to FlexGrid

Thank you for your interest in contributing to FlexGrid! This project demonstrates the connection between spreadsheet computation and functional programming, and contributions that strengthen that demonstration are welcome.

## Getting Started

1. Fork the repository
2. Clone your fork: `git clone https://github.com/yourusername/FlexGrid.git`
3. Create a branch for your work: `git checkout -b feature/your-feature-name`
4. Install dependencies:
   ```bash
   dotnet restore
   npm install
   ```
5. Run tests to ensure everything works: `dotnet test`

## Development Workflow

### Running the Development Server

```bash
npm run dev
```

This compiles the F# code to JavaScript via Fable and starts a Vite development server with hot reload.

### Running Tests

```bash
dotnet test
```

All tests should pass before submitting a pull request.

### Building for Production

```bash
npm run build
```

## Project Structure

Understanding the project structure helps you find where to make changes:

- **`src/FlexGrid/`** - Core library with no UI dependencies
  - `Model.fs` - Type definitions
  - `FormulaParser.fs` - Excel formula parsing
  - `ReactiveEngine.fs` - Signal-based computation
  - `Builder.fs` - DSL for constructing spreadsheets
  - `DualRender.fs` - Excel file output

- **`src/FlexGrid.Solid/`** - Partas.Solid UI components
  - `Components.fs` - Cell components
  - `Grid.fs` - Grid layout
  - `Render.fs` - Model rendering

- **`demos/`** - Example applications
- **`tests/`** - Unit tests

## Contribution Guidelines

### Code Style

- Follow existing F# conventions in the codebase
- Use descriptive names for functions and types
- Add XML documentation comments for public APIs
- Keep functions small and focused

### Adding New Formula Functions

To add a new spreadsheet function:

1. Add the function implementation in `src/FlexGrid/ReactiveEngine.fs` in the `compileExpr` function's `FunctionCall` match case
2. Add tests in `tests/ReactiveEngineTests.fs`
3. Update the README's function table if it's a commonly used function

Example:

```fsharp
| "MEDIAN" ->
    fun () ->
        let values = argSignals |> List.map (fun s -> s()) |> List.sort
        let n = List.length values
        if n = 0 then 0.0
        elif n % 2 = 1 then values.[n / 2]
        else (values.[n / 2 - 1] + values.[n / 2]) / 2.0
```

### Adding New Demo Spreadsheets

Demo spreadsheets should:

1. Illustrate a real-world use case
2. Use multiple input cells and formula cells
3. Demonstrate dependency chains
4. Include the equivalent F# functions for comparison

Place new demos in the `demos/` directory and add them to the demo selector in `App.fs`.

### Parser Enhancements

If extending the formula parser:

1. Add new token patterns to `tokenize` in `FormulaParser.fs`
2. Add parsing logic in the appropriate precedence level
3. Add comprehensive tests in `FormulaParserTests.fs`
4. Ensure error messages are clear for invalid input

## Pull Request Process

1. Ensure all tests pass
2. Update documentation if needed
3. Write a clear PR description explaining:
   - What the change does
   - Why it's valuable
   - Any breaking changes
4. Link any related issues

## Reporting Issues

When reporting issues, please include:

- A clear description of the problem
- Steps to reproduce
- Expected vs actual behavior
- Browser/environment information if relevant
- Any error messages

## Feature Requests

Feature requests are welcome! Please describe:

- The use case for the feature
- How it relates to demonstrating functional programming concepts
- Any implementation ideas you have

## Questions?

If you have questions about contributing, open a discussion issue. We're happy to help!

## License

By contributing to FlexGrid, you agree that your contributions will be licensed under the MIT License.
