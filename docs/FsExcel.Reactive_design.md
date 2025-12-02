# FsExcel.Reactive: A Partas.Solid Extension for Interactive Spreadsheet Demonstrations

## Technical Design Document

### Executive Summary

This document specifies a proposed extension to FsExcel that enables reactive, browser-based spreadsheet demonstrations using Partas.Solid as the rendering framework. The extension bridges FsExcel's declarative spreadsheet DSL with SolidJS's fine-grained reactivity, producing self-contained HTML artifacts that demonstrate functional programming principles through interactive spreadsheet behavior.

The primary use case is live coding demonstrations where presenters can show audiences that F# functions and Excel formulas share identical semantics, with both executing reactively in real-time.

---

## Part I: Understanding the Foundation

### FsExcel's Architecture

FsExcel, created by Kit Eason, provides a declarative DSL for generating Excel spreadsheets from F#. The library builds on ClosedXML and expresses spreadsheet structure through a computation expression that yields cell specifications.

The core abstraction is a sequence of instructions that describe cells, their content, formatting, and navigation:

```fsharp
open FsExcel

let simpleSheet =
    [
        Cell [ String "Revenue" ]
        Cell [ Float 50000.0 ]
        Go NewRow
        Cell [ String "Expenses" ]
        Cell [ Float 35000.0 ]
        Go NewRow
        Cell [ String "Profit" ]
        Cell [ FormulaA1 "=B1-B2" ]
    ]
    |> Render.AsFile "/tmp/financials.xlsx"
```

This instruction stream gets interpreted by ClosedXML to produce an actual `.xlsx` file. The key insight is that FsExcel separates the description of a spreadsheet from its realization. The same description could, in principle, target different outputs.

FsExcel already includes `Render.AsHtml`, which produces a static HTML table representation. However, this rendering is non-reactive; formulas are evaluated once, and the result is baked into the HTML. Changing input values requires regenerating the entire document.

### Why Partas.Solid?

The choice of Partas.Solid for reactive rendering follows from several considerations.

**Fine-grained reactivity without virtual DOM:** When a user changes an input cell, only the dependent cells should update. React would re-render components and diff against a virtual DOM. Svelte compiles reactivity but requires its own file format and build tooling. SolidJS tracks dependencies at the signal level and updates only the specific DOM nodes affected. For a spreadsheet where individual cells update independently, this granularity maps perfectly to the problem domain.

**The Oxpecker DSL style:** Partas.Solid uses computation expressions with constructor parameters for attributes and curly braces for children. This syntax reads naturally to F# developers and resembles HTML closely enough that the structure is immediately apparent:

```fsharp
div(class' = "cell") {
    span(class' = "value") { 
        cellValue() 
    }
}
```

**Type-safe component definition:** The `[<SolidTypeComponent>]` attribute allows defining custom components as types with properties, which the Fable plugin transforms into proper SolidJS components. Cell components can carry typed metadata about their formulas, formats, and dependencies.

**Fable compilation:** Partas.Solid compiles through Fable to JavaScript. The entire reactive spreadsheet can be a self-contained HTML file with embedded JavaScript, requiring no server and working offline. This is ideal for conference presentations where network connectivity is uncertain.

---

## Part II: The Proposed Extension Architecture

### Design Goals

1. **Dual output:** Generate both reactive HTML and static Excel from the same specification
2. **Formula preservation:** Input cells remain editable; formula cells recalculate reactively
3. **Visual parity:** The HTML rendering should resemble a spreadsheet grid
4. **Self-contained artifacts:** Generated HTML files include all necessary JavaScript
5. **Extensibility:** The design should support future enhancements (charts, conditional formatting)

### The Reactive Cell Model

We introduce a discriminated union that captures the reactive nature of cells:

```fsharp
namespace FsExcel.Reactive

open Partas.Solid
open Fable.Core

/// Represents a cell's reactive behavior
type ReactiveCell =
    /// An input cell the user can edit
    | RInput of name: string * initial: float * format: string option
    /// A formula cell that computes from other cells
    | RFormula of expr: string * format: string option
    /// A static label cell
    | RLabel of text: string
    /// A cell displaying a signal value directly
    | RSignal of signal: Accessor<float> * format: string option

/// Represents a complete reactive spreadsheet model
type ReactiveModel = {
    Cells: (int * int * ReactiveCell) list  // row, col, cell
    ColumnWidths: int list option
    Title: string option
}
```

