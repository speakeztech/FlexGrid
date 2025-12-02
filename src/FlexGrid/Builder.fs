namespace FlexGrid

/// Builder for constructing reactive spreadsheets
type ReactiveSpreadsheetBuilder() =
    let mutable cells: PositionedCell list = []
    let mutable currentRow = 0
    let mutable currentCol = 0
    let mutable title: string option = None
    let mutable showHeaders = true

    /// Add an input cell at the current position with a name
    member _.Input(name: string, initial: float, ?format: string) =
        let cell = ReactiveCell.Input(name, initial, format)
        cells <- { Position = { Row = currentRow; Col = currentCol }; Cell = cell } :: cells
        currentCol <- currentCol + 1

    /// Add a formula cell at the current position
    member _.Formula(expr: string, ?format: string) =
        let cell = ReactiveCell.Formula(expr, format)
        cells <- { Position = { Row = currentRow; Col = currentCol }; Cell = cell } :: cells
        currentCol <- currentCol + 1

    /// Add a label cell at the current position
    member _.Label(text: string) =
        let cell = ReactiveCell.Label text
        cells <- { Position = { Row = currentRow; Col = currentCol }; Cell = cell } :: cells
        currentCol <- currentCol + 1

    /// Add an empty cell at the current position
    member _.Empty() =
        let cell = ReactiveCell.Empty
        cells <- { Position = { Row = currentRow; Col = currentCol }; Cell = cell } :: cells
        currentCol <- currentCol + 1

    /// Move to the next row
    member _.NewRow() =
        currentRow <- currentRow + 1
        currentCol <- 0

    /// Skip a number of columns
    member _.Skip(count: int) =
        currentCol <- currentCol + count

    /// Go to a specific row
    member _.GoToRow(row: int) =
        currentRow <- row
        currentCol <- 0

    /// Go to a specific column
    member _.GoToCol(col: int) =
        currentCol <- col

    /// Go to a specific position
    member _.GoTo(row: int, col: int) =
        currentRow <- row
        currentCol <- col

    /// Set the spreadsheet title
    member _.Title(t: string) =
        title <- Some t

    /// Set whether to show Excel-style headers
    member _.ShowHeaders(show: bool) =
        showHeaders <- show

    /// Build the reactive model
    member _.Build() : ReactiveModel =
        { Cells = List.rev cells
          ColumnWidths = None
          Title = title
          ShowHeaders = showHeaders }

/// Computation expression builder for reactive spreadsheets
type ReactiveSheetBuilder() =
    member _.Yield(_: unit) = []

    member _.Zero() = []

    member _.Delay(f: unit -> PositionedCell list) = f()

    member _.Combine(a: PositionedCell list, b: PositionedCell list) = a @ b

    /// Add an input cell
    [<CustomOperation("input")>]
    member _.InputCell(state: PositionedCell list, row: int, col: int, name: string, initial: float) =
        let cell = { Position = { Row = row; Col = col }; Cell = ReactiveCell.Input(name, initial, None) }
        state @ [cell]

    /// Add an input cell with format
    [<CustomOperation("inputf")>]
    member _.InputCellFormatted(state: PositionedCell list, row: int, col: int, name: string, initial: float, format: string) =
        let cell = { Position = { Row = row; Col = col }; Cell = ReactiveCell.Input(name, initial, Some format) }
        state @ [cell]

    /// Add a formula cell
    [<CustomOperation("formula")>]
    member _.FormulaCell(state: PositionedCell list, row: int, col: int, expr: string) =
        let cell = { Position = { Row = row; Col = col }; Cell = ReactiveCell.Formula(expr, None) }
        state @ [cell]

    /// Add a formula cell with format
    [<CustomOperation("formulaf")>]
    member _.FormulaCellFormatted(state: PositionedCell list, row: int, col: int, expr: string, format: string) =
        let cell = { Position = { Row = row; Col = col }; Cell = ReactiveCell.Formula(expr, Some format) }
        state @ [cell]

    /// Add a label cell
    [<CustomOperation("label")>]
    member _.LabelCell(state: PositionedCell list, row: int, col: int, text: string) =
        let cell = { Position = { Row = row; Col = col }; Cell = ReactiveCell.Label text }
        state @ [cell]

    member _.Run(cells: PositionedCell list) : ReactiveModel =
        { Cells = cells
          ColumnWidths = None
          Title = None
          ShowHeaders = true }

[<AutoOpen>]
module BuilderInstances =
    /// DSL entry point for imperative style
    let reactiveSheet () = ReactiveSpreadsheetBuilder()

    /// DSL entry point for computation expression style
    let rsheet = ReactiveSheetBuilder()
