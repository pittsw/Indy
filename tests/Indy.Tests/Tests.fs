module Indy.Tests.Tests

open System.IO
open System.Reflection

open Expecto
open Indy.Searcher

let curDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)

type SearchTargetType() =
    let searchTargetEventPrivate = new Event<_>()

    static member Name = "SearchTargetType"
    static member Expected =
        {
            Name = SearchTargetType.Name
            FullName = "Indy.Tests.Tests/SearchTargetType"
            AssemblyName = "Indy.Tests.exe"
            AssemblyPath = Path.Combine(curDir, "Indy.Tests.exe")
            Type = Class
        }

    static member SearchTargetMethod() = ()
    member val SearchTargetProperty = 0 with get, set

    [<CLIEvent>]
    member this.SearchTargetEventPublic = searchTargetEventPrivate.Publish

[<Tests>]
let basicSearchTests =
    let defaultArgs = { Directory = curDir; NoRecurse = false; Types = [Class] }
    testList "basicSearchTests" [
        testCase "basic type search" <| fun _ ->
            Expect.equal
                (search defaultArgs [SearchTargetType.Name]) 
                ([SearchTargetType.Expected])
                "Basic type search test."

        testCase "directory specification" <| fun _ ->
            let args = { defaultArgs with Directory = Path.GetDirectoryName(curDir); NoRecurse = true }
            Expect.equal
                (search args [SearchTargetType.Name])
                ([])
                "Searching with TopOnly should exclude child directories."

        testCase "different item types" <| fun _ ->
            Expect.equal
                (search defaultArgs [SearchTargetType.Name]) 
                ([SearchTargetType.Expected])
                "Class search."
            Expect.equal
                (search { defaultArgs with Types = [Method] } ["SearchTarget"]) 
                ([
                    {
                        Name = "SearchTargetMethod"
                        FullName = "System.Void Indy.Tests.Tests/SearchTargetType::SearchTargetMethod()"
                        AssemblyName = "Indy.Tests.exe"
                        AssemblyPath = Path.Combine(curDir, "Indy.Tests.exe")
                        Type = Method
                    }
                ])
                "Method search."
            Expect.equal
                (search { defaultArgs with Types = [Property] } ["SearchTarget"]) 
                ([
                    {
                        Name = "SearchTargetProperty"
                        FullName = "System.Int32 Indy.Tests.Tests/SearchTargetType::SearchTargetProperty()"
                        AssemblyName = "Indy.Tests.exe"
                        AssemblyPath = Path.Combine(curDir, "Indy.Tests.exe")
                        Type = Property
                    }
                ])
                "Property search."
            Expect.equal
                (search { defaultArgs with Types = [Indy.Searcher.Event] } ["SearchTarget"]) 
                ([
                    {
                        Name = "SearchTargetEventPublic"
                        FullName = "Microsoft.FSharp.Control.FSharpHandler`1<System.Object> Indy.Tests.Tests/SearchTargetType::SearchTargetEventPublic"
                        AssemblyName = "Indy.Tests.exe"
                        AssemblyPath = Path.Combine(curDir, "Indy.Tests.exe")
                        Type = Event
                    }
                ])
                "Event search."
    ]