The `RInput` case creates both a signal and an editable UI element. The `RFormula` case parses the expression, identifies dependencies, and creates a derived computation. The `RSignal` case allows direct signal injection for programmatic control.

### Formula Parsing

Excel formula syntax requires parsing to identify cell references and build the dependency graph. We implement a lightweight parser using active patterns:

```fsharp
module FormulaParser =
    
    open System.Text.RegularExpressions
    
    /// Represents a parsed expression
    type Expr =
        | Number of float
        | CellRef of col: int * row: int
        | NamedRef of string
        | BinaryOp of Expr * string * Expr
        | FunctionCall of string * Expr list
        | Parenthesized of Expr
    
    /// Parse a cell reference like "A1" or "BC42"
    let (|CellReference|_|) (s: string) =
        let m = Regex.Match(s, @"^([A-Z]+)(\d+)$")
        if m.Success then
            let colStr = m.Groups.[1].Value
            let row = int m.Groups.[2].Value
            // Convert column letters to zero-based index
            let col = 
                colStr 
                |> Seq.fold (fun acc c -> acc * 26 + (int c - int 'A' + 1)) 0
                |> fun x -> x - 1
            Some (col, row - 1)  // Convert to zero-based
        else None
    
    /// Tokenize a formula string
    let tokenize (formula: string) : string list =
        let pattern = @"([A-Z]+\d+|\d+\.?\d*|[+\-*/^()=,]|\w+)"
        Regex.Matches(formula, pattern)
        |> Seq.cast<Match>
        |> Seq.map (fun m -> m.Value)
        |> Seq.toList
    
    /// Recursive descent parser for formula expressions
    let rec parseExpr tokens =
        parseAdditive tokens
    
    and parseAdditive tokens =
        let left, remaining = parseMultiplicative tokens
        match remaining with
        | "+" :: rest ->
            let right, final = parseAdditive rest
            BinaryOp(left, "+", right), final
        | "-" :: rest ->
            let right, final = parseAdditive rest
            BinaryOp(left, "-", right), final
        | _ -> left, remaining
    
    and parseMultiplicative tokens =
        let left, remaining = parsePower tokens
        match remaining with
        | "*" :: rest ->
            let right, final = parseMultiplicative rest
            BinaryOp(left, "*", right), final
        | "/" :: rest ->
            let right, final = parseMultiplicative rest
            BinaryOp(left, "/", right), final
        | _ -> left, remaining
    
    and parsePower tokens =
        let left, remaining = parsePrimary tokens
        match remaining with
        | "^" :: rest ->
            let right, final = parsePower rest
            BinaryOp(left, "^", right), final
        | _ -> left, remaining
    
    and parsePrimary tokens =
        match tokens with
        | "(" :: rest ->
            let expr, afterExpr = parseExpr rest
            match afterExpr with
            | ")" :: final -> Parenthesized expr, final
            | _ -> failwith "Expected closing parenthesis"
        | CellReference (col, row) :: rest ->
            CellRef(col, row), rest
        | token :: rest when Regex.IsMatch(token, @"^\d+\.?\d*$") ->
            Number(float token), rest
        | name :: "(" :: rest when Regex.IsMatch(name, @"^[A-Z]+$") ->
            // Function call like SUM(...)
            let args, afterArgs = parseArguments rest
            FunctionCall(name, args), afterArgs
        | name :: rest when Regex.IsMatch(name, @"^[a-zA-Z_]\w*$") ->
            NamedRef(name), rest
        | _ -> failwith $"Unexpected token: {tokens}"
    
    and parseArguments tokens =
        // Simplified: parse comma-separated expressions until )
        let rec loop acc tokens =
            let expr, remaining = parseExpr tokens
            match remaining with
            | ")" :: rest -> List.rev (expr :: acc), rest
            | "," :: rest -> loop (expr :: acc) rest
            | _ -> failwith "Expected , or ) in function arguments"
        loop [] tokens
    
    /// Parse a formula string (without leading =)
    let parse (formula: string) : Expr =
        let tokens = tokenize (formula.TrimStart('='))
        let expr, remaining = parseExpr tokens
        if remaining <> [] then
            failwith $"Unexpected tokens remaining: {remaining}"
        expr
    
    /// Extract all cell references from an expression
    let rec extractReferences expr =
        match expr with
        | Number _ -> []
        | CellRef (col, row) -> [(col, row)]
        | NamedRef _ -> []  // Named references resolved separately
        | BinaryOp (left, _, right) ->
            extractReferences left @ extractReferences right
        | FunctionCall (_, args) ->
            args |> List.collect extractReferences
        | Parenthesized inner ->
            extractReferences inner
```

