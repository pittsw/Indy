module Indy.Searcher

open System
open System.Collections.Generic
open System.IO

open Mono.Cecil

type Type =
    | Class
    | Function

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
        |> Seq.choose (fun typeDefinition ->
            if allNames |> Array.exists (fun name -> typeDefinition.Name.ToLower().Contains(name)) then
                Some {
                    Name = typeDefinition.Name
                    FullName = typeDefinition.FullName
                    AssemblyName = moduleDef.Name
                    AssemblyPath = dllPath
                    Type = Class
                }
            else
                None)

    let searchOption = if args.NoRecurse then SearchOption.TopDirectoryOnly else SearchOption.AllDirectories
    let searchAll pat = Directory.EnumerateFiles(args.Directory, pat, searchOption)
    seq {
        yield! searchAll "*.dll"
        yield! searchAll "*.exe"
    }
    |> Seq.collect searchDll
    |> List.ofSeq