module Indy.Main

open Argu

type Arguments =
    | [<MainCommand; ExactlyOnce; Last>] Name of string list
    | [<AltCommandLine("-d")>] Directory of string
    | [<AltCommandLine("-e")>] Element_Type of Searcher.ElementType
    | [<AltCommandLine("-n")>] No_Recurse
with
    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Name _ -> "The name of the item that you wish to search for."
            | Directory _ -> "Directories in which to search.  Defaults to current directory."
            | Element_Type _ -> "The type(s) of element to search for.  Defaults to searching for all listed types."
            | No_Recurse -> "Search only the directory listed, and not its children."

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
                    }
                    terms
            for result in searchResults do
                printfn "%s: %s" result.AssemblyPath result.FullName
    with
    | :? ArguParseException as e -> printfn "%s" e.Message
    0 // return an integer exit code
