namespace Partas.Solid.FlexGrid

open Fable.Core
open Fable.Core.JsInterop
open global.Partas.Solid
open global.FlexGrid

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

    /// Cell data for rendering - can be passed to the CellRenderer component
    type CellData =
        | InputData of signal: Accessor<float> * setter: (float -> unit) * format: string option * name: string
        | FormulaData of signal: Accessor<float> * format: string option * formula: string
        | LabelData of text: string
        | EmptyData

    /// Get the cell data for a given position
    let getCellData
        (model: ReactiveModel)
        (registry: SignalRegistry)
        (row: int)
        (col: int) : CellData =

        let maybeCell = ReactiveModel.tryGetCell row col model

        match maybeCell with
        | Some (ReactiveCell.Input(name, _, format)) ->
            match SignalRegistry.tryGetCellSignal col row registry,
                  SignalRegistry.tryGetCellSetter col row registry with
            | Some signal, Some setter ->
                InputData(signal, setter, format, name)
            | _ ->
                EmptyData

        | Some (ReactiveCell.Formula(expr, format)) ->
            match SignalRegistry.tryGetCellSignal col row registry with
            | Some signal ->
                FormulaData(signal, format, expr)
            | None ->
                EmptyData

        | Some (ReactiveCell.Label text) ->
            LabelData text

        | Some ReactiveCell.Empty ->
            EmptyData

        | None ->
            EmptyData

/// Cell renderer component - renders the appropriate cell type based on data
[<Erase>]
type CellRenderer() =
    inherit td()

    [<Erase>]
    member val cellData: Render.CellData = Render.EmptyData with get, set

    [<SolidTypeComponent>]
    member props.constructor =
        match props.cellData with
        | Render.InputData(signal, setter, format, name) ->
            InputCell(
                signal = signal,
                setSignal = setter,
                format = format,
                name = name
            ) :> HtmlElement
        | Render.FormulaData(signal, format, formula) ->
            FormulaCell(
                signal = signal,
                format = format,
                formula = formula
            ) :> HtmlElement
        | Render.LabelData text ->
            LabelCell(text = text) :> HtmlElement
        | Render.EmptyData ->
            EmptyCell() :> HtmlElement

module Render2 =
    open Render

    /// Render a ReactiveModel as a Partas.Solid spreadsheet component
    [<SolidComponent>]
    let SpreadsheetApp (model: ReactiveModel) =
        let registry = buildRegistry model
        let rows, cols = ReactiveModel.dimensions model

        let getCellComponent row col =
            let cellData = getCellData model registry row col
            CellRenderer(cellData = cellData) :> HtmlElement

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
