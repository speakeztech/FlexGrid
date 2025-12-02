module Build

open System
open System.IO
open Fake.Core
open Fake.Core.TargetOperators
open Fake.DotNet
open Fake.IO
open Fake.JavaScript

// --------------------------------------------------------------------------------------
// Configuration
// --------------------------------------------------------------------------------------

let rootDir = Path.GetFullPath(Path.Combine(__SOURCE_DIRECTORY__, ".."))
let outputDir = Path.Combine(rootDir, "output")
let demosProject = Path.Combine(rootDir, "demos/FlexGrid.Demos.fsproj")
let testsProject = Path.Combine(rootDir, "tests/FlexGrid.Tests.fsproj")

// --------------------------------------------------------------------------------------
// Helpers
// --------------------------------------------------------------------------------------

let dotnet cmd args =
    DotNet.exec (fun opts -> { opts with WorkingDirectory = rootDir }) cmd args
    |> ignore

let npm cmd =
    Npm.exec cmd (fun opts -> { opts with WorkingDirectory = rootDir })

// --------------------------------------------------------------------------------------
// Targets
// --------------------------------------------------------------------------------------

let initTargets() =
    Target.create "Clean" (fun _ ->
        Shell.cleanDirs [outputDir; Path.Combine(rootDir, "dist")]
        Trace.log "Cleaned output directories"
    )

    Target.create "RestoreTools" (fun _ ->
        dotnet "tool" "restore"
        Trace.log "Restored .NET tools (Fable)"
    )

    Target.create "Restore" (fun _ ->
        // Use exec instead of restore to avoid binary log format issues with .NET 10
        dotnet "restore" "."
        Npm.install (fun opts -> { opts with WorkingDirectory = rootDir })
        Trace.log "Restored .NET and npm packages"
    )

    Target.create "Test" (fun _ ->
        // Use exec instead of test to avoid binary log format issues with .NET 10
        dotnet "test" testsProject
        Trace.log "Tests completed"
    )

    Target.create "Compile" (fun _ ->
        let fableArgs =
            [ demosProject
              "--exclude"; "Partas.Solid.FablePlugin"
              "--noCache"
              "-o"; outputDir
              "-e"; ".fs.jsx"
              "-c"; "Release" ]
            |> String.concat " "

        dotnet "fable" fableArgs
        Trace.log "Fable compilation completed"
    )

    Target.create "Build" (fun _ ->
        npm "run build"
        Trace.log "Production build completed"
    )

    Target.create "Dev" (fun _ ->
        // Start Vite dev server (this will block)
        npm "exec vite"
    )

    Target.create "Watch" (fun _ ->
        // Run Fable watch and Vite concurrently
        npm "run dev:watch"
    )

    Target.create "Preview" (fun _ ->
        npm "run preview"
    )

    Target.create "All" ignore

    // Dependencies
    "Clean"
        ==> "RestoreTools"
        ==> "Restore"
        ==> "Compile"
        |> ignore

    "Compile"
        ==> "Build"
        ==> "All"
        |> ignore

    "Restore"
        ==> "Test"
        |> ignore

    "Compile"
        ==> "Dev"
        |> ignore

    "Restore"
        ==> "Watch"
        |> ignore

    "Build"
        ==> "Preview"
        |> ignore

// --------------------------------------------------------------------------------------
// Entry Point
// --------------------------------------------------------------------------------------

[<EntryPoint>]
let main args =
    args
    |> Array.toList
    |> Context.FakeExecutionContext.Create false "build.fsx"
    |> Context.RuntimeContext.Fake
    |> Context.setExecutionContext

    initTargets()
    Target.runOrDefaultWithArguments "Dev"
    0
