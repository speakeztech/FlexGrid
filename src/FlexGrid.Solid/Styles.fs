namespace Partas.Solid.FlexGrid

/// CSS class definitions for spreadsheet components using SpeakEZ theme
module Styles =
    /// Base styles applied to all cells
    let cellBase = "border border-speakez-neutral/30 dark:border-speakez-neutral-light/20 px-2 py-1 text-right font-mono text-sm"

    /// Styles for editable input cells (amber/gold for inputs)
    let inputCell = cellBase + " bg-amber-50 dark:bg-amber-900/30 text-speakez-neutral dark:text-amber-100"

    /// Styles for formula result cells
    let formulaCell = cellBase + " bg-white dark:bg-speakez-neutral-dark text-speakez-neutral dark:text-speakez-neutral-light"

    /// Styles for label/text cells
    let labelCell = cellBase + " bg-slate-100 dark:bg-speakez-neutral text-left font-sans text-speakez-neutral dark:text-speakez-neutral-light"

    /// Styles for header cells (row/column headers) - teal accent
    let headerCell = cellBase + " bg-speakez-teal/10 dark:bg-speakez-teal-dark/20 font-semibold text-center text-speakez-neutral dark:text-speakez-neutral-light"

    /// Styles for empty cells
    let emptyCell = cellBase + " bg-white dark:bg-speakez-neutral-dark"

    /// Styles for error cells
    let errorCell = cellBase + " bg-red-100 dark:bg-red-900/40 text-red-700 dark:text-red-300"

    /// Styles for the input element inside input cells
    let cellInput = "w-full bg-transparent text-right outline-none text-speakez-neutral dark:text-amber-100"

    /// Styles for the spreadsheet table
    let spreadsheetTable = "border-collapse shadow-lg rounded-lg overflow-hidden"

    /// Styles for the spreadsheet container
    let spreadsheetContainer = "p-4 font-sans"

    /// Styles for the spreadsheet title
    let spreadsheetTitle = "text-xl font-bold mb-4 font-heading text-speakez-neutral dark:text-speakez-neutral-light"

    /// Join multiple class strings
    let cn (classes: string seq) =
        classes |> String.concat " "

    // LogPanel accordion styles - themed to match site (light/dark)
    let logPanelContainer = "mt-4 border border-speakez-neutral/30 dark:border-speakez-neutral-light/20 rounded-lg overflow-hidden"
    let logPanelContainerSideBySide = "mt-0 border border-speakez-neutral/30 dark:border-speakez-neutral-light/20 rounded-lg overflow-hidden"
    let logPanelHeader = "flex items-center justify-between px-4 py-2 bg-slate-100 dark:bg-speakez-neutral cursor-pointer hover:bg-slate-200 dark:hover:bg-speakez-neutral/80 transition-colors"
    let logPanelTitle = "font-semibold text-sm text-speakez-neutral dark:text-speakez-neutral-light"
    let logPanelToggle = "text-speakez-neutral/60 dark:text-speakez-neutral-light/60 transform transition-transform"
    let logPanelContent = "max-h-96 overflow-y-auto bg-slate-50 dark:bg-speakez-neutral-dark text-sm"
    let logPanelToolbar = "flex items-center gap-2 px-4 py-2 bg-slate-100 dark:bg-speakez-neutral border-b border-speakez-neutral/20 dark:border-speakez-neutral-light/10"
    let logPanelButton = "px-2 py-1 text-xs bg-white dark:bg-speakez-neutral-dark text-speakez-neutral dark:text-speakez-neutral-light border border-speakez-neutral/30 dark:border-speakez-neutral-light/20 rounded hover:bg-slate-100 dark:hover:bg-speakez-neutral-dark/80 transition-colors"
    let logPanelButtonActive = "px-2 py-1 text-xs bg-speakez-teal text-white rounded hover:bg-speakez-teal-dark transition-colors"

    // Log entry base style - themed borders
    let logEntry = "font-mono text-xs px-4 py-1 border-b border-speakez-neutral/10 dark:border-speakez-neutral-light/10"
    // INPUT entries get a double-line bottom border to demarcate new calculation sets
    let logEntryInput = "font-mono text-xs px-4 py-1 border-b-4 border-double border-speakez-neutral/30 dark:border-speakez-neutral-light/30"

    // Log entry icon colors (the [EVAL], [CALC], etc. tags)
    let logIconInput = "text-amber-500 dark:text-amber-400"  // Yellow for INPUT
    let logIconEval = "text-speakez-blue dark:text-speakez-blue-light"
    let logIconCalc = "text-speakez-teal dark:text-speakez-teal"
    let logIconDep = "text-purple-600 dark:text-purple-400"

    // Log entry message colors
    let logMsgDefault = "text-speakez-neutral dark:text-speakez-neutral-light"  // Theme text color for formulas
    let logMsgAccent = "text-speakez-orange dark:text-speakez-orange"  // Orange highlight for CALC results
