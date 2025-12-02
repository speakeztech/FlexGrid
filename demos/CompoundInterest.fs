module FlexGrid.Demos.CompoundInterest

open FlexGrid

/// Build the loan return (investment growth) demonstration spreadsheet
let build () =
    let builder = reactiveSheet()

    // Title area
    builder.Label("Loan Return Calculator")
    builder.Skip(2)
    builder.NewRow()
    builder.NewRow()

    // Input section
    builder.Label("Principal ($)")
    builder.Input("principal", 10000.0, format = "N0")
    builder.NewRow()

    builder.Label("Annual Rate (%)")
    builder.Input("rate", 7.0, format = "N2")
    builder.NewRow()

    builder.Label("Years")
    builder.Input("years", 10.0, format = "N0")
    builder.NewRow()
    builder.NewRow()

    // Result section
    builder.Label("Total Return")
    builder.Formula("=principal*(1+rate/100)^years", format = "C2")
    builder.NewRow()
    builder.NewRow()

    // Year-by-year breakdown header
    builder.Label("Year")
    builder.Label("Value")
    builder.Label("Interest Earned")
    builder.NewRow()

    // Generate rows for years 1-10 (conditionally shown based on years input)
    for year in 1 .. 10 do
        // Year label - show blank if beyond selected years
        builder.Formula($"=IF({year}<=years,{year},BLANK())", format = "N0")
        // Value - show blank if beyond selected years
        builder.Formula($"=IF({year}<=years,principal*(1+rate/100)^{year},BLANK())", format = "C2")
        // Interest earned - show blank if beyond selected years
        if year = 1 then
            builder.Formula($"=IF({year}<=years,principal*(1+rate/100)^{year}-principal,BLANK())", format = "C2")
        else
            builder.Formula($"=IF({year}<=years,principal*(1+rate/100)^{year}-principal*(1+rate/100)^{year-1},BLANK())", format = "C2")
        builder.NewRow()

    builder.Title("Loan Return Calculator")
    builder.Build()

/// Pure F# equivalent functions for side-by-side comparison
module FSharpEquivalent =

    /// Calculate future value given principal, rate (as percentage), and years
    let futureValue principal rate years =
        principal * (1.0 + rate / 100.0) ** float years

    /// Generate yearly breakdown
    let yearlyBreakdown principal rate years =
        [ for year in 1 .. years ->
            let balance = futureValue principal rate year
            let prevBalance =
                if year = 1 then principal
                else futureValue principal rate (year - 1)
            let interest = balance - prevBalance
            {| Year = year; Balance = balance; Interest = interest |} ]
