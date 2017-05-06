module Indy.Searcher

open System
open System.Linq
open System.IO

open Glob
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
    Static : bool option
    NoRecurse : bool
    Verbose : bool
}

/// Attempts the given operation.  If it fails and Verbose is true, calls logger with the exception.
let tryArray f logger args =
    try
        f()
    with
    | e ->
        if args.Verbose then logger e
        [||]

/// Performs a case insensitive match on the given element against the given name.
let private filterByName<'T> (nameFunc : 'T -> string) (name : string) (elements : 'T seq) =
    elements
    |> Seq.filter (fun e -> (nameFunc e).IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)

/// Filters the given list by the type filter, if it exists.
let private filterByTypeFilter<'T> args (nameFunc : 'T -> string) (elements : 'T seq) =
    match args.TypeFilter with
    | None -> elements
    | Some t -> filterByName nameFunc t elements

/// Filters the given list by the static filter, if it exists.
let private filterByStatic<'T> args (staticFunc : 'T -> bool) (elements : 'T seq) =
    match args.Static with
    | None -> elements
    | Some s -> elements |> Seq.filter (fun e -> staticFunc e = s)

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

    let getRefParts ``type`` foundMembers =
        match ``type`` with
        | Class -> [typeDefinition] |> Seq.cast<MemberReference>
        | Method ->
            typeDefinition.Methods
            |> Seq.filter (fun m -> not m.IsSpecialName)
            |> filterByTypeFilter args (fun m -> m.ReturnType.FullName)
            |> filterByStatic args (fun m -> m.IsStatic)
            |> Seq.cast<MemberReference>
        | Property ->
            let methodStatic (m : MethodDefinition) =
                not (isNull m) && m.IsStatic

            typeDefinition.Properties
            |> filterByTypeFilter args (fun p -> p.PropertyType.FullName)
            |> filterByStatic args (fun p -> methodStatic p.GetMethod || methodStatic p.SetMethod)
            |> Seq.cast<MemberReference>
        | Field ->
            typeDefinition.Fields
            |> Seq.filter (fun f -> not (f.Name.Contains("@") || f.Name.Contains("<")))
            |> filterByTypeFilter args (fun f -> f.FieldType.FullName)
            |> filterByStatic args (fun f -> f.IsStatic)
            |> Seq.cast<MemberReference>
        | Event ->
            typeDefinition.Events
            |> filterByTypeFilter args (fun e -> e.EventType.FullName)
            |> filterByStatic args (fun e -> e.AddMethod.IsStatic)
            |> Seq.cast<MemberReference>
        |> Seq.filter (fun mem -> not <| Seq.exists (fun fm -> fm.FullName = mem.FullName) foundMembers)

    let findMatches foundNames t = getRefParts t foundNames |> Seq.choose (matchRef t)
    let events = set (findMatches [] Event)

    let nonEvents =
        args.ElementTypes
        |> Seq.filter (fun e ->
            match e with
            | Event -> false
            | _ -> true)
        |> Seq.collect (findMatches events)

    if List.contains Event args.ElementTypes then
        Seq.concat [nonEvents; events :> SearchResult seq]
    else
        nonEvents

let private getTopDirs args =
    let globChars = [|'*'; '?'; '['; ']'; '{'; '}'|]
    let hasNoGlobChars (directoryPart : string) =
        not <| Array.exists (fun c -> Array.contains c globChars) (directoryPart.ToCharArray())
    let directoryParts =
        args.Directory.Split(
            [|Path.DirectorySeparatorChar; Path.AltDirectorySeparatorChar|],
            StringSplitOptions.RemoveEmptyEntries)

    let nonGlobDirs =
        directoryParts
        |> Array.takeWhile hasNoGlobChars

    // Use String.Join here instead of Path.Combine since Path.Combine doesn't properly combine the drive letter.
    let topNonGlobDir = Path.GetFullPath(String.Join(Path.DirectorySeparatorChar.ToString(), nonGlobDirs))
    let globDirs =
        directoryParts
        |> Array.skipWhile hasNoGlobChars
    let globs =
        globDirs
        |> Seq.scan (fun s t ->
            match s with
            | None -> Some t
            | Some s2 -> Some (sprintf "%s%c%s" s2 Path.DirectorySeparatorChar t)) None
        |> Seq.map (fun s ->
            let patternPart =
                match s with
                | None -> ""
                | Some s2 -> sprintf "%c%s" Path.DirectorySeparatorChar s2
            Glob(sprintf "%s%s" topNonGlobDir patternPart, GlobOptions.Compiled))
        |> Array.ofSeq

    let rec findDirs depth curDir =
        let isPartialMatch = (Array.length globs = 1) || (globs.[Math.Min(globs.Length - 1, depth)].IsMatch(curDir))
        if isPartialMatch then
            let isFullMatch = (Array.length globs = 1) || (globs.[globs.Length - 1].IsMatch(curDir))
            if isFullMatch then
                [|curDir|]
            else
                tryArray
                    (fun () -> Directory.GetDirectories(curDir))
                    (fun e -> eprintfn "Error enumerating directories under %s: %s" curDir e.Message)
                    args
                |> Array.collect (findDirs (depth + 1))
        else
            [||]

    findDirs 0 topNonGlobDir


/// Runs a search using the given args and list of names to search against.
let search args (names : string seq) dllMatchCallback =
    let searchDll (dllPath : string) =
        let rec getAllTypes (t : TypeDefinition) =
            seq {
                yield t
                for nt in t.NestedTypes do
                    yield! getAllTypes nt
            }

        tryArray
            (fun () ->
                let moduleDef = ModuleDefinition.ReadModule(dllPath)
                moduleDef.Types
                |> Seq.collect getAllTypes
                |> Seq.collect (getMatchingMembers args names dllPath)
                |> Array.ofSeq)
            (fun e -> eprintfn "Error reading %s: '%s'" dllPath e.Message)
            args
        |> dllMatchCallback

    let rec searchHelper curDir =
            let allFiles =
                [|"*.dll"; "*.exe"|]
                |> Array.collect (fun pat ->
                    tryArray
                        (fun () -> Directory.GetFiles(curDir, pat))
                        (fun e -> eprintfn "Error enumerating files in %s: '%s'" curDir e.Message)
                        args)

            for file in allFiles do
                searchDll file

            if not args.NoRecurse then
                let allDirs =
                    tryArray
                        (fun () -> Directory.GetDirectories(curDir))
                        (fun e -> eprintfn "Error enumerating directories in %s: '%s'" curDir e.Message)
                        args
                for subDir in allDirs do
                    searchHelper subDir
    
    getTopDirs args
    |> Seq.iter searchHelper
