module FlexGrid.Tests.ReactiveEngineTests

open Expecto
open FlexGrid

[<Tests>]
let basicEvaluationTests =
    testList "Basic Evaluation" [
        test "evaluates constant" {
            let registry = SignalRegistry.create()
            let result = ReactiveEngine.evaluateFormula registry "42"
            Expect.floatClose Accuracy.high result 42.0 "Should evaluate to 42"
        }

        test "evaluates addition" {
            let registry = SignalRegistry.create()
            let result = ReactiveEngine.evaluateFormula registry "1+2"
            Expect.floatClose Accuracy.high result 3.0 "Should evaluate to 3"
        }

        test "evaluates subtraction" {
            let registry = SignalRegistry.create()
            let result = ReactiveEngine.evaluateFormula registry "5-3"
            Expect.floatClose Accuracy.high result 2.0 "Should evaluate to 2"
        }

        test "evaluates multiplication" {
            let registry = SignalRegistry.create()
            let result = ReactiveEngine.evaluateFormula registry "4*3"
            Expect.floatClose Accuracy.high result 12.0 "Should evaluate to 12"
        }

        test "evaluates division" {
            let registry = SignalRegistry.create()
            let result = ReactiveEngine.evaluateFormula registry "10/4"
            Expect.floatClose Accuracy.high result 2.5 "Should evaluate to 2.5"
        }

        test "evaluates power" {
            let registry = SignalRegistry.create()
            let result = ReactiveEngine.evaluateFormula registry "2^3"
            Expect.floatClose Accuracy.high result 8.0 "Should evaluate to 8"
        }

        test "evaluates complex expression" {
            let registry = SignalRegistry.create()
            let result = ReactiveEngine.evaluateFormula registry "(1+2)*3"
            Expect.floatClose Accuracy.high result 9.0 "Should evaluate to 9"
        }

        test "handles division by zero" {
            let registry = SignalRegistry.create()
            let result = ReactiveEngine.evaluateFormula registry "1/0"
            Expect.isTrue (System.Double.IsNaN result || System.Double.IsInfinity result) "Should handle div by zero"
        }
    ]

[<Tests>]
let namedReferenceTests =
    testList "Named References" [
        test "evaluates named reference" {
            let registry = SignalRegistry.create()
            SignalRegistry.registerNamedSignal "x" (fun () -> 10.0) registry
            let result = ReactiveEngine.evaluateFormula registry "x+5"
            Expect.floatClose Accuracy.high result 15.0 "Should evaluate x+5"
        }

        test "evaluates multiple named references" {
            let registry = SignalRegistry.create()
            SignalRegistry.registerNamedSignal "a" (fun () -> 2.0) registry
            SignalRegistry.registerNamedSignal "b" (fun () -> 3.0) registry
            let result = ReactiveEngine.evaluateFormula registry "a*b"
            Expect.floatClose Accuracy.high result 6.0 "Should evaluate a*b"
        }

        test "evaluates compound interest formula" {
            let registry = SignalRegistry.create()
            SignalRegistry.registerNamedSignal "principal" (fun () -> 1000.0) registry
            SignalRegistry.registerNamedSignal "rate" (fun () -> 10.0) registry
            SignalRegistry.registerNamedSignal "years" (fun () -> 1.0) registry

            let result = ReactiveEngine.evaluateFormula registry "principal*(1+rate/100)^years"
            Expect.floatClose Accuracy.high result 1100.0 "Should calculate compound interest"
        }

        test "undefined named reference returns 0" {
            let registry = SignalRegistry.create()
            let result = ReactiveEngine.evaluateFormula registry "undefined+5"
            Expect.floatClose Accuracy.high result 5.0 "Undefined should be 0"
        }
    ]

[<Tests>]
let cellReferenceTests =
    testList "Cell References" [
        test "evaluates cell reference" {
            let registry = SignalRegistry.create()
            SignalRegistry.registerCellSignal 0 0 (fun () -> 42.0) registry
            let result = ReactiveEngine.evaluateFormula registry "A1"
            Expect.floatClose Accuracy.high result 42.0 "Should evaluate A1"
        }

        test "evaluates cell addition" {
            let registry = SignalRegistry.create()
            SignalRegistry.registerCellSignal 0 0 (fun () -> 10.0) registry
            SignalRegistry.registerCellSignal 1 0 (fun () -> 20.0) registry
            let result = ReactiveEngine.evaluateFormula registry "A1+B1"
            Expect.floatClose Accuracy.high result 30.0 "Should evaluate A1+B1"
        }

        test "undefined cell reference returns 0" {
            let registry = SignalRegistry.create()
            let result = ReactiveEngine.evaluateFormula registry "Z99+5"
            Expect.floatClose Accuracy.high result 5.0 "Undefined cell should be 0"
        }
    ]

