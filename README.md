# FlexGrid

**A Demonstration of Reactive Functional Programming in Spreadsheets**

<!-- TODO: Add presentation video link when available -->

## The Insight

Over 1.5 billion people use spreadsheets daily. Every one of them is doing functional programming. They just don't use programming jargon.

When a finance professional writes `=B1*(1+B2/100)^B3` to calculate compound interest, they've created a pure function: same inputs produce same outputs, no side effects, automatic dependency tracking. This is referential transparency. This is declarative programming. This is what functional programmers have advocated for decades.

FlexGrid makes this connection concrete. It renders reactive spreadsheets in the browser using F# and SolidJS, showing that the F# function:

```fsharp
let futureValue principal rate years =
    principal * (1.0 + rate / 100.0) ** float years
```

...and the spreadsheet formula:

```
=principal*(1+rate/100)^years
```

...are the same computation with different notation.

## Why This Matters

Spreadsheets succeeded *because* of functional language design. The properties that make spreadsheet models trustworthy: predictable results, auditable calculation chains, no hidden state are precisely the properties that functional programming provides.

When advocating for functional programming in your organization, you're not asking leadership to trust something foreign. You're asking them to recognize that the principles they already trust in their spreadsheets apply equally to the systems that run their business.

## How It Works

FlexGrid demonstrates the functional nature of spreadsheet computation through several mechanisms:

### Formula Parser

The [FormulaParser](src/FlexGrid/FormulaParser.fs) implements a recursive descent parser for Excel-style formulas. It handles:

- Cell references (`A1`, `B2`, `AA10`)
- Named references (`principal`, `rate`, `years`)
- Arithmetic with proper precedence (`+`, `-`, `*`, `/`, `^`)
- Comparison operators (`<`, `>`, `<=`, `>=`, `<>`)
- Function calls (`SUM`, `AVERAGE`, `IF`, `PMT`, etc.)

The parser produces an AST that explicitly represents the formula's structure—the same structure that spreadsheet engines use internally.

### Reactive Engine

The [ReactiveEngine](src/FlexGrid/ReactiveEngine.fs) compiles formula ASTs into signal-based computations. Each formula becomes a function that:

1. Reads values from dependent signals
2. Computes the result
3. Automatically re-executes when dependencies change

This is functional reactive programming—the same model that powers every spreadsheet's recalculation engine. When cell A1 changes, only cells that depend on A1 recompute. The dependency graph determines evaluation order automatically.

### Signal Registry

Input cells create signals (reactive state containers). Formula cells create derived computations that read from those signals. The registry tracks:

- Cell signals by position (`(col, row) -> Accessor<float>`)
- Named signals by identifier (`string -> Accessor<float>`)
- Setters for input cells

This mirrors how spreadsheet engines maintain their internal state—a graph of values and the formulas that connect them.

### Partas.Solid Components

The [FlexGrid.Solid](src/FlexGrid.Solid/) library provides UI components that render the reactive model:

- **InputCell**: Editable cells bound to signals. Type a new value, and dependent cells update instantly.
- **FormulaCell**: Read-only cells displaying computed values. Hover to see the underlying formula.
- **SpreadsheetGrid**: The table structure with optional Excel-style headers (A, B, C... and 1, 2, 3...).

SolidJS's fine-grained reactivity means only the specific DOM nodes for changed cells update—no virtual DOM diffing, no unnecessary re-renders. This is the same optimization strategy that spreadsheet engines use.

### Dual Rendering

The same `ReactiveModel` can render to:

- **Interactive HTML** via Partas.Solid (for live demonstrations)
- **Excel files** via FsExcel/ClosedXML (for distribution)

This shows the semantic equivalence: the model is the computation; the rendering is just presentation.

## Project Structure

