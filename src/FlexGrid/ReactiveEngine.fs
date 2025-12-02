namespace FlexGrid

open Fable.Core
open Fable.Core.JsInterop

/// Type alias for a SolidJS signal getter
type Accessor<'T> = unit -> 'T

/// Type alias for a SolidJS signal setter
type Setter<'T> = 'T -> unit

/// Registry for managing signals and their dependencies
type SignalRegistry = {
    mutable CellSignals: Map<(int * int), Accessor<float>>
    mutable NamedSignals: Map<string, Accessor<float>>
    mutable CellSetters: Map<(int * int), Setter<float>>
    mutable CellFormulas: Map<(int * int), string>
}

module SignalRegistry =
    /// Create a new empty registry
    let create () : SignalRegistry = {
        CellSignals = Map.empty
        NamedSignals = Map.empty
        CellSetters = Map.empty
        CellFormulas = Map.empty
    }

    /// Register a cell signal
    let registerCellSignal col row signal (registry: SignalRegistry) =
        registry.CellSignals <- Map.add (col, row) signal registry.CellSignals

    /// Register a cell setter
    let registerCellSetter col row setter (registry: SignalRegistry) =
        registry.CellSetters <- Map.add (col, row) setter registry.CellSetters

    /// Register a named signal
    let registerNamedSignal name signal (registry: SignalRegistry) =
        registry.NamedSignals <- Map.add name signal registry.NamedSignals

    /// Try to get a cell signal
    let tryGetCellSignal col row (registry: SignalRegistry) =
        Map.tryFind (col, row) registry.CellSignals

    /// Try to get a named signal
    let tryGetNamedSignal name (registry: SignalRegistry) =
        Map.tryFind name registry.NamedSignals

    /// Try to get a cell setter
    let tryGetCellSetter col row (registry: SignalRegistry) =
        Map.tryFind (col, row) registry.CellSetters

    /// Register a cell formula
    let registerCellFormula col row formula (registry: SignalRegistry) =
        registry.CellFormulas <- Map.add (col, row) formula registry.CellFormulas

    /// Try to get a cell formula
    let tryGetCellFormula col row (registry: SignalRegistry) =
        Map.tryFind (col, row) registry.CellFormulas

