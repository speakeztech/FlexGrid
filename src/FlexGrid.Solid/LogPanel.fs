namespace Partas.Solid.FlexGrid

open Fable.Core
open Fable.Core.JsInterop
open Browser.Types
open global.Partas.Solid
open global.FlexGrid

/// Import SolidJS primitives for the log panel
module private LogPanelImports =
    [<Import("createSignal", "solid-js")>]
    let createSignal<'T> (initialValue: 'T) : Accessor<'T> * ('T -> unit) = jsNative

    [<Import("createMemo", "solid-js")>]
    let createMemo<'T> (fn: unit -> 'T) : Accessor<'T> = jsNative

    [<Import("For", "solid-js")>]
    let For : obj = jsNative

/// Log panel accordion component that displays the calculation log
[<Erase>]
type LogPanel() =
    inherit div()

    [<SolidTypeComponent>]
    member props.constructor =
        let isOpen, setIsOpen = LogPanelImports.createSignal false
        let refreshTrigger, setRefresh = LogPanelImports.createSignal 0

        // Enable logging when panel is opened
        let handleToggle (_: MouseEvent) =
            let newState = not (isOpen())
            setIsOpen newState
            if newState then
                GlobalCalcLogger.enable()
            // Refresh log display
            setRefresh(refreshTrigger() + 1)

        let handleClear (_: MouseEvent) =
            GlobalCalcLogger.clear()
            setRefresh(refreshTrigger() + 1)

        let getEntryClass (eventType: string) =
            match eventType with
            | "INPUT_CHANGED" -> Styles.cn [Styles.logEntry; Styles.logEntryInput]
            | "FORMULA_EVALUATING" -> Styles.cn [Styles.logEntry; Styles.logEntryEvaluating]
            | "FORMULA_EVALUATED" -> Styles.cn [Styles.logEntry; Styles.logEntryEvaluated]
            | "DEPENDENCY_TRIGGERED" -> Styles.cn [Styles.logEntry; Styles.logEntryDependency]
            | _ -> Styles.logEntry

        let getIcon (eventType: string) =
            match eventType with
            | "INPUT_CHANGED" -> "[INPUT]"
            | "FORMULA_EVALUATING" -> "[CALC>]"
            | "FORMULA_EVALUATED" -> "[CALC=]"
            | "DEPENDENCY_TRIGGERED" -> "[DEP]"
            | _ -> "[?]"

        div(class' = Styles.logPanelContainer) {
            // Accordion header
            div(class' = Styles.logPanelHeader, onClick = handleToggle) {
                span(class' = Styles.logPanelTitle) { "Calculation Log" }
                span(class' = Styles.logPanelToggle + if isOpen() then " rotate-180" else "") {
                    // Simple triangle indicator
                    if isOpen() then "\u25B2" else "\u25BC"
                }
            }

            // Accordion content (only shown when open)
            Show(when' = isOpen()) {
                div() {
                    // Toolbar
                    div(class' = Styles.logPanelToolbar) {
                        button(
                            class' = Styles.logPanelButton,
                            onClick = handleClear
                        ) { "Clear Log" }
                        span(class' = "text-xs text-gray-400") {
                            let _ = refreshTrigger() // Track for reactivity
                            let count = GlobalCalcLogger.getEntries() |> List.length
                            sprintf "%d entries" count
                        }
                    }

                    // Log entries
                    div(class' = Styles.logPanelContent) {
                        let _ = refreshTrigger() // Track for reactivity
                        let entriesArray = GlobalCalcLogger.getEntries() |> List.rev |> List.toArray // Show newest at bottom
                        For(each = entriesArray) { yield fun entry _ ->
                            div(class' = getEntryClass entry.EventType) {
                                span(class' = "mr-2") { getIcon entry.EventType }
                                span() { entry.Message }
                            }
                        }
                    }
                }
            }
        }

/// Log panel that auto-refreshes periodically when open
[<Erase>]
type LiveLogPanel() =
    inherit div()

    [<SolidTypeComponent>]
    member props.constructor =
        let isOpen, setIsOpen = LogPanelImports.createSignal false
        let entries, setEntries = LogPanelImports.createSignal<LogEntry list> []

        // Set up interval to refresh entries when panel is open
        let mutable intervalId: int option = None

        let refreshEntries () =
            if isOpen() then
                let newEntries = GlobalCalcLogger.getEntries()
                setEntries newEntries

        let handleToggle (_: MouseEvent) =
            let newState = not (isOpen())
            setIsOpen newState
            if newState then
                GlobalCalcLogger.enable()
                refreshEntries()
                // Start auto-refresh interval
                let id = Browser.Dom.window.setInterval(refreshEntries, 100)
                intervalId <- Some (int id)
            else
                // Stop auto-refresh
                match intervalId with
                | Some id -> Browser.Dom.window.clearInterval(float id)
                | None -> ()
                intervalId <- None

        let handleClear (_: MouseEvent) =
            GlobalCalcLogger.clear()
            refreshEntries()

        let getEntryClass (eventType: string) =
            match eventType with
            | "INPUT_CHANGED" -> Styles.cn [Styles.logEntry; Styles.logEntryInput]
            | "FORMULA_EVALUATING" -> Styles.cn [Styles.logEntry; Styles.logEntryEvaluating]
            | "FORMULA_EVALUATED" -> Styles.cn [Styles.logEntry; Styles.logEntryEvaluated]
            | "DEPENDENCY_TRIGGERED" -> Styles.cn [Styles.logEntry; Styles.logEntryDependency]
            | _ -> Styles.logEntry

        let getIcon (eventType: string) =
            match eventType with
            | "INPUT_CHANGED" -> "[INPUT]"
            | "FORMULA_EVALUATING" -> "[CALC>]"
            | "FORMULA_EVALUATED" -> "[CALC=]"
            | "DEPENDENCY_TRIGGERED" -> "[DEP]"
            | _ -> "[?]"

        div(class' = Styles.logPanelContainer) {
            // Accordion header
            div(class' = Styles.logPanelHeader, onClick = handleToggle) {
                span(class' = Styles.logPanelTitle) { "Calculation Log (Live)" }
                span(class' = Styles.logPanelToggle + if isOpen() then " rotate-180" else "") {
                    if isOpen() then "\u25B2" else "\u25BC"
                }
            }

            // Accordion content (only shown when open)
            Show(when' = isOpen()) {
                div() {
                    // Toolbar
                    div(class' = Styles.logPanelToolbar) {
                        button(
                            class' = Styles.logPanelButton,
                            onClick = handleClear
                        ) { "Clear Log" }
                        span(class' = "text-xs text-gray-400") {
                            sprintf "%d entries" (List.length (entries()))
                        }
                    }

                    // Log entries (newest at bottom for readability)
                    div(class' = Styles.logPanelContent) {
                        let currentEntries = entries() |> List.rev |> List.toArray
                        For(each = currentEntries) { yield fun entry _ ->
                            div(class' = getEntryClass entry.EventType) {
                                span(class' = "mr-2") { getIcon entry.EventType }
                                span() { entry.Message }
                            }
                        }
                    }
                }
            }
        }
