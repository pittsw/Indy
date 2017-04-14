module Indy.Tests.Tests

open System.IO
open System.Reflection

open Expecto
open Indy.Searcher

let curDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)

type SearchTargetEnum =
    | ValueOne = 1

type SearchTargetDelegate = delegate of int -> int

type SearchTargetType() =
    [<DefaultValue>]
    val mutable searchTargetField : int

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
    member __.SearchTargetEventPublic = (new Event<_>()).Publish

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

        testCase "class search" <| fun _ ->
            Expect.equal
                (search defaultArgs ["SearchTarget"]) 
                ([
                    {
                        Name = "SearchTargetEnum"
                        FullName = "Indy.Tests.Tests/SearchTargetEnum"
                        AssemblyName = "Indy.Tests.exe"
                        AssemblyPath = Path.Combine(curDir, "Indy.Tests.exe")
                        Type = Class
                    }
                    {
                        Name = "SearchTargetDelegate"
                        FullName = "Indy.Tests.Tests/SearchTargetDelegate"
                        AssemblyName = "Indy.Tests.exe"
                        AssemblyPath = Path.Combine(curDir, "Indy.Tests.exe")
                        Type = Class
                    }
                    SearchTargetType.Expected
                ])
                "Class search."

        testCase "method search" <| fun _ ->
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

        testCase "property search" <| fun _ ->
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

        testCase "field search" <| fun _ ->
            Expect.equal
                (search { defaultArgs with Types = [Field] } ["SearchTarget"]) 
                ([
                    {
                        Name = "searchTargetField"
                        FullName = "System.Int32 Indy.Tests.Tests/SearchTargetType::searchTargetField"
                        AssemblyName = "Indy.Tests.exe"
                        AssemblyPath = Path.Combine(curDir, "Indy.Tests.exe")
                        Type = Field
                    }
                ])
                "Field search."

        testCase "event search" <| fun _ ->
            Expect.equal
                (search { defaultArgs with Types = [Event] } ["SearchTarget"]) 
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