[<Tests>]
let functionTests =
    testList "Functions" [
        test "SUM function" {
            let registry = SignalRegistry.create()
            let result = ReactiveEngine.evaluateFormula registry "SUM(1,2,3)"
            Expect.floatClose Accuracy.high result 6.0 "SUM should work"
        }

        test "AVERAGE function" {
            let registry = SignalRegistry.create()
            let result = ReactiveEngine.evaluateFormula registry "AVERAGE(2,4,6)"
            Expect.floatClose Accuracy.high result 4.0 "AVERAGE should work"
        }

        test "MAX function" {
            let registry = SignalRegistry.create()
            let result = ReactiveEngine.evaluateFormula registry "MAX(1,5,3)"
            Expect.floatClose Accuracy.high result 5.0 "MAX should work"
        }

        test "MIN function" {
            let registry = SignalRegistry.create()
            let result = ReactiveEngine.evaluateFormula registry "MIN(1,5,3)"
            Expect.floatClose Accuracy.high result 1.0 "MIN should work"
        }

        test "ABS function" {
            let registry = SignalRegistry.create()
            let result = ReactiveEngine.evaluateFormula registry "ABS(0-5)"
            Expect.floatClose Accuracy.high result 5.0 "ABS should work"
        }

        test "SQRT function" {
            let registry = SignalRegistry.create()
            let result = ReactiveEngine.evaluateFormula registry "SQRT(16)"
            Expect.floatClose Accuracy.high result 4.0 "SQRT should work"
        }

        test "ROUND function" {
            let registry = SignalRegistry.create()
            let result = ReactiveEngine.evaluateFormula registry "ROUND(3.456, 2)"
            Expect.floatClose Accuracy.high result 3.46 "ROUND should work"
        }

        test "IF function - true" {
            let registry = SignalRegistry.create()
            let result = ReactiveEngine.evaluateFormula registry "IF(1, 10, 20)"
            Expect.floatClose Accuracy.high result 10.0 "IF true should work"
        }

        test "IF function - false" {
            let registry = SignalRegistry.create()
            let result = ReactiveEngine.evaluateFormula registry "IF(0, 10, 20)"
            Expect.floatClose Accuracy.high result 20.0 "IF false should work"
        }

        test "POWER function" {
            let registry = SignalRegistry.create()
            let result = ReactiveEngine.evaluateFormula registry "POWER(2, 8)"
            Expect.floatClose Accuracy.high result 256.0 "POWER should work"
        }

        test "nested functions" {
            let registry = SignalRegistry.create()
            let result = ReactiveEngine.evaluateFormula registry "SQRT(SUM(9,16))"
            Expect.floatClose Accuracy.high result 5.0 "Nested functions should work"
        }
    ]

[<Tests>]
let comparisonTests =
    testList "Comparisons" [
        test "less than - true" {
            let registry = SignalRegistry.create()
            let result = ReactiveEngine.evaluateFormula registry "1<2"
            Expect.floatClose Accuracy.high result 1.0 "1<2 should be true (1)"
        }

        test "less than - false" {
            let registry = SignalRegistry.create()
            let result = ReactiveEngine.evaluateFormula registry "2<1"
            Expect.floatClose Accuracy.high result 0.0 "2<1 should be false (0)"
        }

        test "greater than" {
            let registry = SignalRegistry.create()
            let result = ReactiveEngine.evaluateFormula registry "5>3"
            Expect.floatClose Accuracy.high result 1.0 "5>3 should be true"
        }

        test "less than or equal" {
            let registry = SignalRegistry.create()
            let result = ReactiveEngine.evaluateFormula registry "3<=3"
            Expect.floatClose Accuracy.high result 1.0 "3<=3 should be true"
        }

        test "not equal" {
            let registry = SignalRegistry.create()
            let result = ReactiveEngine.evaluateFormula registry "1<>2"
            Expect.floatClose Accuracy.high result 1.0 "1<>2 should be true"
        }
    ]

[<Tests>]
let financialFunctionTests =
    testList "Financial Functions" [
        test "PMT function - basic loan" {
            let registry = SignalRegistry.create()
            // PMT(5%/12, 60, 10000) should be around $188.71
            let result = ReactiveEngine.evaluateFormula registry "PMT(0.05/12, 60, 10000)"
            // Using low precision as the actual value is ~-188.7123
            Expect.floatClose Accuracy.low result -188.71 "PMT should calculate monthly payment"
        }

        test "FV function - simple growth" {
            let registry = SignalRegistry.create()
            // FV(10%, 1, 0, 1000) = -1100 (future value of $1000 at 10% after 1 year)
            let result = ReactiveEngine.evaluateFormula registry "FV(0.1, 1, 0, 1000)"
            Expect.floatClose Accuracy.medium result -1100.0 "FV should calculate future value"
        }
    ]
