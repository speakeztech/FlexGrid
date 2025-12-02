namespace FlexGrid

open Fable.Core
open Fable.Core.JsInterop

/// Represents a single log entry for calculation tracing
type LogEntry = {
    Timestamp: float
    EventType: string
    CellAddress: string option
    Formula: string option
    OldValue: float option
    NewValue: float option
    Message: string
}

/// Log entry types
module LogEventType =
    let [<Literal>] InputChanged = "INPUT_CHANGED"
    let [<Literal>] FormulaEvaluating = "FORMULA_EVALUATING"
    let [<Literal>] FormulaEvaluated = "FORMULA_EVALUATED"
    let [<Literal>] DependencyTriggered = "DEPENDENCY_TRIGGERED"

/// Calculation logger that tracks formula evaluation cascade
type CalcLogger = {
    mutable Entries: LogEntry list
    mutable Enabled: bool
    mutable MaxEntries: int
}

module CalcLogger =

    /// Import browser performance.now() for high-resolution timestamps
    [<Emit("performance.now()")>]
    let private performanceNow () : float = jsNative

    /// Create a new logger
    let create () : CalcLogger = {
        Entries = []
        Enabled = false
        MaxEntries = 500
    }

    /// Enable logging
    let enable (logger: CalcLogger) =
        logger.Enabled <- true

    /// Disable logging
    let disable (logger: CalcLogger) =
        logger.Enabled <- false

    /// Clear all log entries
    let clear (logger: CalcLogger) =
        logger.Entries <- []

    /// Convert column index to letter (0 -> A, 1 -> B, etc.)
    let private colToLetter (col: int) =
        let rec loop n acc =
            if n < 0 then acc
            else
                let letter = char (int 'A' + (n % 26))
                loop ((n / 26) - 1) (string letter + acc)
        loop col ""

    /// Format cell address (col=0, row=0 -> "A1")
    let formatAddress (col: int) (row: int) =
        sprintf "%s%d" (colToLetter col) (row + 1)

    /// Add a log entry
    let private addEntry (logger: CalcLogger) (entry: LogEntry) =
        if logger.Enabled then
            logger.Entries <- entry :: logger.Entries
            // Trim if over max
            if List.length logger.Entries > logger.MaxEntries then
                logger.Entries <- List.take logger.MaxEntries logger.Entries

    /// Log an input cell value change
    let logInputChanged (logger: CalcLogger) (col: int) (row: int) (name: string) (oldVal: float) (newVal: float) =
        addEntry logger {
            Timestamp = performanceNow()
            EventType = LogEventType.InputChanged
            CellAddress = Some (formatAddress col row)
            Formula = None
            OldValue = Some oldVal
            NewValue = Some newVal
            Message = sprintf "Input '%s' changed: %.4f -> %.4f" name oldVal newVal
        }

    /// Log start of formula evaluation
    let logFormulaEvaluating (logger: CalcLogger) (col: int) (row: int) (formula: string) =
        addEntry logger {
            Timestamp = performanceNow()
            EventType = LogEventType.FormulaEvaluating
            CellAddress = Some (formatAddress col row)
            Formula = Some formula
            OldValue = None
            NewValue = None
            Message = sprintf "Evaluating %s: %s" (formatAddress col row) formula
        }

    /// Log completion of formula evaluation
    let logFormulaEvaluated (logger: CalcLogger) (col: int) (row: int) (formula: string) (result: float) =
        addEntry logger {
            Timestamp = performanceNow()
            EventType = LogEventType.FormulaEvaluated
            CellAddress = Some (formatAddress col row)
            Formula = Some formula
            OldValue = None
            NewValue = Some result
            Message = sprintf "%s = %.4f" (formatAddress col row) result
        }

    /// Log a dependency being triggered
    let logDependencyTriggered (logger: CalcLogger) (sourceName: string) (dependentCol: int) (dependentRow: int) =
        addEntry logger {
            Timestamp = performanceNow()
            EventType = LogEventType.DependencyTriggered
            CellAddress = Some (formatAddress dependentCol dependentRow)
            Formula = None
            OldValue = None
            NewValue = None
            Message = sprintf "Dependency: %s triggered recalc of %s" sourceName (formatAddress dependentCol dependentRow)
        }

    /// Get all entries (newest first)
    let getEntries (logger: CalcLogger) : LogEntry list =
        logger.Entries

    /// Get entries in chronological order (oldest first)
    let getEntriesChronological (logger: CalcLogger) : LogEntry list =
        List.rev logger.Entries

    /// Format an entry for display
    let formatEntry (entry: LogEntry) : string =
        let icon =
            match entry.EventType with
            | "INPUT_CHANGED" -> "[INPUT]"
            | "FORMULA_EVALUATING" -> "[CALC>]"
            | "FORMULA_EVALUATED" -> "[CALC=]"
            | "DEPENDENCY_TRIGGERED" -> "[DEP]"
            | _ -> "[?]"
        sprintf "%s %s" icon entry.Message

/// Global logger instance (for simplicity in this demo)
module GlobalCalcLogger =
    let mutable private instance : CalcLogger option = None

    let get () =
        match instance with
        | Some logger -> logger
        | None ->
            let logger = CalcLogger.create()
            instance <- Some logger
            logger

    let enable () = CalcLogger.enable (get())
    let disable () = CalcLogger.disable (get())
    let clear () = CalcLogger.clear (get())
    let getEntries () = CalcLogger.getEntries (get())
