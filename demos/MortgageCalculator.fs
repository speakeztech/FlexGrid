module FlexGrid.Demos.MortgageCalculator

open FlexGrid

/// Build the mortgage calculator demonstration spreadsheet
let build () =
    let builder = reactiveSheet()

    // Title
    builder.Label("Mortgage Payment Calculator")
    builder.Skip(2)
    builder.NewRow()
    builder.NewRow()

    // Loan details section
    builder.Label("Loan Amount ($)")
    builder.Input("loan", 300000.0, format = "N0")
    builder.NewRow()

    builder.Label("Annual Interest Rate (%)")
    builder.Input("annualRate", 6.5, format = "N2")
    builder.NewRow()

    builder.Label("Loan Term (Years)")
    builder.Input("termYears", 30.0, format = "N0")
    builder.NewRow()
    builder.NewRow()

    // Calculated values section
    builder.Label("Monthly Interest Rate")
    builder.Formula("=annualRate/100/12", format = "P4")
    builder.NewRow()

    builder.Label("Number of Payments")
    builder.Formula("=termYears*12", format = "N0")
    builder.NewRow()
    builder.NewRow()

    // Results section
    builder.Label("Monthly Payment")
    builder.Formula("=PMT(annualRate/100/12, termYears*12, loan)", format = "C2")
    builder.NewRow()

    builder.Label("Total Payment")
    builder.Formula("=PMT(annualRate/100/12, termYears*12, loan)*termYears*12", format = "C2")
    builder.NewRow()

    builder.Label("Total Interest")
    builder.Formula("=PMT(annualRate/100/12, termYears*12, loan)*termYears*12+loan", format = "C2")
    builder.NewRow()
    builder.NewRow()

    // Amortization preview (first 12 months)
    builder.Label("Month")
    builder.Label("Payment")
    builder.Label("Principal")
    builder.Label("Interest")
    builder.Label("Balance")
    builder.NewRow()

    // Initial balance row
    builder.Label("0")
    builder.Label("-")
    builder.Label("-")
    builder.Label("-")
    builder.Formula("=loan", format = "C2")
    builder.NewRow()

    // First 6 months of amortization (simplified)
    for month in 1 .. 6 do
        builder.Label(string month)
        builder.Formula("=-PMT(annualRate/100/12, termYears*12, loan)", format = "C2")
        // Interest = previous balance * monthly rate
        builder.Formula($"=loan*(annualRate/100/12)", format = "C2") // Simplified
        // Principal = payment - interest
        builder.Formula($"=-PMT(annualRate/100/12, termYears*12, loan)-loan*(annualRate/100/12)", format = "C2")
        // Remaining balance (simplified - not cumulative for demo)
        builder.Formula($"=loan-{month}*(-PMT(annualRate/100/12, termYears*12, loan)-loan*(annualRate/100/12))", format = "C2")
        builder.NewRow()

    builder.Title("Mortgage Payment Calculator")
    builder.Build()

/// Pure F# equivalent functions
module FSharpEquivalent =

    /// Calculate monthly payment using PMT formula
    let monthlyPayment loanAmount annualRate termYears =
        let monthlyRate = annualRate / 100.0 / 12.0
        let numPayments = float termYears * 12.0
        if monthlyRate = 0.0 then
            loanAmount / numPayments
        else
            loanAmount * monthlyRate * (1.0 + monthlyRate) ** numPayments /
            ((1.0 + monthlyRate) ** numPayments - 1.0)

    /// Generate amortization schedule
    let amortizationSchedule loanAmount annualRate termYears =
        let payment = monthlyPayment loanAmount annualRate termYears
        let monthlyRate = annualRate / 100.0 / 12.0

        let rec generate month balance acc =
            if month > termYears * 12 || balance <= 0.0 then
                List.rev acc
            else
                let interest = balance * monthlyRate
                let principal = payment - interest
                let newBalance = max 0.0 (balance - principal)
                let row = {|
                    Month = month
                    Payment = payment
                    Principal = principal
                    Interest = interest
                    Balance = newBalance
                |}
                generate (month + 1) newBalance (row :: acc)

        generate 1 loanAmount []