This parser handles the subset of Excel formulas needed for demonstrations: arithmetic operations, cell references, parentheses, and basic functions. A production implementation would extend this significantly, but for presentation purposes, this covers compound interest calculations, summations, and typical financial formulas.

### Signal Generation and Dependency Tracking

The core of the reactive system translates the parsed AST into SolidJS signals and derived computations:

```fsharp
module ReactiveEngine =
    
    open Partas.Solid
    open FormulaParser
    open Fable.Core
    open Fable.Core.JsInterop
    
    /// Registry of named signals for cell references
    type SignalRegistry = {
        mutable CellSignals: Map<(int * int), Accessor<float>>
        mutable NamedSignals: Map<string, Accessor<float>>
        mutable CellSetters: Map<(int * int), float -> unit>
    }
    
    let createRegistry () = {
        CellSignals = Map.empty
        NamedSignals = Map.empty
        CellSetters = Map.empty
    }
    
    /// Create a signal for an input cell
    let createInputSignal (registry: SignalRegistry) (col: int) (row: int) (initial: float) =
        let signal, setSignal = createSignal initial
        registry.CellSignals <- Map.add (col, row) signal registry.CellSignals
        registry.CellSetters <- Map.add (col, row) setSignal registry.CellSetters
        signal, setSignal
    
    /// Register a named signal
    let registerNamed (registry: SignalRegistry) (name: string) (signal: Accessor<float>) =
        registry.NamedSignals <- Map.add name signal registry.NamedSignals
    
    /// Compile an expression into a derived signal
    let rec compileExpr (registry: SignalRegistry) (expr: Expr) : Accessor<float> =
        match expr with
        | Number n -> 
            // Constant: return a getter that always returns n
            fun () -> n
        
        | CellRef (col, row) ->
            // Look up the signal for this cell
            match Map.tryFind (col, row) registry.CellSignals with
            | Some signal -> signal
            | None -> failwith $"Reference to undefined cell ({col}, {row})"
        
        | NamedRef name ->
            match Map.tryFind name registry.NamedSignals with
            | Some signal -> signal
            | None -> failwith $"Reference to undefined name: {name}"
        
        | BinaryOp (left, op, right) ->
            let leftSignal = compileExpr registry left
            let rightSignal = compileExpr registry right
            // Create a derived computation
            let operation =
                match op with
                | "+" -> (+)
                | "-" -> (-)
                | "*" -> (*)
                | "/" -> (/)
                | "^" -> fun a b -> a ** b
                | _ -> failwith $"Unknown operator: {op}"
            // Return a getter that computes on access
            fun () -> operation (leftSignal()) (rightSignal())
        
        | FunctionCall (name, args) ->
            let argSignals = args |> List.map (compileExpr registry)
            match name.ToUpper() with
            | "SUM" ->
                fun () -> argSignals |> List.sumBy (fun s -> s())
            | "AVERAGE" ->
                fun () -> 
                    let values = argSignals |> List.map (fun s -> s())
                    List.sum values / float (List.length values)
            | "MAX" ->
                fun () -> argSignals |> List.map (fun s -> s()) |> List.max
            | "MIN" ->
                fun () -> argSignals |> List.map (fun s -> s()) |> List.min
            | "ABS" ->
                fun () -> abs (argSignals.[0]())
            | "SQRT" ->
                fun () -> sqrt (argSignals.[0]())
            | _ -> failwith $"Unknown function: {name}"
        
        | Parenthesized inner ->
            compileExpr registry inner
    
    /// Create a formula signal from a formula string
    let createFormulaSignal (registry: SignalRegistry) (formula: string) : Accessor<float> =
        let expr = FormulaParser.parse formula
        compileExpr registry expr
```

The `compileExpr` function is the heart of the system. It walks the AST and produces closures that, when invoked, compute the current value by reading from their dependent signals. SolidJS's reactivity system automatically tracks these reads. When an input signal changes, any derived computation that read from it will re-execute.

Notice that we don't explicitly build a dependency graph; SolidJS handles this through its signal subscription mechanism. Each getter function closes over its dependencies, and SolidJS traces which signals are accessed during evaluation.

### The Partas.Solid Component Layer

Now we build the visual components using Partas.Solid's DSL. Each cell type gets a corresponding component:

