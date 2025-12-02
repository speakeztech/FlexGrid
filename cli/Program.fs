module FlexGrid.CLI.Program

open System
open System.IO
open System.Net.Http
open System.Net.Http.Headers
open Argu
open Spectre.Console

type DeployArgs =
    | [<AltCommandLine("-d")>] Directory of string
    | [<AltCommandLine("-p")>] Project of string
    | [<AltCommandLine("-v")>] Verbose
    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Directory _ -> "Directory to deploy (defaults to ./dist)"
            | Project _ -> "Cloudflare Pages project name (defaults to 'flexgrid-demo')"
            | Verbose -> "Verbose output"

type CliCommand =
    | [<CliPrefix(CliPrefix.None)>] Deploy of ParseResults<DeployArgs>
    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Deploy _ -> "Deploy the built site to Cloudflare Pages"

[<EntryPoint>]
let main argv =
    try
        let parser = ArgumentParser.Create<CliCommand>(programName = "flexgrid-cli")
        let results = parser.ParseCommandLine(argv)

        // Check for required environment variables
        let apiToken = Environment.GetEnvironmentVariable("CLOUDFLARE_API_TOKEN")
        let accountId = Environment.GetEnvironmentVariable("CLOUDFLARE_ACCOUNT_ID")

        if String.IsNullOrEmpty(apiToken) then
            AnsiConsole.MarkupLine("[red]Error:[/] CLOUDFLARE_API_TOKEN environment variable not set")
            exit 1

        if String.IsNullOrEmpty(accountId) then
            AnsiConsole.MarkupLine("[red]Error:[/] CLOUDFLARE_ACCOUNT_ID environment variable not set")
            exit 1

        match results.GetSubCommand() with
        | Deploy args ->
            let directory = args.TryGetResult DeployArgs.Directory |> Option.defaultValue "./dist"
            let projectName = args.TryGetResult DeployArgs.Project |> Option.defaultValue "flexgrid-demo"
            let verbose = args.Contains DeployArgs.Verbose

            // Verify directory exists
            let fullPath = Path.GetFullPath(directory)
            if not (Directory.Exists(fullPath)) then
                AnsiConsole.MarkupLine($"[red]Error:[/] Directory not found: {Markup.Escape(fullPath)}")
                AnsiConsole.MarkupLine("")
                AnsiConsole.MarkupLine("Run [cyan]npm run build[/] first to create the dist directory.")
                exit 1

            AnsiConsole.MarkupLine($"[cyan]FlexGrid Pages Deployment[/]")
            AnsiConsole.MarkupLine("")
            AnsiConsole.MarkupLine($"  Directory: {fullPath}")
            AnsiConsole.MarkupLine($"  Project: {projectName}")
            AnsiConsole.MarkupLine("")

            // Create HTTP client with auth
            use httpClient = new HttpClient()
            httpClient.DefaultRequestHeaders.Authorization <- AuthenticationHeaderValue("Bearer", apiToken)

            let pages = PagesUploader.PagesOperations(httpClient, accountId)

            // Check if project exists, create if not
            AnsiConsole.MarkupLine("  [[1/2]] Checking project...")
            let exists = pages.ProjectExists(projectName) |> Async.RunSynchronously
            if not exists then
                AnsiConsole.MarkupLine($"        Creating project [cyan]{projectName}[/]...")
                match pages.CreateProject(projectName, "main") |> Async.RunSynchronously with
                | Error e ->
                    AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(e)}")
                    exit 1
                | Ok () ->
                    AnsiConsole.MarkupLine("        Project created")
            else
                if verbose then AnsiConsole.MarkupLine("        Project exists")

            // Deploy
            AnsiConsole.MarkupLine("  [[2/2]] Deploying...")
            let progressCallback msg =
                AnsiConsole.MarkupLine($"        {Markup.Escape(msg)}")

            let result =
                pages.DeployDirectory projectName fullPath verbose progressCallback
                |> Async.RunSynchronously

            match result with
            | Error e ->
                AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(e)}")
                exit 1
            | Ok url ->
                AnsiConsole.MarkupLine("")
                AnsiConsole.MarkupLine("[green]Deployment complete![/]")
                AnsiConsole.MarkupLine("")
                AnsiConsole.MarkupLine($"  Site URL: [cyan]https://{projectName}.pages.dev[/]")
                if url <> "Deployment created successfully" then
                    AnsiConsole.MarkupLine($"  Preview: [cyan]{url}[/]")
                0

    with
    | :? ArguParseException as ex ->
        printfn "%s" ex.Message
        1
    | ex ->
        AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}")
        if ex.InnerException <> null then
            AnsiConsole.MarkupLine($"[red]Inner:[/] {Markup.Escape(ex.InnerException.Message)}")
        1
