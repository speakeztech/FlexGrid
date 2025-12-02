namespace FlexGrid.Solid

/// CSS class definitions for spreadsheet components
module Styles =
    /// Base styles applied to all cells
    let cellBase = "border border-gray-300 px-2 py-1 text-right font-mono text-sm"

    /// Styles for editable input cells
    let inputCell = cellBase + " bg-yellow-50"

    /// Styles for formula result cells
    let formulaCell = cellBase + " bg-white"

    /// Styles for label/text cells
    let labelCell = cellBase + " bg-gray-100 text-left font-sans"

    /// Styles for header cells (row/column headers)
    let headerCell = cellBase + " bg-gray-200 font-semibold text-center"

    /// Styles for empty cells
    let emptyCell = cellBase + " bg-white"

    /// Styles for error cells
    let errorCell = cellBase + " bg-red-100 text-red-700"

    /// Styles for the input element inside input cells
    let cellInput = "w-full bg-transparent text-right outline-none"

    /// Styles for the spreadsheet table
    let spreadsheetTable = "border-collapse shadow-lg rounded-lg overflow-hidden"

    /// Styles for the spreadsheet container
    let spreadsheetContainer = "p-4 font-sans"

    /// Styles for the spreadsheet title
    let spreadsheetTitle = "text-xl font-bold mb-4"

    /// Join multiple class strings
    let cn (classes: string seq) =
        classes |> String.concat " "
