namespace Partas.Solid.FlexGrid

open Fable.Core
open Fable.Core.JsInterop
open Browser.Types
open global.Partas.Solid
open global.FlexGrid

/// Input cell component: editable, creates a signal
[<Erase>]
type InputCell() =
    inherit td()

    /// The signal accessor for the cell value
    [<Erase>]
    member val signal: Accessor<float> = Unchecked.defaultof<_> with get, set

    /// The setter function for the cell value
    [<Erase>]
    member val setSignal: (float -> unit) = Unchecked.defaultof<_> with get, set

    /// Optional format string for display
    [<Erase>]
    member val format: string option = None with get, set

    /// The cell name (for display purposes)
    [<Erase>]
    member val name: string = "" with get, set

    /// Column position for logging
    [<Erase>]
    member val col: int = 0 with get, set

    /// Row position for logging
    [<Erase>]
    member val row: int = 0 with get, set

    [<SolidTypeComponent>]
    member props.constructor =
        let formatValue (v: float) =
            match props.format with
            | Some fmt ->
                // Simple format handling
                if fmt.Contains("N") || fmt.Contains("F") then
                    let decimals =
                        fmt.Replace("N", "").Replace("F", "")
                        |> fun s -> if s = "" then 2 else int s
                    v.ToString($"F{decimals}")
                elif fmt.Contains("C") then
                    "$" + v.ToString("F2")
                elif fmt.Contains("P") then
                    (v * 100.0).ToString("F1") + "%"
                else
                    v.ToString()
            | None -> v.ToString("F2")

        let handleInput (e: Event) =
            let target = e.target :?> HTMLInputElement
            match System.Double.TryParse(target.value) with
            | true, v ->
                let oldVal = props.signal()
                props.setSignal v
                // Log the input change
                let logger = GlobalCalcLogger.get()
                CalcLogger.logInputChanged logger props.col props.row props.name oldVal v
            | false, _ -> () // Ignore invalid input

        td(class' = Styles.inputCell, title = props.name) {
            input(
                type' = "text",
                class' = Styles.cellInput,
                value = formatValue(props.signal()),
                onInput = handleInput
            )
        }

/// Formula cell component: displays computed value
[<Erase>]
type FormulaCell() =
    inherit td()

    /// The signal accessor for the computed value
    [<Erase>]
    member val signal: Accessor<float> = Unchecked.defaultof<_> with get, set

    /// Optional format string for display
    [<Erase>]
    member val format: string option = None with get, set

    /// The formula expression (shown on hover)
    [<Erase>]
    member val formula: string = "" with get, set

    [<SolidTypeComponent>]
    member props.constructor =
        let formatValue (v: float) =
            if System.Double.IsNaN(v) then
                "#ERROR"
            elif System.Double.IsInfinity(v) then
                "#DIV/0!"
            else
                match props.format with
                | Some fmt ->
                    if fmt.Contains("N") || fmt.Contains("F") then
                        let decimals =
                            fmt.Replace("N", "").Replace("F", "")
                            |> fun s -> if s = "" then 2 else int s
                        v.ToString($"F{decimals}")
                    elif fmt.Contains("C") then
                        "$" + v.ToString("F2")
                    elif fmt.Contains("P") then
                        (v * 100.0).ToString("F1") + "%"
                    else
                        v.ToString()
                | None -> v.ToString("F2")

        let getCellClass (v: float) =
            if System.Double.IsNaN(v) || System.Double.IsInfinity(v) then
                Styles.errorCell
            else
                Styles.formulaCell

        // Note: In SolidJS, reactivity requires signals to be called inside JSX
        // We pass the accessor (props.signal) itself, and call it within the JSX expression
        td(class' = getCellClass(props.signal()), title = props.formula) {
            formatValue(props.signal())
        }

/// Label cell component: static text
[<Erase>]
type LabelCell() =
    inherit td()

    /// The text to display
    [<Erase>]
    member val text: string = "" with get, set

    [<SolidTypeComponent>]
    member props.constructor =
        td(class' = Styles.labelCell) {
            props.text
        }

/// Header cell component: column/row labels
[<Erase>]
type HeaderCell() =
    inherit th()

    /// The header text (A, B, C... or 1, 2, 3...)
    [<Erase>]
    member val text: string = "" with get, set

    [<SolidTypeComponent>]
    member props.constructor =
        th(class' = Styles.headerCell) {
            props.text
        }

/// Empty cell component
[<Erase>]
type EmptyCell() =
    inherit td()

    [<SolidTypeComponent>]
    member props.constructor =
        td(class' = Styles.emptyCell) { "" }
