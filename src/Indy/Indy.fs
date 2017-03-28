module Indy.Main

open Argu

type Arguments =
    | [<MainCommand; ExactlyOnce; Last>] Name of string list
    | [<AltCommandLine("-d")>] Directory of string
    | [<AltCommandLine("-t")>] Type of Searcher.Type
    | [<AltCommandLine("-n")>] No_Recurse
with
    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Name _ -> "The name of the item that you wish to search for."
            | Directory _ -> "Directories in which to search.  Defaults to current directory."
            | Type _ -> "The types of item to search for.  Defaults to seraching for all types."
            | No_Recurse -> "Sarch only the directory listed, and not its children."

[<EntryPoint>]
let main argv =
    let parser = ArgumentParser.Create<Arguments>(programName = "Indy.exe")
    try
        let args = parser.Parse argv
        let searchResults =
            Searcher.search
                {
                    Searcher.SearchArgs.Directory = args.GetResult (<@ Directory @>, ".")
                    NoRecurse = args.Contains <@ No_Recurse @>
                    Types = args.GetResults <@ Type @>
                            |> (fun x -> if List.isEmpty x then [Searcher.Class; Searcher.Method] else x)
                }
                (args.GetResult <@ Name @>)
        for result in searchResults do
            printfn "%s: %s" result.AssemblyPath result.FullName
    with
    | :? ArguParseException as e -> printfn "%s" e.Message
    0 // return an integer exit code
