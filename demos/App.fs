module FlexGrid.Demos.App

open Fable.Core
open Fable.Core.JsInterop
open Browser.Dom
open Partas.Solid
open FlexGrid
open FlexGrid.Solid

/// Import SolidJS render function
[<Import("render", "solid-js/web")>]
let render (app: unit -> HtmlElement) (container: Browser.Types.Element) : unit = jsNative

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

/// Main application component
[<SolidComponent>]
let App () =
    let currentDemo, setCurrentDemo = Render.createSignal "compound"

    let getModel () =
        match currentDemo() with
        | "mortgage" -> MortgageCalculator.build()
        | _ -> CompoundInterest.build()

    div(class' = "min-h-screen bg-gray-50 py-8") {
        div(class' = "max-w-4xl mx-auto px-4") {
            // Header
            div(class' = "mb-8") {
                h1(class' = "text-3xl font-bold text-gray-800 mb-2") {
                    "FlexGrid Demo"
                }
                p(class' = "text-gray-600") {
                    "Reactive spreadsheets demonstrating functional programming principles"
                }
            }

            // Demo selector
            DemoSelector(onSelect = setCurrentDemo)

            // Description panel
            div(class' = "mb-6 p-4 bg-white rounded-lg shadow") {
                h3(class' = "font-semibold mb-2") { "About this demo" }
                p(class' = "text-sm text-gray-600") {
                    "Yellow cells are editable inputs. White cells show computed formulas. " +
                    "Hover over formula cells to see the underlying expression. " +
                    "Edit any input value and watch the dependent cells update reactively."
                }
            }

            // Spreadsheet
            div(class' = "bg-white rounded-lg shadow overflow-hidden") {
                Render.SpreadsheetApp (getModel())
            }

            // F# Code comparison
            div(class' = "mt-8 p-4 bg-gray-800 rounded-lg") {
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
            div(class' = "mt-8 text-center text-gray-500 text-sm") {
                p() {
                    "FlexGrid - Bridging FsExcel with Partas.Solid for reactive spreadsheet demonstrations"
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
