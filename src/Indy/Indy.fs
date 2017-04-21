module Indy.Main

open Argu

type Arguments =
    | [<MainCommand; ExactlyOnce; Last>] Name of string list
    | [<AltCommandLine("-d")>] Directory of string
    | [<AltCommandLine("-e")>] Element_Type of Searcher.ElementType
    | [<AltCommandLine("-t")>] Type_Filter of string
    | [<AltCommandLine("-s")>] Static of bool
    | [<AltCommandLine("-n")>] No_Recurse
    | [<AltCommandLine("-v")>] Verbose
with
    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Name _ -> "The name of the item that you wish to search for."
            | Directory _ -> "Directories in which to search.  Defaults to current directory."
            | Element_Type _ ->
                "The type(s) of element to search for.  Defaults to searching for all listed types. The 'class' option"
                    + " also searches enums and delegates."
            | Type_Filter _ ->
                "Allows filtering on the type of the search term as well as the name. For properties and fields, this"
                    + " will filter on the type of property or field. For methods, this will filter on the return type."
            | Static _ ->
                "Specify either true or false to only search static or instance members."
                    + " Has no effect on class searches."
            | No_Recurse -> "Search only the directory listed, and not its children."
            | Verbose -> "Print detailed logs."

[<EntryPoint>]
let main argv =
    let parser = ArgumentParser.Create<Arguments>(programName = "Indy.exe")
    try
        let args = parser.Parse argv
        let searchTerm = (args.GetResult <@ Name @>)
        match searchTerm with
        | ["-h"]
        | ["-H"]
        | ["-?"]
        | ["/h"]
        | ["/H"]
        | ["/?"] -> printfn "%s" <| parser.PrintUsage()
        | terms ->
            let searchResults =
                Searcher.search
                    {
                        Searcher.SearchArgs.Directory = args.GetResult (<@ Directory @>, ".")
                        NoRecurse = args.Contains <@ No_Recurse @>
                        ElementTypes = args.GetResults <@ Element_Type @>
                                |> (fun x -> if List.isEmpty x then Searcher.ElementType.AllTypes else x)
                        TypeFilter = args.TryGetResult <@ Type_Filter @>
                        Static = args.TryGetResult <@ Static @>
                        Verbose = args.Contains <@ Verbose @>
                    }
                    terms
            for result in searchResults do
                printfn "%s: %s" result.AssemblyPath result.FullName
    with
    | :? ArguParseException as e -> printfn "%s" e.Message
    0 // return an integer exit code
