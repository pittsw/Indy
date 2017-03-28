module Indy.Searcher

open System
open System.Collections.Generic
open System.IO

open Mono.Cecil

type Item =
    | Class

type SearchResult = {
    Name : string
    FullName : string
    AssemblyName : string
    AssemblyPath : string
    Item : Item
}

type SearchArgs = {
    Directory : string
    TopOnly : bool
}

let search args (names : string seq) =
    let allNames = HashSet<string>(names, StringComparer.OrdinalIgnoreCase)
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
        |> Seq.choose (fun typeDefinition ->
            if allNames.Contains(typeDefinition.Name) then
                Some {
                    Name = typeDefinition.Name
                    FullName = typeDefinition.FullName
                    AssemblyName = moduleDef.Name
                    AssemblyPath = dllPath
                    Item = Class
                }
            else
                None)

    let searchOption = if args.TopOnly then SearchOption.TopDirectoryOnly else SearchOption.AllDirectories
    let searchAll pat = Directory.EnumerateFiles(args.Directory, pat, searchOption)
    seq {
        yield! searchAll "*.dll"
        yield! searchAll "*.exe"
    }
    |> Seq.collect searchDll
    |> List.ofSeq