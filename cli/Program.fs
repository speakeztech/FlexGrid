module FlexGrid.CLI.Program

open System
open System.Diagnostics
open System.IO
open System.Net.Http
open System.Net.Http.Headers
open Argu
open Spectre.Console

type DeployArgs =
    | [<AltCommandLine("-p")>] Project of string
    | [<AltCommandLine("-v")>] Verbose
    | [<AltCommandLine("--no-build")>] NoBuild
    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Project _ -> "Cloudflare Pages project name (defaults to 'flexgrid-demo')"
            | Verbose -> "Verbose output"
            | NoBuild -> "Skip the build step (deploy existing dist folder)"

type CliCommand =
    | [<CliPrefix(CliPrefix.None)>] Deploy of ParseResults<DeployArgs>
    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Deploy _ -> "Build and deploy the site to Cloudflare Pages"

/// Run a shell command and return success/failure
let runCommand (command: string) (args: string) (workingDir: string) (verbose: bool) : Result<unit, string> =
    let psi = ProcessStartInfo(command, args)
    psi.WorkingDirectory <- workingDir
    psi.RedirectStandardOutput <- true
    psi.RedirectStandardError <- true
    psi.UseShellExecute <- false
    psi.CreateNoWindow <- true

    use proc = Process.Start(psi)
    let stdout = proc.StandardOutput.ReadToEnd()
    let stderr = proc.StandardError.ReadToEnd()
    proc.WaitForExit()

    if verbose && not (String.IsNullOrWhiteSpace(stdout)) then
        AnsiConsole.MarkupLine($"        [grey]{Markup.Escape(stdout.Trim())}[/]")

    if proc.ExitCode <> 0 then
        Error (if String.IsNullOrWhiteSpace(stderr) then stdout else stderr)
    else
        Ok ()

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
            let projectName = args.TryGetResult DeployArgs.Project |> Option.defaultValue "flexgrid-demo"
            let verbose = args.Contains DeployArgs.Verbose
            let skipBuild = args.Contains DeployArgs.NoBuild

            // Get project root (parent of cli directory)
            let cliDir = AppContext.BaseDirectory
            let projectRoot = Path.GetFullPath(Path.Combine(cliDir, "..", "..", "..", ".."))
            let distDir = Path.Combine(projectRoot, "dist")

            AnsiConsole.MarkupLine($"[cyan]FlexGrid Pages Deployment[/]")
            AnsiConsole.MarkupLine("")
            AnsiConsole.MarkupLine($"  Project: {projectName}")
            AnsiConsole.MarkupLine("")

            // Step 1: Build
            if not skipBuild then
                AnsiConsole.MarkupLine("  [[1/3]] Building site...")

                // Clean dist directory
                if Directory.Exists(distDir) then
                    if verbose then AnsiConsole.MarkupLine("        Cleaning dist directory...")
                    Directory.Delete(distDir, true)

                // Run npm run build
                if verbose then AnsiConsole.MarkupLine("        Running npm run build...")
                match runCommand "npm" "run build" projectRoot verbose with
                | Error e ->
                    AnsiConsole.MarkupLine($"[red]Error:[/] Build failed: {Markup.Escape(e)}")
                    exit 1
                | Ok () ->
                    if not (Directory.Exists(distDir)) then
                        AnsiConsole.MarkupLine($"[red]Error:[/] Build did not create dist directory")
                        exit 1
                    let fileCount = Directory.GetFiles(distDir, "*", SearchOption.AllDirectories).Length
                    if verbose then AnsiConsole.MarkupLine($"        Found {fileCount} files in dist directory")
            else
                AnsiConsole.MarkupLine("  [[1/3]] Skipping build (--no-build)")
                if not (Directory.Exists(distDir)) then
                    AnsiConsole.MarkupLine($"[red]Error:[/] dist directory not found. Run without --no-build first.")
                    exit 1

            // Create HTTP client with auth
            use httpClient = new HttpClient()
            httpClient.DefaultRequestHeaders.Authorization <- AuthenticationHeaderValue("Bearer", apiToken)

            let pages = PagesUploader.PagesOperations(httpClient, accountId)

            // Step 2: Check if project exists, create if not
            AnsiConsole.MarkupLine("  [[2/3]] Checking project...")
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

            // Step 3: Deploy
            AnsiConsole.MarkupLine("  [[3/3]] Deploying...")
            let progressCallback msg =
                AnsiConsole.MarkupLine($"        {Markup.Escape(msg)}")

            let result =
                pages.DeployDirectory projectName distDir verbose progressCallback
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