```fsharp
namespace FsExcel.Reactive.Components

open Partas.Solid
open Fable.Core
open Browser.Types

/// Base styles for spreadsheet cells
module Styles =
    let cellBase = "border border-gray-300 px-2 py-1 text-right font-mono text-sm"
    let inputCell = cellBase + " bg-yellow-50 focus:bg-yellow-100 focus:outline-none"
    let formulaCell = cellBase + " bg-white"
    let labelCell = cellBase + " bg-gray-100 text-left font-sans"
    let headerCell = cellBase + " bg-gray-200 font-semibold text-center"

/// Input cell component: editable, creates a signal
[<Erase>]
type InputCell() =
    inherit td()
    
    member val signal: Accessor<float> = unbox null with get, set
    member val setSignal: (float -> unit) = unbox null with get, set
    member val format: string option = None with get, set
    
    [<SolidTypeComponent>]
    member props.constructor =
        let formatValue (v: float) =
            match props.format with
            | Some fmt -> v.ToString(fmt)
            | None -> v.ToString("F2")
        
        let handleInput (e: Event) =
            let target = e.target :?> HTMLInputElement
            match System.Double.TryParse(target.value) with
            | true, v -> props.setSignal v
            | false, _ -> ()  // Ignore invalid input
        
        td(class' = Styles.inputCell) {
            input(
                type' = "text",
                class' = "w-full bg-transparent text-right outline-none",
                value = formatValue(props.signal()),
                onInput = handleInput
            )
        }

/// Formula cell component: displays computed value
[<Erase>]
type FormulaCell() =
    inherit td()
    
    member val signal: Accessor<float> = unbox null with get, set
    member val format: string option = None with get, set
    member val formula: string = "" with get, set
    
    [<SolidTypeComponent>]
    member props.constructor =
        let formatValue (v: float) =
            match props.format with
            | Some fmt -> v.ToString(fmt)
            | None -> v.ToString("F2")
        
        td(
            class' = Styles.formulaCell,
            title = props.formula  // Show formula on hover
        ) {
            formatValue(props.signal())
        }

/// Label cell component: static text
[<Erase>]
type LabelCell() =
    inherit td()
    
    member val text: string = "" with get, set
    
    [<SolidTypeComponent>]
    member props.constructor =
        td(class' = Styles.labelCell) {
            props.text
        }

/// Header cell component: column/row labels
[<Erase>]
type HeaderCell() =
    inherit th()
    
    member val text: string = "" with get, set
    
    [<SolidTypeComponent>]
    member props.constructor =
        th(class' = Styles.headerCell) {
            props.text
        }
```

These components use Tailwind CSS classes for styling. The `InputCell` creates an input element bound to its signal; typing a new value calls `setSignal`, which propagates through the dependency graph. The `FormulaCell` simply reads its signal and formats the display; SolidJS ensures it updates when dependencies change.

The `title` attribute on `FormulaCell` displays the formula on hover, providing transparency into the calculation, just as Excel shows formulas in the formula bar.

### The Spreadsheet Grid Component

The grid component assembles cells into a table structure:

```fsharp
/// Complete spreadsheet grid component
[<Erase>]
type SpreadsheetGrid() =
    inherit table()
    
    member val rows: int = 0 with get, set
    member val cols: int = 0 with get, set
    member val getCellComponent: (int -> int -> HtmlElement) = unbox null with get, set
    member val showHeaders: bool = true with get, set
    
    [<SolidTypeComponent>]
    member props.constructor =
        let colLetters = 
            [| for i in 0 .. props.cols - 1 -> 
                string (char (int 'A' + i)) |]
        
        table(class' = "border-collapse") {
            // Optional column headers
            Show(when' = props.showHeaders) {
                thead() {
                    tr() {
                        // Empty corner cell
                        HeaderCell(text = "")
                        // Column letters
                        For(each = colLetters) {
                            fun letter _ -> HeaderCell(text = letter)
                        }
                    }
                }
            }
            
            tbody() {
                For(each = [| 0 .. props.rows - 1 |]) {
                    fun row _ ->
                        tr() {
                            // Row number header
                            Show(when' = props.showHeaders) {
                                HeaderCell(text = string (row + 1))
                            }
                            // Data cells
                            For(each = [| 0 .. props.cols - 1 |]) {
                                fun col _ -> props.getCellComponent row col
                            }
                        }
                }
            }
        }
```

The grid accepts a callback function `getCellComponent` that returns the appropriate component for each row/column position. This decouples the grid layout from the cell content, allowing the same grid to render different spreadsheet models.

---

## Part III: The Builder API

### From FsExcel Syntax to Reactive Model

