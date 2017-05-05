module Indy.Tests.Tests

open System
open System.CodeDom.Compiler
open System.IO
open System.Reflection

open Expecto
open Indy.Searcher

let curDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)

let defaultArgs = {
    Directory = curDir
    NoRecurse = false
    ElementTypes = ElementType.AllTypes
    TypeFilter = None
    Static = None
    Verbose = false
}

module Data =
    let initialize =
        let provider = CodeDomProvider.CreateProvider("CSharp")
        let inFile = Path.Combine(curDir, "SearchTarget.cs")
        let outFile = Path.Combine(curDir, "SearchTarget.dll")
        let cp = CompilerParameters(OutputAssembly = outFile, GenerateInMemory = false, TreatWarningsAsErrors = false)
        let results = provider.CompileAssemblyFromFile(cp, inFile)
        if results.Errors.Count > 0 then
            for error in results.Errors do
                eprintfn "%s" <| error.ToString()

            for error in results.Errors do
                if not (error.IsWarning) then
                    failwith "Could not compile target"
        ()

    let searchResult elementType name fullName =
        let assemblyName = "SearchTarget.dll"
        {
            Name = name
            FullName = fullName
            AssemblyName = assemblyName
            AssemblyPath = Path.Combine(curDir, assemblyName)
            ElementType = elementType
        }

    let makeClass = searchResult Class
    let makeMethod = searchResult Method
    let makeProperty = searchResult Property
    let makeField = searchResult Field
    let makeEvent = searchResult Event

    let stType = makeClass "SearchTarget" "Indy.Tests.SearchTarget"
    let stEnum = makeClass "SearchTargetEnum" "Indy.Tests.SearchTargetEnum"
    let stDelegate = makeClass "SearchTargetDelegate" "Indy.Tests.SearchTargetDelegate"
    let stDelegateInClass = makeClass
                                "SearchTargetDelegateInClass"
                                "Indy.Tests.SearchTarget/SearchTargetDelegateInClass"

    let stMethod = makeMethod
                    "SearchTargetMethod"
                    "System.Void Indy.Tests.SearchTarget::SearchTargetMethod()"
    let stMethodStatic = makeMethod
                            "SearchTargetMethodStatic"
                            "System.Int32 Indy.Tests.SearchTarget::SearchTargetMethodStatic()"

    let stProperty = makeProperty
                        "SearchTargetProperty"
                        "System.Int32 Indy.Tests.SearchTarget::SearchTargetProperty()"
    let stPropertyStatic = makeProperty
                            "SearchTargetPropertyStatic"
                            "System.String Indy.Tests.SearchTarget::SearchTargetPropertyStatic()"

    let stField = makeField
                    "searchTargetField"
                    "System.Int32 Indy.Tests.SearchTarget::searchTargetField"
    let stFieldStatic = makeField
                            "searchTargetFieldStatic"
                            "System.String Indy.Tests.SearchTarget::searchTargetFieldStatic"
                            
    let stEvent = makeEvent
                    "SearchTargetEvent"
                    "Indy.Tests.SearchTargetDelegate Indy.Tests.SearchTarget::SearchTargetEvent"
    let stEventStatic =
        makeEvent
            "SearchTargetEventStatic"
            "Indy.Tests.SearchTarget/SearchTargetDelegateInClass Indy.Tests.SearchTarget::SearchTargetEventStatic"

let query args =
    search args ["SearchTarget"] |> List.ofSeq

[<Tests>]
let basicSearchTests =
    testList "basicSearchTests" [
        testCase "class search" <| fun _ ->
            Expect.equal
                (query { defaultArgs with ElementTypes = [Class] }) 
                [Data.stEnum; Data.stDelegate; Data.stType; Data.stDelegateInClass]
                "Class search."

        testCase "method search" <| fun _ ->
            Expect.equal
                (query { defaultArgs with ElementTypes = [Method] }) 
                [Data.stMethod; Data.stMethodStatic]
                "Method search."

        testCase "property search" <| fun _ ->
            Expect.equal
                (query { defaultArgs with ElementTypes = [Property] }) 
                [Data.stProperty; Data.stPropertyStatic]
                "Property search."

        testCase "field search" <| fun _ ->
            Expect.equal
                (query { defaultArgs with ElementTypes = [Field] })
                [Data.stField; Data.stFieldStatic]
                "Field search."

        testCase "event search" <| fun _ ->
            Expect.equal
                (query { defaultArgs with ElementTypes = [Event] }) 
                [Data.stEvent; Data.stEventStatic]
                "Event search."
    ]

[<Tests>]
let fileSelectionTests =
    testList "fileSelectionTests" [
        testCase "directory specification" <| fun _ ->
            let args = { defaultArgs with Directory = Path.GetDirectoryName(curDir); NoRecurse = true }
            Expect.equal
                (query args)
                []
                "Searching with TopOnly should exclude child directories."

        testCase "glob support" <| fun _ ->
            let args =
                { defaultArgs with
                    Directory =
                        // Use String.Join instead of Path.Combine because we have invalid path names.
                        String.Join(
                            Path.DirectorySeparatorChar.ToString(),
                            Path.GetDirectoryName(Path.GetDirectoryName(curDir)),
                            "**",
                            Path.GetFileName(curDir))
                    ElementTypes = [Class]
                }
            Expect.equal
                (query args)
                [Data.stEnum; Data.stDelegate; Data.stType; Data.stDelegateInClass]
                "Searching with a glob pattern should work."
    ]

[<Tests>]
let elementFilteringTests =
    testList "elementFilteringTests" [
        testCase "return type" <| fun _ ->
            let args = { defaultArgs with ElementTypes = [Method]; TypeFilter = Some "int" }
            Expect.equal
                (query args) 
                [Data.stMethodStatic]
                "Method return type filtering."

        testCase "property type filtering" <| fun _ ->
            Expect.equal
                (query { defaultArgs with ElementTypes = [Property]; TypeFilter = Some "int" }) 
                [Data.stProperty]
                "Property type filtering."

        testCase "field type filtering" <| fun _ ->
            Expect.equal
                (query { defaultArgs with ElementTypes = [Field]; TypeFilter = Some "int" }) 
                [Data.stField]
                "Field type filtering."

        testCase "event type filtering" <| fun _ ->
            Expect.equal
                (query { defaultArgs with ElementTypes = [Event]; TypeFilter = Some "Indy.Tests.SearchTargetDelegate" })
                [Data.stEvent]
                "Event type filtering."

        testCase "static filtering" <| fun _ ->
            Expect.equal
                (query { defaultArgs with Static = Some true })
                [
                    Data.stEnum
                    Data.stDelegate
                    Data.stType
                    Data.stMethodStatic
                    Data.stPropertyStatic
                    Data.stFieldStatic
                    Data.stEventStatic
                    Data.stDelegateInClass
                ]
                "Static search"

        testCase "non-static filtering" <| fun _ ->
            Expect.equal
                (query { defaultArgs with Static = Some false })
                [
                    Data.stEnum
                    Data.stDelegate
                    Data.stType
                    Data.stMethod
                    Data.stProperty
                    Data.stField
                    Data.stEvent
                    Data.stDelegateInClass
                ]
                "Non-static search"
    ]
