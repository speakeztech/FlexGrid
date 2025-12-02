namespace FlexGrid.Solid

open Fable.Core
open Fable.Core.JsInterop
open Partas.Solid
open FlexGrid

/// Module for rendering ReactiveModel to Partas.Solid components
module Render =

    /// Import SolidJS createSignal
    [<Import("createSignal", "solid-js")>]
    let createSignal<'T> (initialValue: 'T) : Accessor<'T> * ('T -> unit) = jsNative

    /// Build the signal registry from a reactive model
    let buildRegistry (model: ReactiveModel) : SignalRegistry =
        let registry = SignalRegistry.create()

        // First pass: create signals for all input cells
        for posCell in model.Cells do
            match posCell.Cell with
            | ReactiveCell.Input(name, initial, _) ->
                let signal, setter = createSignal initial
                let col, row = posCell.Position.Col, posCell.Position.Row
                SignalRegistry.registerCellSignal col row signal registry
                SignalRegistry.registerCellSetter col row setter registry
                SignalRegistry.registerNamedSignal name signal registry
            | _ -> ()

        // Second pass: create derived signals for formula cells
        for posCell in model.Cells do
            match posCell.Cell with
            | ReactiveCell.Formula(expr, _) ->
                let col, row = posCell.Position.Col, posCell.Position.Row
                let formulaSignal = ReactiveEngine.createFormulaSignal registry expr
                SignalRegistry.registerCellSignal col row formulaSignal registry
            | _ -> ()

        registry

    /// Create the cell component for a given position
    let createCellComponent
        (model: ReactiveModel)
        (registry: SignalRegistry)
        (row: int)
        (col: int) : HtmlElement =

        let maybeCell = ReactiveModel.tryGetCell row col model

        match maybeCell with
        | Some (ReactiveCell.Input(name, _, format)) ->
            match SignalRegistry.tryGetCellSignal col row registry,
                  SignalRegistry.tryGetCellSetter col row registry with
            | Some signal, Some setter ->
                InputCell(
                    signal = signal,
                    setSignal = setter,
                    format = format,
                    name = name
                ) :> HtmlElement
            | _ ->
                EmptyCell() :> HtmlElement

        | Some (ReactiveCell.Formula(expr, format)) ->
            match SignalRegistry.tryGetCellSignal col row registry with
            | Some signal ->
                FormulaCell(
                    signal = signal,
                    format = format,
                    formula = expr
                ) :> HtmlElement
            | None ->
                EmptyCell() :> HtmlElement

        | Some (ReactiveCell.Label text) ->
            LabelCell(text = text) :> HtmlElement

        | Some ReactiveCell.Empty ->
            EmptyCell() :> HtmlElement

        | None ->
            EmptyCell() :> HtmlElement

    /// Render a ReactiveModel as a Partas.Solid spreadsheet component
    [<SolidComponent>]
    let SpreadsheetApp (model: ReactiveModel) =
        let registry = buildRegistry model
        let rows, cols = ReactiveModel.dimensions model

        let getCellComponent row col =
            createCellComponent model registry row col

        Spreadsheet(
            title = model.Title,
            rows = rows,
            cols = cols,
            getCellComponent = getCellComponent,
            showHeaders = model.ShowHeaders
        )

    /// Create a standalone app component with mount logic
    [<SolidComponent>]
    let App (model: ReactiveModel) =
        div(class' = "min-h-screen bg-gray-50 py-8") {
            div(class' = "max-w-4xl mx-auto") {
                SpreadsheetApp model
            }
        }
