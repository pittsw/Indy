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
            ElementType = Class
        }

    static member SearchTargetMethod() = ()
    static member SearchTargetMethod2() = 5
    member val SearchTargetProperty = 0 with get, set

    [<CLIEvent>]
    member __.SearchTargetEventPublic = (new Event<_>()).Publish

let defaultArgs = { Directory = curDir; NoRecurse = false; ElementTypes = ElementType.AllTypes; ReturnType = None }

[<Tests>]
let basicSearchTests =
    testList "basicSearchTests" [
        testCase "class search" <| fun _ ->
            Expect.equal
                (search { defaultArgs with ElementTypes = [Class] } ["SearchTarget"]) 
                ([
                    {
                        Name = "SearchTargetEnum"
                        FullName = "Indy.Tests.Tests/SearchTargetEnum"
                        AssemblyName = "Indy.Tests.exe"
                        AssemblyPath = Path.Combine(curDir, "Indy.Tests.exe")
                        ElementType = Class
                    }
                    {
                        Name = "SearchTargetDelegate"
                        FullName = "Indy.Tests.Tests/SearchTargetDelegate"
                        AssemblyName = "Indy.Tests.exe"
                        AssemblyPath = Path.Combine(curDir, "Indy.Tests.exe")
                        ElementType = Class
                    }
                    SearchTargetType.Expected
                ])
                "Class search."

        testCase "method search" <| fun _ ->
            Expect.equal
                (search { defaultArgs with ElementTypes = [Method] } ["SearchTarget"]) 
                ([
                    {
                        Name = "SearchTargetMethod"
                        FullName = "System.Void Indy.Tests.Tests/SearchTargetType::SearchTargetMethod()"
                        AssemblyName = "Indy.Tests.exe"
                        AssemblyPath = Path.Combine(curDir, "Indy.Tests.exe")
                        ElementType = Method
                    }
                    {
                        Name = "SearchTargetMethod2"
                        FullName = "System.Int32 Indy.Tests.Tests/SearchTargetType::SearchTargetMethod2()"
                        AssemblyName = "Indy.Tests.exe"
                        AssemblyPath = Path.Combine(curDir, "Indy.Tests.exe")
                        ElementType = Method
                    }
                ])
                "Method search."

        testCase "property search" <| fun _ ->
            Expect.equal
                (search { defaultArgs with ElementTypes = [Property] } ["SearchTarget"]) 
                ([
                    {
                        Name = "SearchTargetProperty"
                        FullName = "System.Int32 Indy.Tests.Tests/SearchTargetType::SearchTargetProperty()"
                        AssemblyName = "Indy.Tests.exe"
                        AssemblyPath = Path.Combine(curDir, "Indy.Tests.exe")
                        ElementType = Property
                    }
                ])
                "Property search."

        testCase "field search" <| fun _ ->
            Expect.equal
                (search { defaultArgs with ElementTypes = [Field] } ["SearchTarget"]) 
                ([
                    {
                        Name = "searchTargetField"
                        FullName = "System.Int32 Indy.Tests.Tests/SearchTargetType::searchTargetField"
                        AssemblyName = "Indy.Tests.exe"
                        AssemblyPath = Path.Combine(curDir, "Indy.Tests.exe")
                        ElementType = Field
                    }
                ])
                "Field search."

        testCase "event search" <| fun _ ->
            Expect.equal
                (search { defaultArgs with ElementTypes = [Event] } ["SearchTarget"]) 
                ([
                    {
                        Name = "SearchTargetEventPublic"
                        FullName = "Microsoft.FSharp.Control.FSharpHandler`1<System.Object> Indy.Tests.Tests/SearchTargetType::SearchTargetEventPublic"
                        AssemblyName = "Indy.Tests.exe"
                        AssemblyPath = Path.Combine(curDir, "Indy.Tests.exe")
                        ElementType = Event
                    }
                ])
                "Event search."
    ]

[<Tests>]
let fileSelectionTests =
    testList "fileSelectionTests" [
        testCase "directory specification" <| fun _ ->
            let args = { defaultArgs with Directory = Path.GetDirectoryName(curDir); NoRecurse = true }
            Expect.equal
                (search args [SearchTargetType.Name])
                ([])
                "Searching with TopOnly should exclude child directories."
    ]

[<Tests>]
let elementFilteringTests =
    testList "elementFilteringTests" [
        testCase "return type" <| fun _ ->
            let args = { defaultArgs with ElementTypes = [Method]; ReturnType = Some "int" }
            Expect.equal
                (search args ["SearchTarget"]) 
                ([
                    {
                        Name = "SearchTargetMethod2"
                        FullName = "System.Int32 Indy.Tests.Tests/SearchTargetType::SearchTargetMethod2()"
                        AssemblyName = "Indy.Tests.exe"
                        AssemblyPath = Path.Combine(curDir, "Indy.Tests.exe")
                        ElementType = Method
                    }
                ])
                "Method search."
    ]