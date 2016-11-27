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

let doit = true
let parseAlways = 
    if doit then
        [|  @"..\..\test-data\Sample_VS2013_FSharp_Portable_Library_net451_adjusted_to_profile259\Sample_VS2013_FSharp_Portable_Library_net451.fsproj"
            @"..\..\test-data\Sample_VS2015_FSharp_Portable7_Library\Sample_VS2015_FSharp_Portable7_Library.fsproj"
//            @"..\..\test-data\Sample_VS2015_FSharp_Portable47_Library\Sample_VS2015_FSharp_Portable47_Library.fsproj"
//            @"..\..\test-data\Sample_VS2015_FSharp_Portable78_Library\Sample_VS2015_FSharp_Portable78_Library.fsproj"
        |]
    else
        [||]

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
            Array.append paths parseAlways
            |> Array.filter ^ fun path -> not ^ String.IsNullOrWhiteSpace path
            |> Array.filter ^ fun path -> 
                
                let path = Path.GetFullPath (unquote path)
                if not ^ File.Exists path then
                    printfn "no file found at '%s'" path

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
