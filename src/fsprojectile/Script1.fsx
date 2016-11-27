System.IO.Directory.SetCurrentDirectory __SOURCE_DIRECTORY__
#r "System.Xml.Linq"

#load "Prelude.fs"
open System
open System.IO



open System.Xml.Linq
open fsprojectile.Prelude


let projXDoc = XDocument.Load("fsprojectile.fsproj")

let toolsVersion =  
    match XDoc.tryGetElement "Project" projXDoc with
    | None -> "15.0"
    | Some xelem -> xelem |> XElem.getAttribute "ToolsVersion" |> XAttr.value
;;
let unquote (text:string) = 
    let text = text.Trim()
    if text.StartsWith "\"" && text.EndsWith "\"" then
        text.TrimStart('"').TrimEnd('"')
    else text

let path1 = @" ""C:\Users\jared\Github\Owned\fsprojectile\test-data\lib\ExampleLibrary.fsproj"" "
;;
unquote path1
;;

let path = @"C:\Users\jared\Github\Owned\fsprojectile\test-data\lib\ExampleLibrary.fsproj"

Path.GetExtension path
;;
