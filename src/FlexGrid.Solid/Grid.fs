namespace FlexGrid.Solid

open Fable.Core
open Partas.Solid

/// Complete spreadsheet grid component
[<Erase>]
type SpreadsheetGrid() =
    inherit table()

    /// Number of rows in the grid
    [<Erase>]
    member val rows: int = 0 with get, set

    /// Number of columns in the grid
    [<Erase>]
    member val cols: int = 0 with get, set

    /// Function to get the cell component for a given position
    [<Erase>]
    member val getCellComponent: (int -> int -> HtmlElement) = Unchecked.defaultof<_> with get, set

    /// Whether to show column/row headers
    [<Erase>]
    member val showHeaders: bool = true with get, set

    [<SolidTypeComponent>]
    member props.constructor =
        // Generate column letters (A, B, C, ..., Z, AA, AB, ...)
        let colLetter (index: int) =
            let rec loop n acc =
                if n < 0 then acc
                else
                    let letter = char (int 'A' + (n % 26))
                    loop (n / 26 - 1) (string letter + acc)
            loop index ""

        let colLetters = [| for i in 0 .. props.cols - 1 -> colLetter i |]
        let rowIndices = [| 0 .. props.rows - 1 |]
        let colIndices = [| 0 .. props.cols - 1 |]

        table(class' = Styles.spreadsheetTable) {
            // Column headers row
            Show(when' = props.showHeaders) {
                thead() {
                    tr() {
                        // Empty corner cell
                        HeaderCell(text = "")
                        // Column letters
                        For(each = colLetters) { yield fun letter _ ->
                            HeaderCell(text = letter) :> HtmlElement
                        }
                    }
                }
            }

            tbody() {
                For(each = rowIndices) { yield fun row _ ->
                    tr() {
                        // Row number header
                        Show(when' = props.showHeaders) {
                            HeaderCell(text = string (row + 1))
                        }
                        // Data cells
                        For(each = colIndices) { yield fun col _ ->
                            props.getCellComponent row col
                        }
                    }
                }
            }
        }

/// Spreadsheet wrapper component with title
[<Erase>]
type Spreadsheet() =
    inherit div()

    /// Optional title for the spreadsheet
    [<Erase>]
    member val title: string option = None with get, set

    /// Number of rows
    [<Erase>]
    member val rows: int = 0 with get, set

    /// Number of columns
    [<Erase>]
    member val cols: int = 0 with get, set

    /// Cell component getter
    [<Erase>]
    member val getCellComponent: (int -> int -> HtmlElement) = Unchecked.defaultof<_> with get, set

    /// Show headers
    [<Erase>]
    member val showHeaders: bool = true with get, set

    [<SolidTypeComponent>]
    member props.constructor =
        div(class' = Styles.spreadsheetContainer) {
            Show(when' = props.title.IsSome) {
                h2(class' = Styles.spreadsheetTitle) {
                    props.title |> Option.defaultValue ""
                }
            }
            SpreadsheetGrid(
                rows = props.rows,
                cols = props.cols,
                getCellComponent = props.getCellComponent,
                showHeaders = props.showHeaders
            )
        }
