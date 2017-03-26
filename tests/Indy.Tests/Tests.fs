module Indy.Tests.Tests

open System.IO
open System.Reflection

open Expecto
open Indy.Searcher

type SearchTargetType() =
    class
    end

let curDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)

[<Tests>]
let basicSearchTests =
    let args = { Directory = curDir }
    testList "basicSearchTests" [
        testCase "basicTypeSearch" <| fun _ ->
            Expect.equal
                (search args ["SearchTargetType"]) 
                ([{
                    Name = "SearchTargetType"
                    FullName = "Indy.Tests.Tests/SearchTargetType"
                    AssemblyName = "Indy.Tests.exe"
                    AssemblyPath = Path.Combine(curDir, "Indy.Tests.exe")
                    Item = Class
                }])
                "Basic type search test."
    ]