module FlexGrid.Tests.FormulaParserTests

open Expecto
open FlexGrid
open FlexGrid.FormulaParser

[<Tests>]
let columnConversionTests =
    testList "Column Conversion" [
        test "A converts to 0" {
            Expect.equal (columnLettersToIndex "A") 0 "A should be 0"
        }

        test "Z converts to 25" {
            Expect.equal (columnLettersToIndex "Z") 25 "Z should be 25"
        }

        test "AA converts to 26" {
            Expect.equal (columnLettersToIndex "AA") 26 "AA should be 26"
        }

        test "AB converts to 27" {
            Expect.equal (columnLettersToIndex "AB") 27 "AB should be 27"
        }

        test "AZ converts to 51" {
            Expect.equal (columnLettersToIndex "AZ") 51 "AZ should be 51"
        }

        test "BA converts to 52" {
            Expect.equal (columnLettersToIndex "BA") 52 "BA should be 52"
        }

        test "index to letters roundtrip" {
            for i in 0 .. 100 do
                let letters = indexToColumnLetters i
                let backToIndex = columnLettersToIndex letters
                Expect.equal backToIndex i $"Roundtrip failed for {i}"
        }
    ]

[<Tests>]
let cellReferenceTests =
    testList "Cell Reference Parsing" [
        test "parses A1" {
            match "A1" with
            | CellReference (col, row) ->
                Expect.equal col 0 "Column should be 0"
                Expect.equal row 0 "Row should be 0"
            | _ -> failtest "Should parse A1"
        }

        test "parses B2" {
            match "B2" with
            | CellReference (col, row) ->
                Expect.equal col 1 "Column should be 1"
                Expect.equal row 1 "Row should be 1"
            | _ -> failtest "Should parse B2"
        }

        test "parses AA10" {
            match "AA10" with
            | CellReference (col, row) ->
                Expect.equal col 26 "Column should be 26"
                Expect.equal row 9 "Row should be 9"
            | _ -> failtest "Should parse AA10"
        }

        test "rejects invalid reference" {
            match "123" with
            | CellReference _ -> failtest "Should not parse numbers"
            | _ -> ()
        }
    ]

[<Tests>]
let tokenizeTests =
    testList "Tokenization" [
        test "tokenizes simple expression" {
            let tokens = tokenize "A1+B1"
            Expect.equal tokens ["A1"; "+"; "B1"] "Should tokenize correctly"
        }

        test "tokenizes complex expression" {
            let tokens = tokenize "A1*(B1+C1)/2"
            Expect.equal tokens ["A1"; "*"; "("; "B1"; "+"; "C1"; ")"; "/"; "2"] "Should tokenize correctly"
        }

        test "tokenizes function call" {
            let tokens = tokenize "SUM(A1,B1,C1)"
            Expect.equal tokens ["SUM"; "("; "A1"; ","; "B1"; ","; "C1"; ")"] "Should tokenize correctly"
        }

        test "tokenizes named references" {
            let tokens = tokenize "principal*(1+rate/100)^years"
            Expect.contains tokens "principal" "Should contain principal"
            Expect.contains tokens "rate" "Should contain rate"
            Expect.contains tokens "years" "Should contain years"
        }
    ]

[<Tests>]
let parseTests =
    testList "Expression Parsing" [
        test "parses number" {
            let expr = parse "42"
            match expr with
            | Expr.Number n -> Expect.equal n 42.0 "Should parse 42"
            | _ -> failtest "Should be a number"
        }

        test "parses decimal" {
            let expr = parse "3.14"
            match expr with
            | Expr.Number n -> Expect.floatClose Accuracy.high n 3.14 "Should parse 3.14"
            | _ -> failtest "Should be a number"
        }

        test "parses cell reference" {
            let expr = parse "A1"
            match expr with
            | Expr.CellRef (col, row) ->
                Expect.equal col 0 "Column should be 0"
                Expect.equal row 0 "Row should be 0"
            | _ -> failtest "Should be a cell reference"
        }

        test "parses named reference" {
            let expr = parse "principal"
            match expr with
            | Expr.NamedRef name -> Expect.equal name "principal" "Should be 'principal'"
            | _ -> failtest "Should be a named reference"
        }

        test "parses addition" {
            let expr = parse "A1+B1"
            match expr with
            | Expr.BinaryOp (_, "+", _) -> ()
            | _ -> failtest "Should be addition"
        }

        test "parses multiplication" {
            let expr = parse "A1*B1"
            match expr with
            | Expr.BinaryOp (_, "*", _) -> ()
            | _ -> failtest "Should be multiplication"
        }

        test "parses power" {
            let expr = parse "2^3"
            match expr with
            | Expr.BinaryOp (_, "^", _) -> ()
            | _ -> failtest "Should be power"
        }

        test "parses parentheses" {
            let expr = parse "(A1+B1)*C1"
            match expr with
            | Expr.BinaryOp (Expr.Parenthesized _, "*", _) -> ()
            | _ -> failtest "Should parse parentheses correctly"
        }

        test "parses function call" {
            let expr = parse "SUM(A1,B1)"
            match expr with
            | Expr.FunctionCall ("SUM", args) ->
                Expect.equal (List.length args) 2 "Should have 2 args"
            | _ -> failtest "Should be a function call"
        }

        test "parses compound interest formula" {
            let expr = parse "principal*(1+rate/100)^years"
            let namedRefs = extractNamedReferences expr
            Expect.containsAll namedRefs ["principal"; "rate"; "years"] "Should contain all named refs"
        }

        test "strips leading equals" {
            let expr = parse "=A1+B1"
            match expr with
            | Expr.BinaryOp (_, "+", _) -> ()
            | _ -> failtest "Should parse with leading ="
        }

        test "handles operator precedence" {
            let expr = parse "1+2*3"
            // Should be 1+(2*3), not (1+2)*3
            match expr with
            | Expr.BinaryOp (Expr.Number 1.0, "+", Expr.BinaryOp (Expr.Number 2.0, "*", Expr.Number 3.0)) -> ()
            | _ -> failtest "Should respect precedence"
        }
    ]

[<Tests>]
let extractionTests =
    testList "Reference Extraction" [
        test "extracts cell references" {
            let expr = parse "A1+B2+C3"
            let refs = extractCellReferences expr
            Expect.equal (List.length refs) 3 "Should find 3 cell refs"
            Expect.containsAll refs [(0, 0); (1, 1); (2, 2)] "Should contain all refs"
        }

        test "extracts named references" {
            let expr = parse "principal*(1+rate/100)^years"
            let names = extractNamedReferences expr
            Expect.containsAll names ["principal"; "rate"; "years"] "Should contain all names"
        }

        test "handles mixed references" {
            let expr = parse "A1+principal"
            let cellRefs = extractCellReferences expr
            let namedRefs = extractNamedReferences expr
            Expect.equal (List.length cellRefs) 1 "Should find 1 cell ref"
            Expect.equal (List.length namedRefs) 1 "Should find 1 named ref"
        }
    ]
