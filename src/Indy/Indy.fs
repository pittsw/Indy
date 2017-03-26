module Indy.Main

open Argu

type Arguments =
    | [<AltCommandLine("-n"); MainCommand; ExactlyOnce; Last>] Name of string list
    | [<AltCommandLine("-d")>] Directory of string
with
    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Name _ -> "The name of the item that you wish to search for."
            | Directory _ -> "Directories in which to search.  Defaults to current directory."

[<EntryPoint>]
let main argv =
    let parser = ArgumentParser.Create<Arguments>(programName = "Indy.exe")
    let usage = parser.PrintUsage()
    try
        let args = parser.Parse argv
        let searchResults =
            Searcher.search
                { Searcher.SearchArgs.Directory = args.GetResult (<@ Directory @>, ".") }
                (args.GetResult <@ Name @>)
        for result in searchResults do
            printfn "%s: %s" result.AssemblyPath result.FullName
    with
    | :? ArguParseException as e -> printfn "%s" e.Message
    0 // return an integer exit code