module ReactiveEngine =

    /// Import SolidJS createMemo for reactive derived values
    [<Import("createMemo", "solid-js")>]
    let private createMemo<'T> (fn: unit -> 'T) : Accessor<'T> = jsNative

    /// Compile an expression into a signal getter
    let rec compileExpr (registry: SignalRegistry) (expr: Expr) : Accessor<float> =
        match expr with
        | Expr.Number n ->
            fun () -> n

        | Expr.CellRef (col, row) ->
            match SignalRegistry.tryGetCellSignal col row registry with
            | Some signal -> signal
            | None ->
                // Return 0 for undefined cell references (like Excel)
                fun () -> 0.0

        | Expr.NamedRef name ->
            match SignalRegistry.tryGetNamedSignal name registry with
            | Some signal -> signal
            | None ->
                // Return 0 for undefined named references
                fun () -> 0.0

        | Expr.BinaryOp (left, op, right) ->
            let leftSignal = compileExpr registry left
            let rightSignal = compileExpr registry right
            let operation =
                match op with
                | "+" -> fun a b -> a + b
                | "-" -> fun a b -> a - b
                | "*" -> fun a b -> a * b
                | "/" -> fun a b -> if b = 0.0 then nan else a / b
                | "^" -> fun a b -> a ** b
                | "<" -> fun a b -> if a < b then 1.0 else 0.0
                | ">" -> fun a b -> if a > b then 1.0 else 0.0
                | "<=" -> fun a b -> if a <= b then 1.0 else 0.0
                | ">=" -> fun a b -> if a >= b then 1.0 else 0.0
                | "<>" -> fun a b -> if a <> b then 1.0 else 0.0
                | _ -> fun _ _ -> nan
            fun () -> operation (leftSignal()) (rightSignal())

        | Expr.FunctionCall (name, args) ->
            let argSignals = args |> List.map (compileExpr registry)
            match name with
            | "SUM" ->
                fun () -> argSignals |> List.sumBy (fun s -> s())
            | "AVERAGE" | "AVG" ->
                fun () ->
                    let values = argSignals |> List.map (fun s -> s())
                    if List.isEmpty values then 0.0
                    else List.sum values / float (List.length values)
            | "MAX" ->
                fun () -> argSignals |> List.map (fun s -> s()) |> List.max
            | "MIN" ->
                fun () -> argSignals |> List.map (fun s -> s()) |> List.min
            | "ABS" ->
                fun () -> abs (argSignals.[0]())
            | "SQRT" ->
                fun () -> sqrt (argSignals.[0]())
            | "ROUND" ->
                fun () ->
                    let value = argSignals.[0]()
                    let decimals = if argSignals.Length > 1 then int (argSignals.[1]()) else 0
                    System.Math.Round(value, decimals)
            | "FLOOR" ->
                fun () -> floor (argSignals.[0]())
            | "CEILING" | "CEIL" ->
                fun () -> ceil (argSignals.[0]())
            | "IF" ->
                fun () ->
                    let condition = argSignals.[0]()
                    let trueVal = argSignals.[1]()
                    let falseVal = if argSignals.Length > 2 then argSignals.[2]() else 0.0
                    if condition <> 0.0 then trueVal else falseVal
            | "BLANK" ->
                // Returns negative infinity as a sentinel value for "blank" display
                fun () -> System.Double.NegativeInfinity
            | "POW" | "POWER" ->
                fun () -> (argSignals.[0]()) ** (argSignals.[1]())
            | "LOG" ->
                fun () ->
                    let value = argSignals.[0]()
                    let baseVal = if argSignals.Length > 1 then argSignals.[1]() else 10.0
                    System.Math.Log(value, baseVal)
            | "LN" ->
                fun () -> log (argSignals.[0]())
            | "EXP" ->
                fun () -> exp (argSignals.[0]())
            | "PMT" ->
                // PMT(rate, nper, pv) - Payment for a loan
                fun () ->
                    let rate = argSignals.[0]()
                    let nper = argSignals.[1]()
                    let pv = argSignals.[2]()
                    if rate = 0.0 then
                        -pv / nper
                    else
                        -pv * rate * (1.0 + rate) ** nper / ((1.0 + rate) ** nper - 1.0)
            | "FV" ->
                // FV(rate, nper, pmt, [pv]) - Future value
                fun () ->
                    let rate = argSignals.[0]()
                    let nper = argSignals.[1]()
                    let pmt = argSignals.[2]()
                    let pv = if argSignals.Length > 3 then argSignals.[3]() else 0.0
                    if rate = 0.0 then
                        -(pv + pmt * nper)
                    else
                        let factor = (1.0 + rate) ** nper
                        -(pv * factor + pmt * (factor - 1.0) / rate)
            | "PV" ->
                // PV(rate, nper, pmt) - Present value
                fun () ->
                    let rate = argSignals.[0]()
                    let nper = argSignals.[1]()
                    let pmt = argSignals.[2]()
                    if rate = 0.0 then
                        -pmt * nper
                    else
                        -pmt * (1.0 - (1.0 + rate) ** -nper) / rate
            | _ ->
                // Unknown function returns NaN
                fun () -> nan

        | Expr.Parenthesized inner ->
            compileExpr registry inner

    /// Create a formula signal from a formula string with optional logging
    /// Uses createMemo to make the computed value reactive - it will automatically
    /// re-evaluate when any of its input signals change
    let createFormulaSignalAt (registry: SignalRegistry) (col: int) (row: int) (formula: string) : Accessor<float> =
        match FormulaParser.tryParse formula with
        | Some expr ->
            let computeFn = compileExpr registry expr
            let logger = GlobalCalcLogger.get()
            // Wrap in createMemo with logging (skip logging for BLANK results)
            createMemo (fun () ->
                let result = computeFn()
                // Don't log BLANK results (NegativeInfinity) to avoid flooding the log
                if not (System.Double.IsNegativeInfinity(result)) then
                    CalcLogger.logFormulaEvaluating logger col row formula
                    CalcLogger.logFormulaEvaluated logger col row formula result
                result
            )
        | None -> fun () -> nan

    /// Create a formula signal from a formula string (without position info for logging)
    let createFormulaSignal (registry: SignalRegistry) (formula: string) : Accessor<float> =
        createFormulaSignalAt registry 0 0 formula

    /// Evaluate a formula string given the current registry state (non-reactive)
    let evaluateFormula (registry: SignalRegistry) (formula: string) : float =
        let signal = createFormulaSignal registry formula
        signal()
