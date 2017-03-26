module Indy.Searcher

open System
open System.IO
open System.Reflection

type Item =
    | Class

type SearchResult = {
    Name : string
    Namespace : string
    AssemblyName : string
    AssemblyPath : string
    Item : Item
}

type SearchArgs = {
    Directory : string
}

let search args name =
    let searchDll dllPath : SearchResult seq =
        let assembly = Assembly.ReflectionOnlyLoadFrom(dllPath)
        assembly.DefinedTypes
        |> Seq.choose (fun typeinfo ->
            if typeinfo.Name = name then
                Some {
                    Name = name
                    Namespace = typeinfo.Namespace
                    AssemblyName = assembly.FullName
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