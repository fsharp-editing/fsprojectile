module fsprojectile.Program

open System
open System.IO

Directory.SetCurrentDirectory __SOURCE_DIRECTORY__
printfn "%s\n\nenter project files seperated by ';'\n" __SOURCE_DIRECTORY__


[<EntryPoint>]
let main argv =

    let candidates =
        let paths = if argv = [||] then Console.ReadLine().Split(';') else argv
        paths |> Array.filter ^ fun path -> File.Exists path && path.EndsWith ".fsproj"

    if candidates.Length = 0 then
        printfn "No valid '.fsproj' files to inspect"
    else
        candidates |> Array.iter ^ fun path ->
            try
                printfn "-- %s --\n" path
                fullpath path |> getFSharpProjectOptions  |> printfn "%A\n"
            with
            | ex -> printfn "Unable to inspect project - '%s'\n%s\n" path ex.Message
    System.Console.ReadKey() |> ignore
    0
