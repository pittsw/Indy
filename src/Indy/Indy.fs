module Indy.Main

open Argu

type SearchType =
    | Type

type Arguments =
    | [<AltCommandLine("-n"); MainCommand; ExactlyOnce; Last>] Name of string list
    | [<AltCommandLine("-d")>] Directory of string
    | [<AltCommandLine("-s")>] Search of SearchType
    | [<AltCommandLine("-h")>] Hint of string
    | [<AltCommandLine("-t"); Unique>] TopDirectory
with
    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Name _ -> "The name of the item that you wish to search for."
            | Directory _ -> "Directories in which to search."
            | Search _ -> "The variety of thing you're searching for.  Default: Class."
            | Hint _ -> "A hint of the namespace or DLL name.  Results are ranked in the number of hints they match."
            | TopDirectory -> "Search only the top directory, not recursively."

[<EntryPoint>]
let main argv =
    let parser = ArgumentParser.Create<Arguments>(programName = "Indy.exe")
    let usage = parser.PrintUsage()
    try
        let args = (parser.Parse argv).GetAllResults()
        printfn "Args: %A" args
    with
    | :? ArguParseException as e -> printfn "%s" e.Message
    0 // return an integer exit code