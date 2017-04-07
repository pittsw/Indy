module Indy.Searcher

open System
open System.Collections.Generic
open System.IO

open Mono.Cecil

type Type =
    | Class
    | Method

type SearchResult = {
    Name : string
    FullName : string
    AssemblyName : string
    AssemblyPath : string
    Type : Type
}

type SearchArgs = {
    Directory : string
    Types : Type list
    NoRecurse : bool
}

let search args (names : string seq) =
    let allNames = names |> Seq.map (fun s -> s.ToLower()) |> Array.ofSeq
    let searchDll (dllPath : string) : SearchResult seq =
        let rec getAllTypes (t : TypeDefinition) =
            seq {
                yield t
                for nt in t.NestedTypes do
                    yield! getAllTypes nt
            }

        try
            let moduleDef = ModuleDefinition.ReadModule(dllPath)
            moduleDef.Types
            |> Seq.collect getAllTypes
            |> Seq.collect (fun typeDefinition ->
                let isMatch (foundName : string) ``type`` =
                    allNames
                    |> Array.exists (fun name ->
                        foundName.ToLower().Contains(name)
                        && args.Types |> List.contains ``type``)

                let makeSearchResult ``type`` (mem : MemberReference) =
                    {
                        Name = mem.Name
                        FullName = mem.FullName
                        AssemblyName = moduleDef.Name
                        AssemblyPath = dllPath
                        Type = ``type``
                    }

                let matchMethod (func : MethodDefinition) =
                    if isMatch func.Name Method then
                        Some (makeSearchResult Method (func :> MemberReference))
                    else
                        None
            
                let typePart =
                    if isMatch typeDefinition.Name Class then
                        [makeSearchResult Class typeDefinition]
                    else
                        []

                let methodParts =
                    typeDefinition.Methods
                    |> Seq.choose matchMethod
                    |> List.ofSeq
                typePart @ methodParts)
            with
            | e ->
                eprintfn "Error reading %s: '%s'" dllPath e.Message
                Seq.empty

    let searchOption = if args.NoRecurse then SearchOption.TopDirectoryOnly else SearchOption.AllDirectories
    let searchAll pat = Directory.EnumerateFiles(args.Directory, pat, searchOption)
    let allFiles =
        try
            ["*.dll"; "*.exe"]
            |> Seq.collect searchAll
            |> List.ofSeq
        with
        | e ->
            eprintfn "Error enumerating files in %s: %s" args.Directory e.Message
            []
    allFiles
    |> Seq.collect searchDll
    |> List.ofSeq