We want users to write spreadsheet definitions in a syntax close to FsExcel's existing DSL, but with reactive semantics. The builder translates these definitions into a `ReactiveModel`:

```fsharp
module FsExcel.Reactive.Builder

open ReactiveEngine

/// Builder for constructing reactive spreadsheets
type ReactiveSpreadsheetBuilder() =
    let mutable cells: (int * int * ReactiveCell) list = []
    let mutable currentRow = 0
    let mutable currentCol = 0
    let mutable registry = createRegistry()
    
    /// Add an input cell at the current position
    member _.Input(name: string, initial: float, ?format: string) =
        let signal, setter = createInputSignal registry currentCol currentRow initial
        registerNamed registry name signal
        cells <- (currentRow, currentCol, RInput(name, initial, format)) :: cells
        currentCol <- currentCol + 1
    
    /// Add a formula cell at the current position
    member _.Formula(expr: string, ?format: string) =
        let signal = createFormulaSignal registry expr
        registry.CellSignals <- Map.add (currentCol, currentRow) signal registry.CellSignals
        cells <- (currentRow, currentCol, RFormula(expr, format)) :: cells
        currentCol <- currentCol + 1
    
    /// Add a label cell at the current position
    member _.Label(text: string) =
        cells <- (currentRow, currentCol, RLabel(text)) :: cells
        currentCol <- currentCol + 1
    
    /// Move to next row
    member _.NewRow() =
        currentRow <- currentRow + 1
        currentCol <- 0
    
    /// Skip columns
    member _.Skip(count: int) =
        currentCol <- currentCol + count
    
    /// Build the reactive model
    member _.Build() : ReactiveModel * SignalRegistry =
        let maxRow = cells |> List.map (fun (r,_,_) -> r) |> List.max
        let maxCol = cells |> List.map (fun (_,c,_) -> c) |> List.max
        { Cells = List.rev cells
          ColumnWidths = None
          Title = None }, registry

/// DSL entry point
let reactiveSheet = ReactiveSpreadsheetBuilder()
```

Usage looks like:

```fsharp
let compoundInterestDemo =
    let builder = ReactiveSpreadsheetBuilder()
    
    builder.Label("Principal ($)")
    builder.Input("principal", 10000.0, format = "N0")
    builder.NewRow()
    
    builder.Label("Annual Rate (%)")
    builder.Input("rate", 7.0, format = "N1")
    builder.NewRow()
    
    builder.Label("Years")
    builder.Input("years", 10.0, format = "N0")
    builder.NewRow()
    
    builder.NewRow()
    builder.Label("Future Value")
    builder.Formula("=principal*(1+rate/100)^years", format = "C2")
    
    builder.Build()
```

This imperative style matches how FsExcel works, making the mental model consistent. The builder tracks position and constructs both the cell list and the signal registry.

### Alternative: Computation Expression Syntax

For a more idiomatic F# experience, we can also provide a computation expression builder:

```fsharp
type ReactiveSheetBuilder() =
    member _.Yield(_) = ()
    
    member _.Zero() = []
    
    member _.Delay(f) = f()
    
    member _.Combine(a, b) = a @ b
    
    [<CustomOperation("input")>]
    member _.InputCell(state, name, initial, ?format) =
        state @ [RInput(name, initial, format)]
    
    [<CustomOperation("formula")>]
    member _.FormulaCell(state, expr, ?format) =
        state @ [RFormula(expr, format)]
    
    [<CustomOperation("label")>]
    member _.LabelCell(state, text) =
        state @ [RLabel(text)]
    
    [<CustomOperation("row")>]
    member _.NewRow(state) =
        // Marker for row break
        state @ [RLabel("\n")]  // Sentinel value
    
    member _.Run(cells) = cells

let rsheet = ReactiveSheetBuilder()
```

Usage:

```fsharp
let demo = rsheet {
    label "Principal ($)"
    input "principal" 10000.0 "N0"
    row
    label "Annual Rate (%)"
    input "rate" 7.0 "N1"
    row
    label "Years"
    input "years" 10.0 "N0"
    row
    row
    label "Future Value"
    formula "=principal*(1+rate/100)^years" "C2"
}
```

Both styles are valid; the choice depends on user preference.

---

## Part IV: Rendering and Output

### Reactive HTML Generation

The final step transforms the model into a Partas.Solid application:

