module Indy.Searcher

open System
open System.Collections.Generic
open System.IO

open Mono.Cecil

type Type =
    | Class
    | Method
    | Property

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

        let getAllMatchingMembers (moduleDef : ModuleDefinition) (typeDefinition : TypeDefinition) =
            let makeSearchResult ``type`` (mem : MemberReference) =
                {
                    Name = mem.Name
                    FullName = mem.FullName
                    AssemblyName = moduleDef.Name
                    AssemblyPath = dllPath
                    Type = ``type``
                }

            let matchRef ``type`` (ref : MemberReference) =
                let isMatch =
                    let lowerName = ref.Name.ToLower()
                    allNames
                    |> Array.exists (fun name -> lowerName.Contains(name))
                if isMatch then
                    Some (makeSearchResult ``type`` ref)
                else
                    None

            let getRefParts ``type`` =
                match ``type`` with
                | Class -> [typeDefinition] |> Seq.cast<MemberReference>
                | Method ->
                    typeDefinition.Methods
                    |> Seq.filter (fun m -> not m.IsSetter && not m.IsGetter)
                    |> Seq.cast<MemberReference>
                | Property -> typeDefinition.Properties |> Seq.cast<MemberReference>
            
            args.Types
            |> Seq.collect (fun t -> getRefParts t |> Seq.choose (matchRef t))

        try
            let moduleDef = ModuleDefinition.ReadModule(dllPath)
            moduleDef.Types
            |> Seq.collect getAllTypes
            |> Seq.collect (getAllMatchingMembers moduleDef)
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