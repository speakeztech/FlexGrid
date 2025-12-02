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
            class' = "p-2 rounded-lg bg-gray-200 dark:bg-gray-700 hover:bg-gray-300 dark:hover:bg-gray-600 transition-colors text-xl",
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
                class' = "px-4 py-2 bg-blue-500 text-white rounded hover:bg-blue-600 transition",
                onClick = fun _ -> props.onSelect "compound"
            ) { "Compound Interest" }
            button(
                class' = "px-4 py-2 bg-green-500 text-white rounded hover:bg-green-600 transition",
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

        let toggleTheme () =
            let newDark = not (isDark())
            setIsDark newDark
            if newDark then
                document.documentElement.classList.add("dark")
                window.localStorage.setItem("theme", "dark")
            else
                document.documentElement.classList.remove("dark")
                window.localStorage.setItem("theme", "light")

        let getModel () =
            match currentDemo() with
            | "mortgage" -> MortgageCalculator.build()
            | _ -> CompoundInterest.build()

        div(class' = "min-h-screen bg-gray-50 dark:bg-gray-900 py-8 transition-colors") {
            div(class' = "max-w-4xl mx-auto px-4") {
                // Header with theme toggle
                div(class' = "mb-8 flex justify-between items-start") {
                    div() {
                        h1(class' = "text-3xl font-bold text-gray-800 dark:text-gray-100 mb-2") {
                            "FlexGrid Demo"
                        }
                        p(class' = "text-gray-600 dark:text-gray-400") {
                            "Reactive spreadsheets demonstrating functional programming principles"
                        }
                    }
                    ThemeToggle(isDark = isDark, onToggle = toggleTheme)
                }

                // Demo selector
                DemoSelector(onSelect = setCurrentDemo)

                // Description panel
                div(class' = "mb-6 p-4 bg-white dark:bg-gray-800 rounded-lg shadow") {
                    h3(class' = "font-semibold mb-2 text-gray-800 dark:text-gray-100") { "About this demo" }
                    p(class' = "text-sm text-gray-600 dark:text-gray-400") {
                        "Yellow cells are editable inputs. White cells show computed formulas. " +
                        "Hover over formula cells to see the underlying expression. " +
                        "Edit any input value and watch the dependent cells update reactively."
                    }
                }

                // Spreadsheet
                div(class' = "bg-white dark:bg-gray-800 rounded-lg shadow overflow-hidden") {
                    SpreadsheetRenderer.SpreadsheetApp (getModel())
                }

                // Calculation Log Panel (accordion)
                LiveLogPanel()

                // F# Code comparison
                div(class' = "mt-8 p-4 bg-gray-800 dark:bg-gray-950 rounded-lg") {
                    h3(class' = "text-white font-semibold mb-4") {
                        "F# Equivalent"
                    }
                    pre(class' = "text-green-400 text-sm overflow-x-auto") {
                        Show(when' = (currentDemo() = "compound")) {
                            code() {
                                "let futureValue principal rate years =\n" +
                                "    principal * (1.0 + rate / 100.0) ** float years\n\n" +
                                "// Excel formula: =principal*(1+rate/100)^years\n" +
                                "// Same computation. Same semantics."
                            }
                        }
                        Show(when' = (currentDemo() = "mortgage")) {
                            code() {
                                "let monthlyPayment loan rate years =\n" +
                                "    let r = rate / 100.0 / 12.0\n" +
                                "    let n = float years * 12.0\n" +
                                "    loan * r * (1.0 + r) ** n / ((1.0 + r) ** n - 1.0)\n\n" +
                                "// Excel formula: =PMT(rate/12, years*12, loan)\n" +
                                "// Identical mathematics, different notation."
                            }
                        }
                    }
                }

                // Footer
                div(class' = "mt-8 text-center text-gray-500 dark:text-gray-500 text-sm") {
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
