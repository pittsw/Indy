module Indy.Searcher

open System
open System.IO

open Mono.Cecil

/// Type of element to search for. Class includes enums and delegates.
type ElementType =
    | Class
    | Method
    | Property
    | Field
    | Event
with

    /// All element types that can be searched.
    static member AllTypes = [
        Class
        Method
        Property
        Field
        Event
    ]

/// The result of performing a search.
type SearchResult = {
    Name : string
    FullName : string
    AssemblyName : string
    AssemblyPath : string
    ElementType : ElementType
}

/// Arguments for searching.
type SearchArgs = {
    Directory : string
    ElementTypes : ElementType list
    TypeFilter : string option
    NoRecurse : bool
}

/// Performs a case insensitive match on the given element against the given name.
let private filterByName<'T> (nameFunc : 'T -> string) (name : string) (elements : 'T seq) =
    elements
    |> Seq.filter (fun e -> (nameFunc e).IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)

/// Filters the given list by the type filter, if it exists.
let private filterByTypeFilter<'T> args (nameFunc : 'T -> string) (elements : 'T seq) =
    match args.TypeFilter with
    | None -> elements
    | Some t -> filterByName nameFunc t elements

/// Given a type definition, returns all matching members from within it.
let private getMatchingMembers args (names : string seq) dllPath (typeDefinition : TypeDefinition) =
    let allNames = names |> Seq.map (fun s -> s.ToLower()) |> Array.ofSeq
    let makeSearchResult ``type`` (mem : MemberReference) =
        {
            Name = mem.Name
            FullName = mem.FullName
            AssemblyName = typeDefinition.Module.Name
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
            typeDefinition.Methods
            |> Seq.filter (fun m -> not m.IsSpecialName)
            |> filterByTypeFilter args (fun m -> m.ReturnType.FullName)
            |> Seq.cast<MemberReference>
        | Property ->
            typeDefinition.Properties
            |> filterByTypeFilter args (fun p -> p.PropertyType.FullName)
            |> Seq.cast<MemberReference>
        | Field ->
            typeDefinition.Fields
            |> Seq.filter (fun f -> not (f.Name.Contains("@") || f.Name.Contains("<")))
            |> filterByTypeFilter args (fun f -> f.FieldType.FullName)
            |> Seq.cast<MemberReference>
        | Event -> typeDefinition.Events |> Seq.cast<MemberReference>

    args.ElementTypes
    |> Seq.collect (fun t -> getRefParts t |> Seq.choose (matchRef t))

/// Runs a search using the given args and list of names to search against.
let search args (names : string seq) =
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
            |> Seq.collect (getMatchingMembers args names dllPath)
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