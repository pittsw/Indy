module Indy.Searcher

open System.IO

open Mono.Cecil

type ElementType =
    | Class
    | Method
    | Property
    | Field
    | Event
with
    static member AllTypes = [
        Class
        Method
        Property
        Field
        Event
    ]

type SearchResult = {
    Name : string
    FullName : string
    AssemblyName : string
    AssemblyPath : string
    ElementType : ElementType
}

type SearchArgs = {
    Directory : string
    ElementTypes : ElementType list
    ReturnType : string option
    NoRecurse : bool
}

let search args (names : string seq) =
    let allNames = names |> Seq.map (fun s -> s.ToLower()) |> Array.ofSeq
    let lowerReturnType = args.ReturnType |> Option.map (fun t -> t.ToLower())
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
                    ElementType = ``type``
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
                    let methods =
                        typeDefinition.Methods
                        |> Seq.filter (fun m -> not m.IsSpecialName)

                    let filteredMethods =
                        match lowerReturnType with
                        | None -> methods
                        | Some t -> methods |> Seq.filter (fun m -> m.ReturnType.FullName.ToLower().Contains(t))

                    filteredMethods
                    |> Seq.cast<MemberReference>
                | Property -> typeDefinition.Properties |> Seq.cast<MemberReference>
                | Field ->
                    typeDefinition.Fields
                    |> Seq.filter (fun f -> not (f.Name.Contains("@")))
                    |> Seq.cast<MemberReference>
                | Event -> typeDefinition.Events |> Seq.cast<MemberReference>

            args.ElementTypes
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
    let searchAll pat = Directory.EnumerateFiles(Path.GetFullPath(args.Directory), pat, searchOption)
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