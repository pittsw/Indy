module Indy.Searcher

open System
open System.IO
open System.Reflection

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
}

let search args name =
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
            if typeDefinition.Name = name then
                Some {
                    Name = name
                    FullName = typeDefinition.FullName
                    AssemblyName = moduleDef.Name
                    AssemblyPath = dllPath
                    Item = Class
                }
            else
                None)

    let searchAll pat = Directory.EnumerateFiles(args.Directory, pat, SearchOption.AllDirectories)
    seq {
        yield! searchAll "*.dll"
        yield! searchAll "*.exe"
    }
    |> Seq.collect searchDll
    |> List.ofSeq