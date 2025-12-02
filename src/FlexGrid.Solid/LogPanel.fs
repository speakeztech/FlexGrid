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

    [<Import("onMount", "solid-js")>]
    let onMount (fn: unit -> unit) : unit = jsNative

    [<Import("createEffect", "solid-js")>]
    let createEffect (fn: unit -> unit) : unit = jsNative

/// Smooth scroll animation utilities (inverse of SpeakEZ's back-to-top)
module private ScrollAnimation =
    open Browser.Dom
    open Browser.Types
    open Fable.Core.JsInterop

    /// Access performance.now() via JS interop
    [<Emit("performance.now()")>]
    let performanceNow() : float = jsNative

    /// Easing function for smooth animation (same as SpeakEZ back-to-top)
    let easeOutCubic (t: float) : float =
        1.0 - System.Math.Pow(1.0 - t, 3.0)

    /// Smoothly scroll a container element to the bottom
    let scrollToBottom (element: Element option) (duration: float) =
        match element with
        | None -> ()
        | Some el ->
            let container = el :?> HTMLElement
            let start = container.scrollTop
            let targetScroll = container.scrollHeight - container.clientHeight
            let distance = targetScroll - start

            // Only scroll if there's actually distance to cover
            if distance > 0.0 then
                let startTime = performanceNow()

                let rec scroll () =
                    let elapsed = performanceNow() - startTime
                    let progress = min (elapsed / duration) 1.0
                    let eased = easeOutCubic progress
                    container.scrollTop <- start + (distance * eased)

                    if progress < 1.0 then
                        window.requestAnimationFrame(fun _ -> scroll()) |> ignore

                scroll()

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
            | "INPUT_CHANGED" -> Styles.logEntryInput  // Double-line border for INPUT
            | _ -> Styles.logEntry

        let getIconClass (eventType: string) =
            match eventType with
            | "INPUT_CHANGED" -> Styles.logIconInput
            | "FORMULA_EVALUATING" -> Styles.logIconEval
            | "FORMULA_EVALUATED" -> Styles.logIconCalc
            | "DEPENDENCY_TRIGGERED" -> Styles.logIconDep
            | _ -> Styles.logMsgDefault

        let getMsgClass (eventType: string) =
            match eventType with
            | "FORMULA_EVALUATED" -> Styles.logMsgAccent  // CALC results in accent color
            | _ -> Styles.logMsgDefault  // Everything else in theme text color

        let getIcon (eventType: string) =
            match eventType with
            | "INPUT_CHANGED" -> "[INPUT]"
            | "FORMULA_EVALUATING" -> "[EVAL]"
            | "FORMULA_EVALUATED" -> "[CALC]"
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
                        span(class' = "text-xs text-speakez-neutral/60 dark:text-speakez-neutral-light/60") {
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
                                span(class' = Styles.cn ["mr-2"; getIconClass entry.EventType]) { getIcon entry.EventType }
                                span(class' = getMsgClass entry.EventType) { entry.Message }
                            }
                        }
                    }
                }
            }
        }

/// Log panel that auto-refreshes periodically when open
/// Defaults to open and starts logging immediately
[<Erase>]
type LiveLogPanel() =
    inherit div()

    /// Whether the panel should start open (defaults to true)
    [<Erase>]
    member val initialOpen: bool = true with get, set

    /// Whether to use side-by-side layout styling (no top margin, full height)
    [<Erase>]
    member val sideBySide: bool = false with get, set

    [<SolidTypeComponent>]
    member props.constructor =
        let startOpen = props.initialOpen
        let isSideBySide = props.sideBySide
        let isOpen, setIsOpen = LogPanelImports.createSignal startOpen
        let entries, setEntries = LogPanelImports.createSignal<LogEntry list> []

        // Mutable ref for the log content container (for smooth scroll)
        let mutable logContentRef: Browser.Types.Element option = None

        // Set up interval to refresh entries when panel is open
        let mutable intervalId: int option = None

        let refreshEntries () =
            if isOpen() then
                let newEntries = GlobalCalcLogger.getEntries()
                setEntries newEntries

        let startAutoRefresh () =
            GlobalCalcLogger.enable()
            refreshEntries()
            // Start auto-refresh interval
            let id = Browser.Dom.window.setInterval(refreshEntries, 100)
            intervalId <- Some (int id)

        let stopAutoRefresh () =
            match intervalId with
            | Some id -> Browser.Dom.window.clearInterval(float id)
            | None -> ()
            intervalId <- None

        // Start auto-refresh on mount if initially open
        LogPanelImports.onMount (fun () ->
            if startOpen then
                startAutoRefresh()
        )

        // Effect to smoothly scroll to bottom when entries change
        LogPanelImports.createEffect (fun () ->
            let _ = entries() // Track entries signal for reactivity
            // Scroll with 500ms duration (slow, smooth animation)
            ScrollAnimation.scrollToBottom logContentRef 500.0
        )

        let handleToggle (_: MouseEvent) =
            let newState = not (isOpen())
            setIsOpen newState
            if newState then
                startAutoRefresh()
            else
                stopAutoRefresh()

        let handleClear (_: MouseEvent) =
            GlobalCalcLogger.clear()
            refreshEntries()

        let getEntryClass (eventType: string) =
            match eventType with
            | "INPUT_CHANGED" -> Styles.logEntryInput  // Double-line border for INPUT
            | _ -> Styles.logEntry

        let getIconClass (eventType: string) =
            match eventType with
            | "INPUT_CHANGED" -> Styles.logIconInput
            | "FORMULA_EVALUATING" -> Styles.logIconEval
            | "FORMULA_EVALUATED" -> Styles.logIconCalc
            | "DEPENDENCY_TRIGGERED" -> Styles.logIconDep
            | _ -> Styles.logMsgDefault

        let getMsgClass (eventType: string) =
            match eventType with
            | "FORMULA_EVALUATED" -> Styles.logMsgAccent  // CALC results in accent color
            | _ -> Styles.logMsgDefault  // Everything else in theme text color

        let getIcon (eventType: string) =
            match eventType with
            | "INPUT_CHANGED" -> "[INPUT]"
            | "FORMULA_EVALUATING" -> "[EVAL]"
            | "FORMULA_EVALUATED" -> "[CALC]"
            | "DEPENDENCY_TRIGGERED" -> "[DEP]"
            | _ -> "[?]"

        let containerClass = if isSideBySide then Styles.logPanelContainerSideBySide else Styles.logPanelContainer

        div(class' = containerClass) {
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
                        span(class' = "text-xs text-speakez-neutral/60 dark:text-speakez-neutral-light/60") {
                            sprintf "%d entries" (List.length (entries()))
                        }
                    }

                    // Log entries (newest at bottom for readability) - scrollable container
                    div(class' = Styles.logPanelContent).ref(fun el -> logContentRef <- Some el) {
                        let currentEntries = entries() |> List.rev |> List.toArray
                        For(each = currentEntries) { yield fun entry _ ->
                            div(class' = getEntryClass entry.EventType) {
                                span(class' = Styles.cn ["mr-2"; getIconClass entry.EventType]) { getIcon entry.EventType }
                                span(class' = getMsgClass entry.EventType) { entry.Message }
                            }
                        }
                    }
                }
            }
        }
