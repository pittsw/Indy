// include Fake libs
#r "./packages/FAKE/tools/FakeLib.dll"

open Fake
open Fake.Testing.Expecto

// Directories
let buildDir = "./build/"
let appOutDir = buildDir + "Indy"
let testsOutDir = buildDir + "tests"
let deployDir = "./deploy/"


// Filesets
let projectReferences baseDir =
    !! (sprintf "%s/**/*.csproj" baseDir)
    ++ (sprintf "%s/**/*.fsproj" baseDir)

let appReferences = projectReferences "src"
let testReferences = projectReferences "tests"

// version info
let version = "0.1"  // or retrieve from CI server

// Targets
Target "Clean" (fun _ ->
    CleanDirs [buildDir; deployDir]
)

Target "Build" (fun _ ->
    MSBuildRelease appOutDir "Build" appReferences
    |> Log "Build-Output: "
)

Target "Test" (fun _ ->
    MSBuildDebug testsOutDir "Build" testReferences
    |> Log "Test-Output: "

    !! (testsOutDir + "/Indy.Tests.exe")
    |> Expecto id
)

Target "Deploy" (fun _ ->
    !! (appOutDir + "/**/*.*")
    |> Zip buildDir (deployDir + "Indy." + version + ".zip")
)

// Build order
"Clean"
  ==> "Build"
  ==> "Test"
  ==> "Deploy"

// start build
RunTargetOrDefault "Test"