```fsharp
module FsExcel.Reactive.Render

open Partas.Solid
open Components
open ReactiveEngine

/// Create the root application component
[<SolidComponent>]
let SpreadsheetApp (model: ReactiveModel) (registry: SignalRegistry) =
    let maxRow = model.Cells |> List.map (fun (r,_,_) -> r) |> List.max
    let maxCol = model.Cells |> List.map (fun (_,c,_) -> c) |> List.max
    
    let cellMap = 
        model.Cells 
        |> List.map (fun (r, c, cell) -> (r, c), cell) 
        |> Map.ofList
    
    let getCellComponent row col =
        match Map.tryFind (row, col) cellMap with
        | Some (RInput(name, _, format)) ->
            let signal = registry.CellSignals.[(col, row)]
            let setter = registry.CellSetters.[(col, row)]
            InputCell(signal = signal, setSignal = setter, format = format) :> HtmlElement
        
        | Some (RFormula(expr, format)) ->
            let signal = registry.CellSignals.[(col, row)]
            FormulaCell(signal = signal, format = format, formula = expr) :> HtmlElement
        
        | Some (RLabel text) ->
            LabelCell(text = text) :> HtmlElement
        
        | Some (RSignal(signal, format)) ->
            FormulaCell(signal = signal, format = format, formula = "[signal]") :> HtmlElement
        
        | None ->
            td(class' = Styles.cellBase) { "" } :> HtmlElement
    
    div(class' = "p-4 font-sans") {
        h2(class' = "text-xl font-bold mb-4") {
            model.Title |> Option.defaultValue "Reactive Spreadsheet"
        }
        SpreadsheetGrid(
            rows = maxRow + 1,
            cols = maxCol + 1,
            getCellComponent = getCellComponent,
            showHeaders = true
        )
    }

/// Generate self-contained HTML file
let toHtmlFile (path: string) (model: ReactiveModel) (registry: SignalRegistry) =
    let html = $"""
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>{model.Title |> Option.defaultValue "Reactive Spreadsheet"}</title>
    <script src="https://cdn.tailwindcss.com"></script>
</head>
<body>
    <div id="app"></div>
    <script type="module">
        // Generated SolidJS application code inserted here
        {generateJavaScript model registry}
    </script>
</body>
</html>
"""
    System.IO.File.WriteAllText(path, html)
```

The `generateJavaScript` function would emit the compiled Fable output. In practice, the workflow involves:

1. Define the reactive model in F#
2. Fable compiles the Partas.Solid components to JavaScript
3. A post-processing step bundles the output with Solid runtime
4. The result is a single HTML file with embedded JS

For the demonstration, this entire build happens ahead of time. The presenter opens the HTML file in a browser and interacts with it live.

### Dual Output: HTML and Excel

A key feature is generating both reactive HTML and static Excel from the same model:

```fsharp
module FsExcel.Reactive.DualRender

open FsExcel

/// Convert reactive model to FsExcel instructions
let toFsExcel (model: ReactiveModel) : obj list =
    let mutable instructions = []
    let mutable lastRow = 0
    
    for (row, col, cell) in model.Cells |> List.sortBy (fun (r,c,_) -> r, c) do
        // Handle row breaks
        while lastRow < row do
            instructions <- instructions @ [Go NewRow]
            lastRow <- lastRow + 1
        
        // Add cell
        let cellInstr =
            match cell with
            | RInput(_, initial, format) ->
                let props = 
                    [ Float initial
                      BackgroundColor XLColor.LightYellow ] @
                    (format |> Option.map (fun f -> [FormatCode f]) |> Option.defaultValue [])
                Cell props
            
            | RFormula(expr, format) ->
                let props =
                    [ FormulaA1 expr ] @
                    (format |> Option.map (fun f -> [FormatCode f]) |> Option.defaultValue [])
                Cell props
            
            | RLabel text ->
                Cell [ String text; BackgroundColor XLColor.LightGray ]
            
            | RSignal _ ->
                Cell [ String "[computed]" ]
        
        instructions <- instructions @ [cellInstr]
    
    instructions

/// Generate both outputs
let renderBoth (basePath: string) (model: ReactiveModel) (registry: SignalRegistry) =
    // HTML for browser demo
    toHtmlFile (basePath + ".html") model registry
    
    // Excel for distribution
    model 
    |> toFsExcel 
    |> Render.AsFile (basePath + ".xlsx")
```

Now the presenter can say: "Here's the interactive version in your browser; here's the Excel file you can download and open in Excel; both express the same functional semantics."

---

## Part V: The Complete Demo Workflow

### Project Structure

