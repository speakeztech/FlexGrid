namespace FlexGrid

open Fable.Core

/// Represents a cell's reactive behavior in the spreadsheet
[<RequireQualifiedAccess>]
type ReactiveCell =
    /// An input cell the user can edit
    | Input of name: string * initial: float * format: string option
    /// A formula cell that computes from other cells
    | Formula of expr: string * format: string option
    /// A static label cell
    | Label of text: string
    /// An empty cell
    | Empty

/// Cell position in the spreadsheet (zero-based)
type CellPosition = {
    Row: int
    Col: int
}

/// A positioned cell in the spreadsheet
type PositionedCell = {
    Position: CellPosition
    Cell: ReactiveCell
}

/// Represents a complete reactive spreadsheet model
type ReactiveModel = {
    /// List of positioned cells
    Cells: PositionedCell list
    /// Optional column widths in characters
    ColumnWidths: int list option
    /// Optional title for the spreadsheet
    Title: string option
    /// Whether to show Excel-style headers (A, B, C... and 1, 2, 3...)
    ShowHeaders: bool
}

module ReactiveModel =
    /// Create an empty reactive model
    let empty = {
        Cells = []
        ColumnWidths = None
        Title = None
        ShowHeaders = true
    }

    /// Get the dimensions of the spreadsheet (rows, cols)
    let dimensions model =
        if List.isEmpty model.Cells then
            (0, 0)
        else
            let maxRow = model.Cells |> List.map (fun c -> c.Position.Row) |> List.max
            let maxCol = model.Cells |> List.map (fun c -> c.Position.Col) |> List.max
            (maxRow + 1, maxCol + 1)

    /// Get a cell at a specific position
    let tryGetCell row col model =
        model.Cells
        |> List.tryFind (fun c -> c.Position.Row = row && c.Position.Col = col)
        |> Option.map (fun c -> c.Cell)

    /// Add a cell to the model
    let addCell row col cell model =
        let positioned = { Position = { Row = row; Col = col }; Cell = cell }
        { model with Cells = positioned :: model.Cells }

    /// Set the title
    let withTitle title model =
        { model with Title = Some title }

    /// Set whether to show headers
    let withShowHeaders show model =
        { model with ShowHeaders = show }
