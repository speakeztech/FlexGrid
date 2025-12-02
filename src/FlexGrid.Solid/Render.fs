namespace Partas.Solid.FlexGrid

open Fable.Core
open Fable.Core.JsInterop
open global.Partas.Solid
open global.FlexGrid

/// Internal utilities for building reactive signal graphs from spreadsheet models
[<AutoOpen>]
module internal SignalBuilder =

    /// Import SolidJS createSignal
    [<Import("createSignal", "solid-js")>]
    let createSignal<'T> (initialValue: 'T) : Accessor<'T> * ('T -> unit) = jsNative

    /// Build the signal registry from a reactive model
    let buildRegistry (model: ReactiveModel) : SignalRegistry =
        let registry = SignalRegistry.create()

        // First pass: create signals for all input cells
        for posCell in model.Cells do
            match posCell.Cell with
            | ReactiveCell.Input(name, initial, _) ->
                let signal, setter = createSignal initial
                let col, row = posCell.Position.Col, posCell.Position.Row
                SignalRegistry.registerCellSignal col row signal registry
                SignalRegistry.registerCellSetter col row setter registry
                SignalRegistry.registerNamedSignal name signal registry
            | _ -> ()

        // Second pass: create derived signals for formula cells with logging
        for posCell in model.Cells do
            match posCell.Cell with
            | ReactiveCell.Formula(expr, _) ->
                let col, row = posCell.Position.Col, posCell.Position.Row
                let formulaSignal = ReactiveEngine.createFormulaSignalAt registry col row expr
                SignalRegistry.registerCellSignal col row formulaSignal registry
                SignalRegistry.registerCellFormula col row expr registry
            | _ -> ()

        registry

/// Discriminated union representing the data needed to render a cell
type CellData =
    | InputData of signal: Accessor<float> * setter: (float -> unit) * format: string option * name: string * col: int * row: int
    | FormulaData of signal: Accessor<float> * format: string option * formula: string
    | LabelData of text: string
    | EmptyData

/// Internal utilities for resolving cell data from model and registry
[<AutoOpen>]
module internal CellResolver =

    /// Get the cell data for a given position
    let getCellData
        (model: ReactiveModel)
        (registry: SignalRegistry)
        (row: int)
        (col: int) : CellData =

        let maybeCell = ReactiveModel.tryGetCell row col model

        match maybeCell with
        | Some (ReactiveCell.Input(name, _, format)) ->
            match SignalRegistry.tryGetCellSignal col row registry,
                  SignalRegistry.tryGetCellSetter col row registry with
            | Some signal, Some setter ->
                InputData(signal, setter, format, name, col, row)
            | _ ->
                EmptyData

        | Some (ReactiveCell.Formula(expr, format)) ->
            match SignalRegistry.tryGetCellSignal col row registry with
            | Some signal ->
                FormulaData(signal, format, expr)
            | None ->
                EmptyData

        | Some (ReactiveCell.Label text) ->
            LabelData text

        | Some ReactiveCell.Empty ->
            EmptyData

        | None ->
            EmptyData

/// Cell renderer component - renders the appropriate cell type based on CellData
[<Erase>]
type CellRenderer() =
    inherit td()

    [<Erase>]
    member val cellData: CellData = EmptyData with get, set

    [<SolidTypeComponent>]
    member props.constructor =
        match props.cellData with
        | InputData(signal, setter, format, name, col, row) ->
            InputCell(
                signal = signal,
                setSignal = setter,
                format = format,
                name = name,
                col = col,
                row = row
            ) :> HtmlElement
        | FormulaData(signal, format, formula) ->
            FormulaCell(
                signal = signal,
                format = format,
                formula = formula
            ) :> HtmlElement
        | LabelData text ->
            LabelCell(text = text) :> HtmlElement
        | EmptyData ->
            EmptyCell() :> HtmlElement

/// Public API for rendering ReactiveModel to SolidJS components
module SpreadsheetRenderer =

    /// Render a ReactiveModel as a Partas.Solid spreadsheet component
    [<SolidComponent>]
    let SpreadsheetApp (model: ReactiveModel) =
        let registry = buildRegistry model
        let rows, cols = ReactiveModel.dimensions model

        let getCellComponent = System.Func<int, int, HtmlElement>(fun row col ->
            let cellData = getCellData model registry row col
            CellRenderer(cellData = cellData) :> HtmlElement)

        Spreadsheet(
            title = model.Title,
            rows = rows,
            cols = cols,
            getCellComponent = getCellComponent,
            showHeaders = model.ShowHeaders
        )

    /// Render a ReactiveModel with a split: frozen top rows + scrollable bottom rows
    /// splitAtRow is the first row of the scrollable section (0-indexed)
    /// scrollableHeight is the CSS max-height for the entire scrollable container (e.g., "500px")
    [<SolidComponent>]
    let SpreadsheetSplitApp (model: ReactiveModel) (splitAtRow: int) (scrollableHeight: string) =
        let registry = buildRegistry model
        let totalRows, cols = ReactiveModel.dimensions model

        let getCellComponent = System.Func<int, int, HtmlElement>(fun row col ->
            let cellData = getCellData model registry row col
            CellRenderer(cellData = cellData) :> HtmlElement)

        div(class' = Styles.spreadsheetContainer) {
            // Title
            Show(when' = model.Title.IsSome) {
                h2(class' = Styles.spreadsheetTitle) {
                    model.Title |> Option.defaultValue ""
                }
            }

            // Single table with sticky frozen rows - columns naturally align
            SpreadsheetSplitGrid(
                rows = totalRows,
                cols = cols,
                splitAtRow = splitAtRow,
                scrollableHeight = scrollableHeight,
                getCellComponent = getCellComponent,
                showHeaders = model.ShowHeaders
            )
        }

    /// Create a standalone app component with mount logic
    [<SolidComponent>]
    let App (model: ReactiveModel) =
        div(class' = "min-h-screen bg-gray-50 dark:bg-gray-900 py-8") {
            div(class' = "max-w-4xl mx-auto px-4") {
                SpreadsheetApp model
                // Add the live log panel accordion below the spreadsheet
                LiveLogPanel()
            }
        }