```
FsExcel.Reactive/
├── src/
│   ├── FsExcel.Reactive/
│   │   ├── FormulaParser.fs       # Excel formula parsing
│   │   ├── ReactiveEngine.fs      # Signal generation
│   │   ├── Model.fs               # Core types
│   │   └── Builder.fs             # DSL builders
│   │
│   └── FsExcel.Reactive.Solid/
│       ├── Components.fs          # Partas.Solid cell components
│       ├── Grid.fs                # Spreadsheet grid component
│       └── Render.fs              # HTML generation
│
├── demos/
│   ├── CompoundInterest.fs        # Financial demo
│   ├── MortgageCalculator.fs      # More complex formulas
│   └── App.fs                     # Demo entry point
│
├── package.json                   # npm dependencies (solid-js, etc.)
├── vite.config.js                 # Build configuration
└── FsExcel.Reactive.fsproj        # F# project file
```

### The Compound Interest Demo

This is the live coding target for the presentation:

```fsharp
module Demos.CompoundInterest

open FsExcel.Reactive
open FsExcel.Reactive.Builder

/// Build the compound interest demonstration
let build () =
    let builder = ReactiveSpreadsheetBuilder()
    
    // Title area
    builder.Label("Compound Interest Calculator")
    builder.Skip(2)
    builder.NewRow()
    builder.NewRow()
    
    // Input section
    builder.Label("Principal ($)")
    builder.Input("principal", 10000.0, format = "N0")
    builder.NewRow()
    
    builder.Label("Annual Rate (%)")
    builder.Input("rate", 7.0, format = "N2")
    builder.NewRow()
    
    builder.Label("Years")
    builder.Input("years", 10.0, format = "N0")
    builder.NewRow()
    builder.NewRow()
    
    // Result section
    builder.Label("Future Value")
    builder.Formula("=principal*(1+rate/100)^years", format = "C2")
    builder.NewRow()
    builder.NewRow()
    
    // Year-by-year breakdown
    builder.Label("Year")
    builder.Label("Balance")
    builder.Label("Interest Earned")
    builder.NewRow()
    
    // Generate rows for years 1-10
    for year in 1 .. 10 do
        builder.Label(string year)
        builder.Formula($"=principal*(1+rate/100)^{year}", format = "C2")
        if year = 1 then
            builder.Formula($"=principal*(1+rate/100)^{year}-principal", format = "C2")
        else
            builder.Formula($"=principal*(1+rate/100)^{year}-principal*(1+rate/100)^{year-1}", format = "C2")
        builder.NewRow()
    
    builder.Build()

/// Pure F# equivalent functions for side-by-side display
module FSharpEquivalent =
    
    let futureValue principal rate years =
        principal * (1.0 + rate / 100.0) ** float years
    
    let yearlyBreakdown principal rate years =
        [ for year in 1 .. years ->
            let balance = futureValue principal rate year
            let prevBalance = if year = 1 then principal else futureValue principal rate (year - 1)
            let interest = balance - prevBalance
            {| Year = year; Balance = balance; Interest = interest |} ]
```

### Presentation Script

During the demo, the presenter:

1. **Shows the F# function:**
   ```fsharp
   let futureValue principal rate years =
       principal * (1.0 + rate / 100.0) ** float years
   ```

2. **Shows the Excel formula:**
   ```
   =principal*(1+rate/100)^years
   ```

3. **Points out:** "Same computation. Same semantics. The only difference is notation."

4. **Opens the reactive HTML demo in browser.**

5. **Changes Principal from 10,000 to 50,000:** All dependent cells update instantly.

6. **Changes Rate from 7% to 12%:** The entire projection recalculates.

7. **Explains:** "This is functional reactive programming. Pure functions, dependency-tracked evaluation, no side effects. It's exactly what Excel does, expressed in F#."

8. **Opens the generated Excel file:** Shows that the same formulas work identically in actual Excel.

9. **Summarizes:** "Your CFO trusts this model because Excel enforces functional principles. We can build systems with the same guarantees, at scale."

---

## Part VI: Implementation Considerations

### Fable and Partas.Solid Setup

The project requires Fable 5 and specific configuration:

```xml
<!-- FsExcel.Reactive.Solid.fsproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
  
  <ItemGroup>
    <PackageReference Include="Fable.Core" Version="4.3.0" />
    <PackageReference Include="Partas.Solid" Version="2.1.3" />
    <PackageReference Include="Partas.Solid.FablePlugin" Version="1.2.1" />
  </ItemGroup>
  
  <ItemGroup>
    <Compile Include="FormulaParser.fs" />
    <Compile Include="ReactiveEngine.fs" />
    <Compile Include="Components.fs" />
    <Compile Include="Grid.fs" />
    <Compile Include="Render.fs" />
  </ItemGroup>
</Project>
```