```
FlexGrid/
├── src/
│   ├── FlexGrid/                 # Core library (no UI dependencies)
│   │   ├── Model.fs              # ReactiveCell, ReactiveModel types
│   │   ├── FormulaParser.fs      # Excel formula parser
│   │   ├── ReactiveEngine.fs     # Signal-based computation
│   │   ├── Builder.fs            # DSL for constructing spreadsheets
│   │   └── DualRender.fs         # Excel file generation
│   │
│   └── FlexGrid.Solid/           # Browser UI components
│       ├── Components.fs         # InputCell, FormulaCell, etc.
│       ├── Grid.fs               # SpreadsheetGrid component
│       └── Render.fs             # Model-to-component transformation
│
├── demos/                        # Example applications
│   ├── CompoundInterest.fs       # Future value calculator
│   ├── MortgageCalculator.fs     # Loan payment calculator
│   └── App.fs                    # Demo application entry point
│
└── tests/                        # Unit tests
    ├── FormulaParserTests.fs     # Parser correctness
    └── ReactiveEngineTests.fs    # Evaluation correctness
```

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/) (v18+)
- [Fable](https://fable.io/) (`dotnet tool install -g fable`)

### Installation

```bash
# Clone the repository
git clone https://github.com/yourusername/FlexGrid.git
cd FlexGrid

# Restore .NET packages
dotnet restore

# Install npm dependencies
npm install
```

### Development

```bash
# Run tests
dotnet test

# Start development server (compiles F# to JS and serves with hot reload)
npm run dev
```

### Building for Production

```bash
# Build optimized output
npm run build

# Preview production build
npm run preview
```

## Defining Spreadsheets

FlexGrid provides a builder DSL for defining reactive spreadsheets:

```fsharp
open FlexGrid

let model =
    let builder = reactiveSheet()

    builder.Label("Principal")
    builder.Input("principal", 10000.0, format = "N0")
    builder.NewRow()

    builder.Label("Rate (%)")
    builder.Input("rate", 7.0, format = "N2")
    builder.NewRow()

    builder.Label("Years")
    builder.Input("years", 10.0, format = "N0")
    builder.NewRow()

    builder.Label("Future Value")
    builder.Formula("=principal*(1+rate/100)^years", format = "C2")

    builder.Build()
```

The formula syntax matches Excel. Named references (`principal`, `rate`, `years`) bind to input cells. Change any input, and the formula recalculates automatically.

## Supported Functions

The formula engine supports common spreadsheet functions:

| Category | Functions |
|----------|-----------|
| Aggregation | `SUM`, `AVERAGE`, `MAX`, `MIN` |
| Math | `ABS`, `SQRT`, `ROUND`, `FLOOR`, `CEILING`, `POWER`, `LOG`, `LN`, `EXP` |
| Logic | `IF` |
| Financial | `PMT`, `FV`, `PV` |

Additional functions can be added to [ReactiveEngine.fs](src/FlexGrid/ReactiveEngine.fs).

## Dependencies

- **[FsExcel](https://github.com/misterspeedy/FsExcel)** - Declarative Excel generation
- **[Partas.Solid](https://github.com/shayanhabibi/Partas.Solid)** - F# bindings for SolidJS
- **[Fable](https://fable.io/)** - F# to JavaScript compiler
- **[SolidJS](https://www.solidjs.com/)** - Fine-grained reactive UI library
- **[Vite](https://vitejs.dev/)** - Build tooling
- **[Tailwind CSS](https://tailwindcss.com/)** - Styling

## Contributing

Contributions are welcome! See [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

Areas where contributions would be particularly valuable:

- Additional spreadsheet functions (especially financial and statistical)
- Range reference support (`A1:A10`)
- Conditional formatting
- Chart integration
- Additional demo spreadsheets
- Documentation improvements

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE) for details.

## Acknowledgments

- **Simon Peyton Jones** for the insight that "Excel is the world's most widely used functional programming language"
- **Kit Eason** for [FsExcel](https://github.com/misterspeedy/FsExcel)
- **Shayan Habibi** for [Partas.Solid](https://github.com/shayanhabibi/Partas.Solid)
- The 1.5+ billion spreadsheet users who prove daily that functional programming dominates business return-of-value
