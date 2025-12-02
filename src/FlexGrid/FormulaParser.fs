namespace FlexGrid

open System
open System.Text.RegularExpressions

/// Represents a parsed formula expression
[<RequireQualifiedAccess>]
type Expr =
    /// A numeric literal
    | Number of float
    /// A cell reference like A1, B2
    | CellRef of col: int * row: int
    /// A named reference like "principal"
    | NamedRef of string
    /// A binary operation
    | BinaryOp of Expr * string * Expr
    /// A function call like SUM(A1, A2)
    | FunctionCall of name: string * args: Expr list
    /// A parenthesized expression
    | Parenthesized of Expr

module FormulaParser =

    /// Convert column letters (A, B, ..., Z, AA, AB, ...) to zero-based index
    let columnLettersToIndex (letters: string) =
        letters.ToUpperInvariant()
        |> Seq.fold (fun acc c -> acc * 26 + (int c - int 'A' + 1)) 0
        |> fun x -> x - 1

    /// Convert zero-based column index to letters
    let indexToColumnLetters (index: int) =
        let rec loop n acc =
            if n < 0 then acc
            else
                let letter = char (int 'A' + (n % 26))
                loop (n / 26 - 1) (string letter + acc)
        loop index ""

    /// Try to parse a cell reference like "A1" or "BC42"
    let (|CellReference|_|) (s: string) =
        let m = Regex.Match(s, @"^([A-Z]+)(\d+)$", RegexOptions.IgnoreCase)
        if m.Success then
            let col = columnLettersToIndex m.Groups.[1].Value
            let row = int m.Groups.[2].Value - 1  // Convert to zero-based
            Some (col, row)
        else None

    /// Tokenize a formula string
    let tokenize (formula: string) : string list =
        // Multi-char operators must come before single-char ones in the pattern
        let pattern = @"([A-Za-z_][A-Za-z0-9_]*|\d+\.?\d*|<=|>=|<>|[+\-*/^()=,<>])"
        Regex.Matches(formula, pattern)
        |> Seq.cast<Match>
        |> Seq.map (fun m -> m.Value)
        |> Seq.toList

    /// Recursive descent parser for formula expressions
    let rec parseExpr tokens =
        parseComparison tokens

    and parseComparison tokens =
        let left, remaining = parseAdditive tokens
        match remaining with
        | "<" :: rest ->
            let right, final = parseComparison rest
            Expr.BinaryOp(left, "<", right), final
        | ">" :: rest ->
            let right, final = parseComparison rest
            Expr.BinaryOp(left, ">", right), final
        | "<=" :: rest ->
            let right, final = parseComparison rest
            Expr.BinaryOp(left, "<=", right), final
        | ">=" :: rest ->
            let right, final = parseComparison rest
            Expr.BinaryOp(left, ">=", right), final
        | "<>" :: rest ->
            let right, final = parseComparison rest
            Expr.BinaryOp(left, "<>", right), final
        | _ -> left, remaining

    and parseAdditive tokens =
        let left, remaining = parseMultiplicative tokens
        match remaining with
        | "+" :: rest ->
            let right, final = parseAdditive rest
            Expr.BinaryOp(left, "+", right), final
        | "-" :: rest ->
            let right, final = parseAdditive rest
            Expr.BinaryOp(left, "-", right), final
        | _ -> left, remaining

    and parseMultiplicative tokens =
        let left, remaining = parsePower tokens
        match remaining with
        | "*" :: rest ->
            let right, final = parseMultiplicative rest
            Expr.BinaryOp(left, "*", right), final
        | "/" :: rest ->
            let right, final = parseMultiplicative rest
            Expr.BinaryOp(left, "/", right), final
        | _ -> left, remaining

    and parsePower tokens =
        let left, remaining = parseUnary tokens
        match remaining with
        | "^" :: rest ->
            let right, final = parsePower rest
            Expr.BinaryOp(left, "^", right), final
        | _ -> left, remaining

    and parseUnary tokens =
        match tokens with
        | "-" :: rest ->
            let expr, remaining = parsePrimary rest
            Expr.BinaryOp(Expr.Number 0.0, "-", expr), remaining
        | _ -> parsePrimary tokens

    and parsePrimary tokens =
        match tokens with
        | "(" :: rest ->
            let expr, afterExpr = parseExpr rest
            match afterExpr with
            | ")" :: final -> Expr.Parenthesized expr, final
            | _ -> failwith "Expected closing parenthesis"

        | token :: "(" :: rest when Regex.IsMatch(token, @"^[A-Za-z_][A-Za-z0-9_]*$") ->
            // Function call
            let args, afterArgs = parseArguments rest
            Expr.FunctionCall(token.ToUpperInvariant(), args), afterArgs

        | CellReference (col, row) :: rest ->
            Expr.CellRef(col, row), rest

        | token :: rest when Regex.IsMatch(token, @"^\d+\.?\d*$") ->
            Expr.Number(float token), rest

        | token :: rest when Regex.IsMatch(token, @"^[A-Za-z_][A-Za-z0-9_]*$") ->
            Expr.NamedRef(token), rest

        | [] -> failwith "Unexpected end of formula"
        | token :: _ -> failwith $"Unexpected token: {token}"

    and parseArguments tokens =
        match tokens with
        | ")" :: rest -> [], rest
        | _ ->
            let rec loop acc tokens =
                let expr, remaining = parseExpr tokens
                match remaining with
                | ")" :: rest -> List.rev (expr :: acc), rest
                | "," :: rest -> loop (expr :: acc) rest
                | _ -> failwith "Expected , or ) in function arguments"
            loop [] tokens

    /// Parse a formula string (with or without leading =)
    let parse (formula: string) : Expr =
        let cleaned = formula.TrimStart('=').Trim()
        let tokens = tokenize cleaned
        if List.isEmpty tokens then
            Expr.Number 0.0
        else
            let expr, remaining = parseExpr tokens
            if remaining <> [] then
                failwith $"Unexpected tokens remaining: {remaining}"
            expr

    /// Try to parse a formula, returning None on failure
    let tryParse (formula: string) : Expr option =
        try Some (parse formula)
        with _ -> None

    /// Extract all cell references from an expression
    let rec extractCellReferences expr =
        match expr with
        | Expr.Number _ -> []
        | Expr.CellRef (col, row) -> [(col, row)]
        | Expr.NamedRef _ -> []
        | Expr.BinaryOp (left, _, right) ->
            extractCellReferences left @ extractCellReferences right
        | Expr.FunctionCall (_, args) ->
            args |> List.collect extractCellReferences
        | Expr.Parenthesized inner ->
            extractCellReferences inner

    /// Extract all named references from an expression
    let rec extractNamedReferences expr =
        match expr with
        | Expr.Number _ -> []
        | Expr.CellRef _ -> []
        | Expr.NamedRef name -> [name]
        | Expr.BinaryOp (left, _, right) ->
            extractNamedReferences left @ extractNamedReferences right
        | Expr.FunctionCall (_, args) ->
            args |> List.collect extractNamedReferences
        | Expr.Parenthesized inner ->
            extractNamedReferences inner
