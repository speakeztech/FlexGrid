namespace Partas.Solid.FlexGrid

/// CSS class definitions for spreadsheet components
module Styles =
    /// Base styles applied to all cells
    let cellBase = "border border-gray-300 dark:border-gray-600 px-2 py-1 text-right font-mono text-sm"

    /// Styles for editable input cells
    let inputCell = cellBase + " bg-yellow-50 dark:bg-yellow-900/30 dark:text-yellow-100"

    /// Styles for formula result cells
    let formulaCell = cellBase + " bg-white dark:bg-gray-800 dark:text-gray-100"

    /// Styles for label/text cells
    let labelCell = cellBase + " bg-gray-100 dark:bg-gray-700 text-left font-sans dark:text-gray-200"

    /// Styles for header cells (row/column headers)
    let headerCell = cellBase + " bg-gray-200 dark:bg-gray-700 font-semibold text-center dark:text-gray-200"

    /// Styles for empty cells
    let emptyCell = cellBase + " bg-white dark:bg-gray-800"

    /// Styles for error cells
    let errorCell = cellBase + " bg-red-100 dark:bg-red-900/40 text-red-700 dark:text-red-300"

    /// Styles for the input element inside input cells
    let cellInput = "w-full bg-transparent text-right outline-none dark:text-yellow-100"

    /// Styles for the spreadsheet table
    let spreadsheetTable = "border-collapse shadow-lg rounded-lg overflow-hidden"

    /// Styles for the spreadsheet container
    let spreadsheetContainer = "p-4 font-sans"

    /// Styles for the spreadsheet title
    let spreadsheetTitle = "text-xl font-bold mb-4 dark:text-gray-100"

    /// Join multiple class strings
    let cn (classes: string seq) =
        classes |> String.concat " "

    // LogPanel accordion styles
    let logPanelContainer = "mt-4 border border-gray-300 dark:border-gray-600 rounded-lg overflow-hidden"
    let logPanelHeader = "flex items-center justify-between px-4 py-2 bg-gray-100 dark:bg-gray-700 cursor-pointer hover:bg-gray-200 dark:hover:bg-gray-600 transition-colors"
    let logPanelTitle = "font-semibold text-sm text-gray-700 dark:text-gray-200"
    let logPanelToggle = "text-gray-500 dark:text-gray-400 transform transition-transform"
    let logPanelContent = "max-h-64 overflow-y-auto bg-gray-900 dark:bg-gray-950 text-sm"
    let logEntry = "font-mono text-xs px-4 py-1 border-b border-gray-800 dark:border-gray-900"
    let logEntryInput = "text-yellow-400"
    let logEntryEvaluating = "text-blue-400"
    let logEntryEvaluated = "text-green-400"
    let logEntryDependency = "text-purple-400"
    let logPanelToolbar = "flex items-center gap-2 px-4 py-2 bg-gray-800 dark:bg-gray-900 border-b border-gray-700"
    let logPanelButton = "px-2 py-1 text-xs bg-gray-700 dark:bg-gray-800 text-gray-300 rounded hover:bg-gray-600 dark:hover:bg-gray-700 transition-colors"
    let logPanelButtonActive = "px-2 py-1 text-xs bg-green-600 text-white rounded hover:bg-green-500 transition-colors"
