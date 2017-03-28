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

            let matchMethod names (func : MethodDefinition) =
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
                |> Seq.choose (matchMethod allNames)
                |> List.ofSeq
            typePart @ methodParts)

    let searchOption = if args.NoRecurse then SearchOption.TopDirectoryOnly else SearchOption.AllDirectories
    let searchAll pat = Directory.EnumerateFiles(args.Directory, pat, searchOption)
    seq {
        yield! searchAll "*.dll"
        yield! searchAll "*.exe"
    }
    |> Seq.collect searchDll
    |> List.ofSeq