[<AutoOpen>]
module fsprojectile.Program

open fsprojectile
open fsprojectile.ProjectFileInfo
open System
open System.Collections.Generic
open System.IO
open Microsoft.Build.CommandLine

type fsprojectile =

    static member GetFSharpProjectOptions (projectPath:string) =
        ProjectFileInfo.create projectPath
        |> ProjectFileInfo.toFSharpProjectOptions (Dictionary())
        |> fst


let runMSBuildCommand args = 
    MSBuildApp.Execute args
    |> printfn "%A"
    
let getProjectFileInfo path = 
    ProjectFileInfo.create path

    
let getFSharpProjectOptions (projectPath:string) =
    ProjectFileInfo.create projectPath
    |> ProjectFileInfo.toFSharpProjectOptions (Dictionary())
    |> fst


[<EntryPoint>]
let main argv =
    Directory.SetCurrentDirectory __SOURCE_DIRECTORY__
    printfn "%s\n\nenter project files seperated by ';'\n" __SOURCE_DIRECTORY__
        

    let rec loop (argv:string[]) =
        let paths = if argv = [||] then Console.ReadLine().Split ';' else argv

        if paths = [||] then
            printfn "no project files was proivded, exit with 'quit'"
        elif paths.[0] = "quit" then ()
        else
            
        let candidates =
            paths |> Array.filter ^ fun path -> 
                let path = Path.GetFullPath (unquote path)
                File.Exists path &&  Path.GetExtension path = ".fsproj"

        if candidates.Length = 0 then
            printfn "No valid '.fsproj' files to inspect"
        else
            candidates |> Array.iter ^ fun path ->
                try
                    let path = Path.GetFullPath (unquote path)
                    printfn "-- %s --\n" path
                    //fullpath path |> printConditioned
                    fullpath path |> getFSharpProjectOptions  |> printfn "\n%A\n"
                with
                | ex -> 
                    printfn "Unable to inspect project - '%s'\n%s\n" path ex.Message
                    loop [|Console.ReadLine()|]
    loop argv

    System.Console.ReadKey() |> ignore
    0