Fable configuration:

```json
// fable.config.json
{
  "outDir": "./output",
  "extension": ".fs.jsx",
  "exclude": ["Partas.Solid.FablePlugin"]
}
```

The `--exclude` flag is critical; the plugin must not be compiled, only used for transformation.

### Build Pipeline

```bash
# Install dependencies
dotnet restore
npm install

# Fable compilation (release mode required for Partas.Solid)
dotnet fable --configuration Release -o output -e .fs.jsx

# Bundle with Vite
npx vite build

# Output: dist/index.html (self-contained demo)
```

### Testing the Formula Parser

The parser should be tested independently:

```fsharp
module Tests.FormulaParser

open Expecto
open FsExcel.Reactive.FormulaParser

let tests = testList "FormulaParser" [
    test "parses simple cell reference" {
        let expr = parse "A1"
        Expect.equal expr (CellRef(0, 0)) "A1 should parse to (0,0)"
    }
    
    test "parses multi-letter column" {
        let expr = parse "AA1"
        Expect.equal expr (CellRef(26, 0)) "AA1 should parse to (26,0)"
    }
    
    test "parses arithmetic" {
        let expr = parse "A1+B1"
        match expr with
        | BinaryOp(CellRef(0,0), "+", CellRef(1,0)) -> ()
        | _ -> failtest "Expected A1+B1 pattern"
    }
    
    test "parses compound interest formula" {
        let expr = parse "principal*(1+rate/100)^years"
        // Should parse without error
        Expect.isTrue (extractReferences expr |> List.isEmpty) "Named refs have no cell refs"
    }
    
    test "handles operator precedence" {
        let expr = parse "1+2*3"
        // Should be 1+(2*3), not (1+2)*3
        match expr with
        | BinaryOp(Number 1.0, "+", BinaryOp(Number 2.0, "*", Number 3.0)) -> ()
        | _ -> failtest "Expected correct precedence"
    }
]
```

### Error Handling

Formula errors should be displayed gracefully:

```fsharp
[<Erase>]
type ErrorCell() =
    inherit td()
    
    member val message: string = "" with get, set
    
    [<SolidTypeComponent>]
    member props.constructor =
        td(
            class' = Styles.cellBase + " bg-red-100 text-red-700",
            title = props.message
        ) {
            "#ERROR"
        }
```

The engine wraps formula compilation in try/catch and substitutes `ErrorCell` when parsing fails.

---

## Part VII: Future Extensions

### Conditional Formatting

Add support for cell styling based on values:

```fsharp
type ConditionalFormat = {
    Condition: Accessor<float> -> bool
    TrueClass: string
    FalseClass: string
}

member val conditionalFormat: ConditionalFormat option = None with get, set
```

### Charts

Integrate with a charting library (Chart.js or similar):

```fsharp
[<Erase>]
type ChartCell() =
    inherit td()
    
    member val chartType: string = "line" with get, set
    member val dataSignals: Accessor<float> list = [] with get, set
```

### Range References

Extend the parser to handle A1:A10 range syntax:

```fsharp
| RangeRef of startCol: int * startRow: int * endCol: int * endRow: int
```

### Named Ranges

Allow defining named ranges that can be referenced in formulas:

```fsharp
builder.DefineRange("Sales", (0, 1), (0, 12))
builder.Formula("=SUM(Sales)")
```

---

## Conclusion

FsExcel.Reactive bridges the gap between static spreadsheet generation and interactive demonstration. By layering Partas.Solid's fine-grained reactivity on FsExcel's declarative model, we create artifacts that demonstrate functional programming principles through the familiar lens of spreadsheet interaction.

The extension serves dual purposes: it's a practical tool for conference presentations, and it's a conceptual proof that Excel's trusted semantics can be expressed directly in typed functional code. When audiences see F# functions and Excel formulas producing identical reactive behavior, the abstraction becomes concrete.

The architecture is designed for contribution back to the FsExcel project. The core abstractions (ReactiveCell, SignalRegistry, formula parsing) are independent of Partas.Solid and could support alternative rendering backends. The Partas.Solid integration is one implementation of a pluggable rendering strategy.

For presentation purposes, the flywheel effect is powerful: slides assert that Excel is functional programming; the demo proves it by showing the same formulas executing reactively in F# and Excel simultaneously. Theory and practice reinforce each other, making the argument memorable and actionable.