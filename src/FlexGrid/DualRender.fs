namespace FlexGrid

open FsExcel
open ClosedXML.Excel

/// Module for rendering ReactiveModel to FsExcel/Excel format
module DualRender =

    /// Convert a ReactiveCell to FsExcel CellProp list
    let private cellToProps (cell: ReactiveCell) : CellProp list =
        match cell with
        | ReactiveCell.Input(_, initial, format) ->
            let baseProps = [
                Float initial
                BackgroundColor XLColor.LightYellow
            ]
            match format with
            | Some f -> baseProps @ [FormatCode f]
            | None -> baseProps

        | ReactiveCell.Formula(expr, format) ->
            let baseProps = [FormulaA1 expr]
            match format with
            | Some f -> baseProps @ [FormatCode f]
            | None -> baseProps

        | ReactiveCell.Label text ->
            [
                String text
                BackgroundColor XLColor.LightGray
            ]

        | ReactiveCell.Empty ->
            [String ""]

    /// Convert a ReactiveModel to a list of FsExcel Item instructions
    let toFsExcelItems (model: ReactiveModel) : Item list =
        let sortedCells =
            model.Cells
            |> List.sortBy (fun c -> c.Position.Row, c.Position.Col)

        let mutable items: Item list = []
        let mutable lastRow = 0
        let mutable lastCol = -1

        for posCell in sortedCells do
            let row = posCell.Position.Row
            let col = posCell.Position.Col

            // Handle row changes
            while lastRow < row do
                items <- items @ [Go NewRow]
                lastRow <- lastRow + 1
                lastCol <- -1

            // Handle column positioning (skip cells if needed)
            while lastCol + 1 < col do
                items <- items @ [Cell [String ""]]
                lastCol <- lastCol + 1

            // Add the cell
            let cellProps = cellToProps posCell.Cell
            items <- items @ [Cell cellProps]
            lastCol <- col

        items

    /// Render a ReactiveModel to an Excel file
    let toExcelFile (path: string) (model: ReactiveModel) =
        model
        |> toFsExcelItems
        |> Render.AsFile path

    /// Render a ReactiveModel to Excel bytes (for web scenarios)
    let toExcelBytes (model: ReactiveModel) : byte[] =
        model
        |> toFsExcelItems
        |> Render.AsStreamBytes

    /// Generate an HTML representation using FsExcel's AsHtml
    let toStaticHtml (model: ReactiveModel) : string =
        let isHeader row col =
            model.ShowHeaders && (row = 0 || col = 0)

        model
        |> toFsExcelItems
        |> Render.AsHtml isHeader
