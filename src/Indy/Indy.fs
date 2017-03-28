module Indy.Main

open Argu

type Arguments =
    | [<AltCommandLine("-n"); MainCommand; ExactlyOnce; Last>] Name of string list
    | [<AltCommandLine("-d")>] Directory of string
    | [<AltCommandLine("-t")>] Top_Directory_Only
with
    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Name _ -> "The name of the item that you wish to search for."
            | Directory _ -> "Directories in which to search.  Defaults to current directory."
            | Top_Directory_Only -> "Set this flag to only search the directory given and no children."

[<EntryPoint>]
let main argv =
    let parser = ArgumentParser.Create<Arguments>(programName = "Indy.exe")
    try
        let args = parser.Parse argv
        let searchResults =
            Searcher.search
                {
                    Searcher.SearchArgs.Directory = args.GetResult (<@ Directory @>, ".")
                    TopOnly = args.Contains <@ Top_Directory_Only @>
                }
                (args.GetResult <@ Name @>)
        for result in searchResults do
            printfn "%s: %s" result.AssemblyPath result.FullName
    with
    | :? ArguParseException as e -> printfn "%s" e.Message
    0 // return an integer exit code
