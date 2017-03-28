module Indy.Tests.Tests

open System.IO
open System.Reflection

open Expecto
open Indy.Searcher

let curDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)

type SearchTargetType() =
    static member Name = "SearchTargetType"
    static member Expected =
        {
            Name = SearchTargetType.Name
            FullName = "Indy.Tests.Tests/SearchTargetType"
            AssemblyName = "Indy.Tests.exe"
            AssemblyPath = Path.Combine(curDir, "Indy.Tests.exe")
            Item = Class
        }

[<Tests>]
let basicSearchTests =
    let defaultArgs = { Directory = curDir; TopOnly = false }
    testList "basicSearchTests" [
        testCase "basicTypeSearch" <| fun _ ->
            Expect.equal
                (search defaultArgs [SearchTargetType.Name]) 
                ([SearchTargetType.Expected])
                "Basic type search test."
        testCase "directory_specification" <| fun _ ->
            let args = { defaultArgs with Directory = Path.GetDirectoryName(curDir); TopOnly = true }
            Expect.equal
                (search args [SearchTargetType.Name])
                ([])
                "Searching with TopOnly should exclude child directories."
    ]