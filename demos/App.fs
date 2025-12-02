namespace Partas.Solid.FlexGrid.Demos

open Fable.Core
open Fable.Core.JsInterop
open Browser.Dom
open Browser.Types
open global.Partas.Solid
open global.FlexGrid
open global.Partas.Solid.FlexGrid

/// Theme toggle component with sun/moon icons
[<Erase>]
type ThemeToggle() =
    inherit button()

    [<Erase>]
    member val isDark: Accessor<bool> = Unchecked.defaultof<_> with get, set

    [<Erase>]
    member val onToggle: (unit -> unit) = Unchecked.defaultof<_> with get, set

    [<SolidTypeComponent>]
    member props.constructor =
        let titleText = if props.isDark() then "Switch to light mode" else "Switch to dark mode"
        button(
            class' = "p-2 rounded-lg bg-slate-200 dark:bg-speakez-neutral hover:bg-slate-300 dark:hover:bg-speakez-neutral/80 transition-colors text-xl",
            onClick = (fun _ -> props.onToggle()),
            title = titleText
        ) {
            // Sun emoji (shown in dark mode)
            Show(when' = props.isDark()) {
                span(class' = "select-none") { "\u2600\uFE0F" } // Sun emoji
            }
            // Moon emoji (shown in light mode)
            Show(when' = not (props.isDark())) {
                span(class' = "select-none") { "\uD83C\uDF19" } // Crescent moon emoji
            }
        }

/// SVG icon for side-by-side layout (two boxes next to each other)
[<Erase>]
type SideBySideIcon() =
    inherit span()

    [<SolidTypeComponent>]
    member props.constructor =
        span(class' = "inline-flex items-center justify-center w-5 h-5") {
            // Two rectangles side by side
            span(class' = "inline-flex gap-0.5") {
                span(class' = "w-2 h-3 border border-current rounded-sm")
                span(class' = "w-2 h-3 border border-current rounded-sm")
            }
        }

/// SVG icon for stacked layout (two boxes on top of each other)
[<Erase>]
type StackedIcon() =
    inherit span()

    [<SolidTypeComponent>]
    member props.constructor =
        span(class' = "inline-flex flex-col items-center justify-center w-5 h-5") {
            // Two rectangles stacked vertically
            span(class' = "inline-flex flex-col gap-0.5") {
                span(class' = "w-3 h-1.5 border border-current rounded-sm")
                span(class' = "w-3 h-1.5 border border-current rounded-sm")
            }
        }

/// Layout toggle component (stacked vs side-by-side)
[<Erase>]
type LayoutToggle() =
    inherit button()

    [<Erase>]
    member val isSideBySide: Accessor<bool> = Unchecked.defaultof<_> with get, set

    [<Erase>]
    member val onToggle: (unit -> unit) = Unchecked.defaultof<_> with get, set

    [<SolidTypeComponent>]
    member props.constructor =
        let titleText = if props.isSideBySide() then "Switch to stacked layout" else "Switch to side-by-side layout"
        button(
            class' = "p-2 rounded-lg bg-slate-200 dark:bg-speakez-neutral hover:bg-slate-300 dark:hover:bg-speakez-neutral/80 transition-colors text-sm font-medium text-speakez-neutral dark:text-speakez-neutral-light",
            onClick = (fun _ -> props.onToggle()),
            title = titleText
        ) {
            // Side-by-side icon (shown when stacked) - clicking switches TO side-by-side
            Show(when' = not (props.isSideBySide())) {
                span(class' = "select-none flex items-center gap-1.5") {
                    SideBySideIcon()
                    span(class' = "hidden sm:inline") { "Side-by-Side" }
                }
            }
            // Stacked icon (shown when side-by-side) - clicking switches TO stacked
            Show(when' = props.isSideBySide()) {
                span(class' = "select-none flex items-center gap-1.5") {
                    StackedIcon()
                    span(class' = "hidden sm:inline") { "Stacked" }
                }
            }
        }

/// Demo selector component
[<Erase>]
type DemoSelector() =
    inherit div()

    [<Erase>]
    member val onSelect: (string -> unit) = Unchecked.defaultof<_> with get, set

    [<SolidTypeComponent>]
    member props.constructor =
        div(class' = "mb-6 flex gap-4") {
            button(
                class' = "btn-speakez-teal",
                onClick = fun _ -> props.onSelect "compound"
            ) { "Loan Return" }
            button(
                class' = "btn-speakez-blue",
                onClick = fun _ -> props.onSelect "mortgage"
            ) { "Mortgage Calculator" }
        }

module App =
    open FlexGrid.Demos

    /// Import SolidJS render function
    [<Import("render", "solid-js/web")>]
    let render (app: unit -> HtmlElement) (container: Element) : unit = jsNative

    /// Import SolidJS createSignal
    [<Import("createSignal", "solid-js")>]
    let createSignal<'T> (initialValue: 'T) : Accessor<'T> * ('T -> unit) = jsNative

    /// Main application component
    [<SolidComponent>]
    let App () =
        let currentDemo, setCurrentDemo = createSignal "compound"

        // Check initial theme from localStorage or default to true (dark)
        let initialDark =
            let stored = window.localStorage.getItem("theme")
            stored <> "light"

        let isDark, setIsDark = createSignal initialDark

        // Check initial layout from localStorage or default to false (stacked)
        let initialSideBySide =
            let stored = window.localStorage.getItem("layout")
            stored = "side-by-side"

        let isSideBySide, setIsSideBySide = createSignal initialSideBySide

        let toggleTheme () =
            let newDark = not (isDark())
            setIsDark newDark
            if newDark then
                document.documentElement.classList.add("dark")
                window.localStorage.setItem("theme", "dark")
            else
                document.documentElement.classList.remove("dark")
                window.localStorage.setItem("theme", "light")

        let toggleLayout () =
            let newSideBySide = not (isSideBySide())
            setIsSideBySide newSideBySide
            if newSideBySide then
                window.localStorage.setItem("layout", "side-by-side")
            else
                window.localStorage.setItem("layout", "stacked")

        let getModel () =
            match currentDemo() with
            | "mortgage" -> MortgageCalculator.build()
            | _ -> CompoundInterest.build()

        div(class' = "min-h-screen bg-speakez-neutral-light dark:bg-speakez-neutral-dark py-8 transition-colors") {
            div(class' = "max-w-6xl mx-auto px-4") {
                // Header with controls
                div(class' = "mb-8 flex justify-between items-start") {
                    div() {
                        h1(class' = "text-3xl font-bold font-heading text-speakez-neutral dark:text-speakez-neutral-light mb-2") {
                            "FlexGrid Demo"
                        }
                        p(class' = "text-speakez-neutral/70 dark:text-speakez-neutral-light/70") {
                            "Reactive spreadsheets demonstrating functional programming principles"
                        }
                    }
                    div(class' = "flex gap-2") {
                        LayoutToggle(isSideBySide = isSideBySide, onToggle = toggleLayout)
                        ThemeToggle(isDark = isDark, onToggle = toggleTheme)
                    }
                }

                // Demo selector
                DemoSelector(onSelect = setCurrentDemo)

                // Description panel
                div(class' = "mb-6 p-4 card-speakez") {
                    h3(class' = "font-semibold font-heading mb-2 text-speakez-neutral dark:text-speakez-neutral-light") { "About this demo" }
                    p(class' = "text-sm text-speakez-neutral/70 dark:text-speakez-neutral-light/70") {
                        "Yellow cells are editable inputs. White cells show computed formulas. " +
                        "Hover over formula cells to see the underlying expression. " +
                        "Edit any input value and watch the dependent cells update reactively."
                    }
                }

                // Main content area - conditionally side-by-side or stacked
                Show(when' = isSideBySide()) {
                    // Side-by-side layout with equal height columns
                    div(class' = "grid grid-cols-1 lg:grid-cols-2 gap-6 items-stretch") {
                        // Spreadsheet
                        div(class' = "card-speakez h-fit") {
                            SpreadsheetRenderer.SpreadsheetApp (getModel())
                        }
                        // Calculation Log Panel - matches spreadsheet height
                        div(class' = "h-full") {
                            LiveLogPanel(initialOpen = true, sideBySide = true)
                        }
                    }
                }
                Show(when' = not (isSideBySide())) {
                    // Stacked layout
                    div() {
                        // Spreadsheet
                        div(class' = "card-speakez") {
                            SpreadsheetRenderer.SpreadsheetApp (getModel())
                        }
                        // Calculation Log Panel (accordion)
                        LiveLogPanel(initialOpen = true, sideBySide = false)
                    }
                }

                // Footer
                div(class' = "mt-8 text-center text-speakez-neutral/50 dark:text-speakez-neutral-light/50 text-sm") {
                    p() {
                        "FlexGrid - Reactive spreadsheets in F# with Partas.Solid, proving spreadsheets are functional programming"
                    }
                }
            }
        }

    // Application entry point
    let main () =
        let root = document.getElementById("app")
        if not (isNull root) then
            render (fun () -> App() :> HtmlElement) root

    // Auto-start the application
    main()
