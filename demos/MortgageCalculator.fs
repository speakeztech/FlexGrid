module FlexGrid.Demos.MortgageCalculator

open FlexGrid

/// Result type for split spreadsheets
type SplitSpreadsheet = {
    Model: ReactiveModel
    SplitAtRow: int
    ScrollableHeight: string
}

/// Build the mortgage calculator demonstration spreadsheet
/// Returns a SplitSpreadsheet with the split row for frozen/scrollable sections
let build () =
    let builder = reactiveSheet()

    // Title
    builder.Label("Mortgage Payment Calculator")
    builder.Skip(1)
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
    builder.Input("termYears", 30.0, format = "N0")  // Default to 30 years
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
    // Note: PMT returns negative (outflow), so we negate to show positive payment
    builder.Label("Monthly Payment")
    builder.Formula("=-PMT(annualRate/100/12, termYears*12, loan)", format = "C2")
    builder.NewRow()

    builder.Label("Total Payment")
    builder.Formula("=-PMT(annualRate/100/12, termYears*12, loan)*termYears*12", format = "C2")
    builder.NewRow()

    builder.Label("Total Interest")
    builder.Formula("=-PMT(annualRate/100/12, termYears*12, loan)*termYears*12-loan", format = "C2")
    builder.NewRow()

    // Amortization table header (row 12, 0-indexed)
    builder.Label("Month")
    builder.Label("Payment")
    builder.Label("Interest")
    builder.Label("Principal")
    builder.Label("Balance")
    builder.NewRow()

    // Record the split point - amortization data starts at row 13 (0-indexed)
    let splitAtRow = 13

    // Initial balance row (row 13) - shows starting loan amount
    builder.Label("-")
    builder.Label("-")
    builder.Label("-")
    builder.Label("-")
    builder.Formula("=loan", format = "C2")
    builder.NewRow()

    // Generate amortization rows
    // Use closed-form balance formula: Balance_n = P*(1+r)^n - PMT*((1+r)^n - 1)/r
    // Support up to 30 years (360 months) - rows beyond termYears*12 show as blank
    let maxMonths = 360  // 30 years max
    for month in 1 .. maxMonths do
        let r = "annualRate/100/12"
        let N = "termYears*12"
        let payment = $"-PMT({r}, {N}, loan)"

        // Closed-form balance after n payments:
        // B_n = P*(1+r)^n - PMT*((1+r)^n - 1)/r
        // where PMT = -PMT(...) (positive payment amount)
        let growthFactor = $"(1+{r})^{month}"
        let currBalance = $"loan*{growthFactor}-({payment})*({growthFactor}-1)/({r})"

        // Interest = previous balance * monthly rate
        // Previous balance = loan*(1+r)^(n-1) - PMT*((1+r)^(n-1) - 1)/r
        let prevGrowthFactor = if month = 1 then "1" else $"(1+{r})^{month-1}"
        let prevBalance =
            if month = 1 then "loan"
            else $"loan*{prevGrowthFactor}-({payment})*({prevGrowthFactor}-1)/({r})"
        let interest = $"({prevBalance})*({r})"
        // Principal = payment - interest
        let principal = $"({payment})-({interest})"

        // Use IF to show blank if month > termYears*12
        // Also use MAX(0, balance) to avoid small negative values due to floating point
        builder.Formula($"=IF({month}<=termYears*12,{month},BLANK())", format = "N0")
        builder.Formula($"=IF({month}<=termYears*12,{payment},BLANK())", format = "C2")
        builder.Formula($"=IF({month}<=termYears*12,{interest},BLANK())", format = "C2")
        builder.Formula($"=IF({month}<=termYears*12,{principal},BLANK())", format = "C2")
        builder.Formula($"=IF({month}<=termYears*12,MAX(0,{currBalance}),BLANK())", format = "C2")
        builder.NewRow()

    builder.Title("Mortgage Payment Calculator")
    // Show headers for consistency with Loan Return calculator
    builder.ShowHeaders(true)

    {
        Model = builder.Build()
        SplitAtRow = splitAtRow
        // Height for the entire scrollable container (frozen rows + scrollable rows)
        ScrollableHeight = "500px"
    }

/// Build a simple (non-split) version for backwards compatibility
let buildSimple () = (build()).Model

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
