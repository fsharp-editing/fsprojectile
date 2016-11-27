#if INTERACTIVE
open System
open System.IO
//System.IO.Directory.SetCurrentDirectory __SOURCE_DIRECTORY__
System.IO.Directory.SetCurrentDirectory "C:\Users\jared\Github\Owned\fsprojectile\tests\fsprojectile.Tests"
#r "../../packages/Microsoft.Build/lib/net46/Microsoft.Build.dll"
open Microsoft.Build
typeof<Microsoft.Build.Debugging.DebuggerManager>.GetMembers()|>ignore;;
#r "../../packages/Microsoft.Build.Framework/lib/net46/Microsoft.Build.Framework.dll"
typeof<Microsoft.Build.Framework.BuildErrorEventHandler>.GetMembers()|>ignore;;
#r "../../packages/NUnit/lib/net45/nunit.framework.dll"
#r "../../packages/FSharp.Compiler.Service/lib/net45/FSharp.Compiler.Service.dll"
#r "bin/debug/fsprojectile.exe"
#load "FsUnit.fs"
let msbuildexe = @"../../packages/Microsoft.Build.Runtime/contentFiles/any/net46/MSBuild.exe" |> Path.GetFullPath
Environment.SetEnvironmentVariable("MSBUILD_EXE_PATH", msbuildexe);;
#else
module fsprojectile.Tests
#endif
open System
open System.IO



let runningOnMono = try System.Type.GetType "Mono.Runtime" <> null with e ->  false

open System
open System.IO
open NUnit.Framework
open FsUnit
open Microsoft.FSharp.Compiler.SourceCodeServices
open fsprojectile

let normalizePath s = (new Uri(s)).LocalPath

let checkOption (opts:string[]) s = 
    let found = "Found '"+s+"'"
    (if opts |> Array.exists (fun o -> o.EndsWith s) then found 
        else 
            sprintf "Failed to find '%s'\nDid Find:\n%s" s
                        (String.concat "\n" opts)
            
            )
       |> shouldEqual found

let checkOptionNotPresent (opts:string[]) s = 
    let found = "Found '"+s+"'"
    let notFound = "Did not expect to find '"+s+"'"
    (if opts |> Array.exists (fun o -> o.EndsWith(s)) then found else notFound)
       |> shouldEqual notFound

let getReferencedFilenames = Array.choose (fun (o:string) -> if o.StartsWith("-r:") then o.[3..] |> (Path.GetFileName >> Some) else None)
let getReferencedFilenamesAndContainingFolders = Array.choose (fun (o:string) -> if o.StartsWith("-r:") then o.[3..] |> (fun r -> ((r |> Path.GetFileName), (r |> Path.GetDirectoryName |> Path.GetFileName)) |> Some) else None)
let getOutputFile = Array.pick (fun (o:string) -> if o.StartsWith("--out:") then o.[6..] |> Some else None)


#if INTERACTIVE
;;
let p = getFSharpProjectOptions(__SOURCE_DIRECTORY__ + @"../../../test-data/Test2.fsproj")

let references = getReferencedFilenames p.OtherOptions

#endif


let getCompiledFilenames = 
    Array.choose (fun (opt: string) -> 
        if opt.EndsWith ".fs" then 
            opt |> Path.GetFileName |> Some
        else None)
    >> Array.distinct

[<Test>]
let ``Project file parsing example 1 Default Configuration`` () = 
    let projectFile = __SOURCE_DIRECTORY__ + @"../../../test-data/FSharp.Compiler.Service.Tests.fsproj"
    let options = getFSharpProjectOptions projectFile

    checkOption options.ProjectFileNames "FileSystemTests.fs"
    
    checkOption options.OtherOptions "FSharp.Compiler.Service.dll"
    checkOption options.OtherOptions "--define:TRACE"
    checkOption options.OtherOptions "--define:DEBUG"
    checkOption options.OtherOptions "--flaterrors"
    checkOption options.OtherOptions "--simpleresolution"
    checkOption options.OtherOptions "--noframework"

//[<Test>]
//let ``Project file parsing example 1 Release Configuration`` () = 
//    let projectFile = __SOURCE_DIRECTORY__ + @"/FSharp.Compiler.Service.Tests.fsproj"
//    // Check with Configuration = Release
//    let options = getFSharpProjectOptions (projectFile, [("Configuration", "Release")])
//
//    checkOption options.ProjectFileNames "FileSystemTests.fs"
//
//    checkOption options.OtherOptions "FSharp.Compiler.Service.dll"
//    checkOption options.OtherOptions "--define:TRACE"
//    checkOptionNotPresent options.OtherOptions "--define:DEBUG"
//    checkOption options.OtherOptions "--debug:pdbonly"

[<Test>]
let ``Project file parsing example 1 Default configuration relative path`` () = 
    let projectFile = "FSharp.Compiler.Service.Tests.fsproj"
    Directory.SetCurrentDirectory(__SOURCE_DIRECTORY__)
    let options = getFSharpProjectOptions(projectFile)

    checkOption options.ProjectFileNames "FileSystemTests.fs"

    checkOption options.OtherOptions "FSharp.Compiler.Service.dll"
    checkOption options.OtherOptions "--define:TRACE"
    checkOption options.OtherOptions "--define:DEBUG"
    checkOption options.OtherOptions "--flaterrors"
    checkOption options.OtherOptions "--simpleresolution"
    checkOption options.OtherOptions "--noframework"

[<Test>]
let ``Project file parsing VS2013_FSharp_Portable_Library_net45``() = 
    let projectFile = __SOURCE_DIRECTORY__ + @"../../../test-data/Sample_VS2013_FSharp_Portable_Library_net45/Sample_VS2013_FSharp_Portable_Library_net45.fsproj"
    let options = getFSharpProjectOptions projectFile

    checkOption options.OtherOptions "--targetprofile:netcore"
    checkOption options.OtherOptions "--tailcalls-"

    checkOption options.OtherOptions "FSharp.Core.dll"
    checkOption options.OtherOptions "Microsoft.CSharp.dll"
    checkOption options.OtherOptions "System.Runtime.dll"
    checkOption options.OtherOptions "System.Net.Requests.dll"
    checkOption options.OtherOptions "System.Xml.XmlSerializer.dll"

[<Test>]
let ``Project file parsing Sample_VS2013_FSharp_Portable_Library_net451_adjusted_to_profile78``() = 
    let projectFile = __SOURCE_DIRECTORY__ + @"../../../test-data/Sample_VS2013_FSharp_Portable_Library_net451_adjusted_to_profile78/Sample_VS2013_FSharp_Portable_Library_net451.fsproj"
    Directory.SetCurrentDirectory(__SOURCE_DIRECTORY__ + @"../../../test-data/Sample_VS2013_FSharp_Portable_Library_net451_adjusted_to_profile78/")
    let options = getFSharpProjectOptions projectFile

    checkOption options.OtherOptions "--targetprofile:netcore"
    checkOption options.OtherOptions "--tailcalls-"

    checkOption options.OtherOptions "FSharp.Core.dll"
    checkOption options.OtherOptions "Microsoft.CSharp.dll"
    checkOption options.OtherOptions "System.Runtime.dll"
    checkOption options.OtherOptions "System.Net.Requests.dll"
    checkOption options.OtherOptions "System.Xml.XmlSerializer.dll"

[<Test>]
let ``Project file parsing -- compile files 1``() =
  let opts = getFSharpProjectOptions(__SOURCE_DIRECTORY__ + @"../../../test-data/Test1.fsproj")
  CollectionAssert.AreEqual (["Test1File2.fs"; "Test1File1.fs"], opts.ProjectFileNames |> Array.map Path.GetFileName)
  CollectionAssert.IsEmpty (getCompiledFilenames opts.OtherOptions)

[<Test>]
let ``Project file parsing -- compile files 2``() =
  let opts = getFSharpProjectOptions(__SOURCE_DIRECTORY__ + @"../../../test-data/Test2.fsproj")

  CollectionAssert.AreEqual (["Test2File2.fs"; "Test2File1.fs"], opts.ProjectFileNames |> Array.map Path.GetFileName)
  CollectionAssert.IsEmpty (getCompiledFilenames opts.OtherOptions)

//[<Test>]
//let ``Project file parsing -- bad project file``() =
//  let f = normalizePath (__SOURCE_DIRECTORY__ + @"../../../test-data/Malformed.fsproj")
//  let log = snd (getFSharpProjectOptionsLogged(f))
//  log.[f] |> should contain "Microsoft.Build.Exceptions.InvalidProjectFileException"
//
//[<Test>]
//let ``Project file parsing -- non-existent project file``() =
//  let f = normalizePath (__SOURCE_DIRECTORY__ + @"../../../test-data/DoesNotExist.fsproj")
//  let log = snd (getFSharpProjectOptionsLogged(f, enableLogging=true))
//  log.[f] |> should contain "System.IO.FileNotFoundException"

[<Test>]
let ``Project file parsing -- output file``() =
  let p = getFSharpProjectOptions(__SOURCE_DIRECTORY__ + @"../../../test-data/Test1.fsproj")

  let expectedOutputPath =
    normalizePath (__SOURCE_DIRECTORY__ + "/data/Test1/bin/Debug/Test1.dll")

  p.OtherOptions
  |> getOutputFile
  |> shouldEqual expectedOutputPath

[<Test>]
let ``Project file parsing -- references``() =
  let p = getFSharpProjectOptions(__SOURCE_DIRECTORY__ + @"../../../test-data/Test1.fsproj")

  let references = getReferencedFilenames p.OtherOptions
  checkOption references "FSharp.Core.dll"
  checkOption references "mscorlib.dll"
  checkOption references "System.Core.dll"
  checkOption references "System.dll"
  printfn "Project file parsing -- references: references = %A" references
  references |> shouldHaveLength 4
  p.ReferencedProjects |> shouldBeEmpty

[<Test>]
let ``Project file parsing -- 2nd level references``() =
  let p = getFSharpProjectOptions(__SOURCE_DIRECTORY__ + @"../../../test-data/Test2.fsproj")
  printfn "%A" p

  let references = getReferencedFilenames p.OtherOptions
  checkOption references "FSharp.Core.dll"
  checkOption references "mscorlib.dll"
  checkOption references "System.Core.dll"
  checkOption references "System.dll"
  checkOption references "Test1.dll"
  printfn "Project file parsing -- references: references = %A" references
  references |> shouldHaveLength 5
  p.ReferencedProjects |> shouldHaveLength 1
  (snd p.ReferencedProjects.[0]).ProjectFileName |> shouldContainText (normalizePath (__SOURCE_DIRECTORY__ + @"../../../test-data/Test1.fsproj"))

[<Test>]
let ``Project file parsing -- reference project output file``() =
  let p = getFSharpProjectOptions(__SOURCE_DIRECTORY__ + @"../../../test-data/DifferingOutputDir/Dir2/Test2.fsproj")

  let expectedOutputPath =
    normalizePath (__SOURCE_DIRECTORY__ + "../../../test-data/DifferingOutputDir/Dir2/OutputDir2/Test2.exe")

  p.OtherOptions
  |> getOutputFile
  |> shouldEqual expectedOutputPath

  p.OtherOptions
  |> Array.choose (fun (o:string) -> if o.StartsWith("-r:") then o.[3..] |> Some else None)
  |> shouldContain (normalizePath (__SOURCE_DIRECTORY__ + @"../../../test-data/DifferingOutputDir/Dir1/OutputDir1/Test1.dll"))

[<Test>]
let ``Project file parsing -- Tools Version 12``() =
  let opts = getFSharpProjectOptions(__SOURCE_DIRECTORY__ + @"../../../test-data/ToolsVersion12.fsproj")
  checkOption (getReferencedFilenames opts.OtherOptions) "System.Core.dll"
//
//[<Test>]
//let ``Project file parsing -- Logging``() =
//  let projectFileName = normalizePath (__SOURCE_DIRECTORY__ + @"../../../test-data/ToolsVersion12.fsproj")
//  let _, logMap = getFSharpProjectOptionsLogged(projectFileName, enableLogging=true)
//  let log = logMap.[projectFileName]

  if runningOnMono then
    Assert.That(log, Does.Contain("Reference System.Core resolved"))
    Assert.That(log, Does.Contain("Using task ResolveAssemblyReference from Microsoft.Build.Tasks.ResolveAssemblyReference"))
  else
    Assert.That(log, Does.Contain("""Using "ResolveAssemblyReference" task from assembly "Microsoft.Build.Tasks.Core, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"."""))

[<Test>]
let ``Project file parsing -- Full path``() =
  let f = normalizePath (__SOURCE_DIRECTORY__ + @"../../../test-data/ToolsVersion12.fsproj")
  let p = getFSharpProjectOptions(f)
  p.ProjectFileName |> shouldEqual f

[<Test>]
let ``Project file parsing -- project references``() =
  let f1 = normalizePath (__SOURCE_DIRECTORY__ + @"../../../test-data/Test1.fsproj")
  let f2 = normalizePath (__SOURCE_DIRECTORY__ + @"../../../test-data/Test2.fsproj")
  let options = getFSharpProjectOptions(f2)

  options.ReferencedProjects |> shouldHaveLength 1
  fst options.ReferencedProjects.[0] |> shouldEndWith "Test1.dll"
  snd options.ReferencedProjects.[0] |> shouldEqual (getFSharpProjectOptions(f1))

[<Test>]
let ``Project file parsing -- multi language project``() =
  let f = normalizePath (__SOURCE_DIRECTORY__ + @"../../../test-data/MultiLanguageProject/ConsoleApplication1.fsproj")

  let options = getFSharpProjectOptions(f)

  options.ReferencedProjects |> shouldHaveLength 1
  options.ReferencedProjects.[0] |> fst |> shouldEndWith "ConsoleApplication2.exe"

  checkOption options.OtherOptions "ConsoleApplication2.exe"
  checkOption options.OtherOptions "ConsoleApplication3.exe"

[<Test>]
let ``Project file parsing -- PCL profile7 project``() =

    let f = normalizePath (__SOURCE_DIRECTORY__ + @"../../../test-data/Sample_VS2013_FSharp_Portable_Library_net45/Sample_VS2013_FSharp_Portable_Library_net45.fsproj")

    let options = getFSharpProjectOptions(f)
    let references =
      options.OtherOptions
      |> getReferencedFilenames
      |> Set.ofArray
    references
    |> shouldEqual
        (set [|"FSharp.Core.dll"; "Microsoft.CSharp.dll"; "Microsoft.VisualBasic.dll";
               "System.Collections.Concurrent.dll"; "System.Collections.dll";
               "System.ComponentModel.Annotations.dll";
               "System.ComponentModel.DataAnnotations.dll";
               "System.ComponentModel.EventBasedAsync.dll"; "System.ComponentModel.dll";
               "System.Core.dll"; "System.Diagnostics.Contracts.dll";
               "System.Diagnostics.Debug.dll"; "System.Diagnostics.Tools.dll";
               "System.Diagnostics.Tracing.dll"; "System.Dynamic.Runtime.dll";
               "System.Globalization.dll"; "System.IO.Compression.dll"; "System.IO.dll";
               "System.Linq.Expressions.dll"; "System.Linq.Parallel.dll";
               "System.Linq.Queryable.dll"; "System.Linq.dll"; "System.Net.Http.dll";
               "System.Net.NetworkInformation.dll"; "System.Net.Primitives.dll";
               "System.Net.Requests.dll"; "System.Net.dll"; "System.Numerics.dll";
               "System.ObjectModel.dll"; "System.Reflection.Context.dll";
               "System.Reflection.Extensions.dll"; "System.Reflection.Primitives.dll";
               "System.Reflection.dll"; "System.Resources.ResourceManager.dll";
               "System.Runtime.Extensions.dll";
               "System.Runtime.InteropServices.WindowsRuntime.dll";
               "System.Runtime.InteropServices.dll"; "System.Runtime.Numerics.dll";
               "System.Runtime.Serialization.Json.dll";
               "System.Runtime.Serialization.Primitives.dll";
               "System.Runtime.Serialization.Xml.dll"; "System.Runtime.Serialization.dll";
               "System.Runtime.dll"; "System.Security.Principal.dll";
               "System.ServiceModel.Duplex.dll"; "System.ServiceModel.Http.dll";
               "System.ServiceModel.NetTcp.dll"; "System.ServiceModel.Primitives.dll";
               "System.ServiceModel.Security.dll"; "System.ServiceModel.Web.dll";
               "System.ServiceModel.dll"; "System.Text.Encoding.Extensions.dll";
               "System.Text.Encoding.dll"; "System.Text.RegularExpressions.dll";
               "System.Threading.Tasks.Parallel.dll"; "System.Threading.Tasks.dll";
               "System.Threading.dll"; "System.Windows.dll"; "System.Xml.Linq.dll";
               "System.Xml.ReaderWriter.dll"; "System.Xml.Serialization.dll";
               "System.Xml.XDocument.dll"; "System.Xml.XmlSerializer.dll"; "System.Xml.dll";
               "System.dll"; "mscorlib.dll"|])

    checkOption options.OtherOptions "--targetprofile:netcore"

[<Test>]
let ``Project file parsing -- PCL profile78 project``() =

    let f = normalizePath (__SOURCE_DIRECTORY__ + @"../../../test-data/Sample_VS2013_FSharp_Portable_Library_net451_adjusted_to_profile78/Sample_VS2013_FSharp_Portable_Library_net451.fsproj")

    let options = getFSharpProjectOptions(f)
    let references =
      options.OtherOptions
      |> getReferencedFilenames
      |> Set.ofArray
    references
    |> shouldEqual
        (set [|"FSharp.Core.dll"; "Microsoft.CSharp.dll"; "System.Collections.dll";
               "System.ComponentModel.EventBasedAsync.dll"; "System.ComponentModel.dll";
               "System.Core.dll"; "System.Diagnostics.Contracts.dll";
               "System.Diagnostics.Debug.dll"; "System.Diagnostics.Tools.dll";
               "System.Dynamic.Runtime.dll"; "System.Globalization.dll"; "System.IO.dll";
               "System.Linq.Expressions.dll"; "System.Linq.Queryable.dll"; "System.Linq.dll";
               "System.Net.NetworkInformation.dll"; "System.Net.Primitives.dll";
               "System.Net.Requests.dll"; "System.Net.dll"; "System.ObjectModel.dll";
               "System.Reflection.Extensions.dll"; "System.Reflection.Primitives.dll";
               "System.Reflection.dll"; "System.Resources.ResourceManager.dll";
               "System.Runtime.Extensions.dll";
               "System.Runtime.InteropServices.WindowsRuntime.dll";
               "System.Runtime.Serialization.Json.dll";
               "System.Runtime.Serialization.Primitives.dll";
               "System.Runtime.Serialization.Xml.dll"; "System.Runtime.Serialization.dll";
               "System.Runtime.dll"; "System.Security.Principal.dll";
               "System.ServiceModel.Http.dll"; "System.ServiceModel.Primitives.dll";
               "System.ServiceModel.Security.dll"; "System.ServiceModel.Web.dll";
               "System.ServiceModel.dll"; "System.Text.Encoding.Extensions.dll";
               "System.Text.Encoding.dll"; "System.Text.RegularExpressions.dll";
               "System.Threading.Tasks.dll"; "System.Threading.dll"; "System.Windows.dll";
               "System.Xml.Linq.dll"; "System.Xml.ReaderWriter.dll";
               "System.Xml.Serialization.dll"; "System.Xml.XDocument.dll";
               "System.Xml.XmlSerializer.dll"; "System.Xml.dll"; "System.dll"; "mscorlib.dll"|])

    checkOption options.OtherOptions "--targetprofile:netcore"

[<Test>]
let ``Project file parsing -- PCL profile259 project``() =

    let f = normalizePath (__SOURCE_DIRECTORY__ + @"../../../test-data/Sample_VS2013_FSharp_Portable_Library_net451_adjusted_to_profile259/Sample_VS2013_FSharp_Portable_Library_net451.fsproj")

    let options = getFSharpProjectOptions(f)
    let references =
      options.OtherOptions
      |> getReferencedFilenames
      |> Set.ofArray
    references
    |> shouldEqual
        (set [|"FSharp.Core.dll"; "Microsoft.CSharp.dll"; "System.Collections.dll";
               "System.ComponentModel.EventBasedAsync.dll"; "System.ComponentModel.dll";
               "System.Core.dll"; "System.Diagnostics.Contracts.dll";
               "System.Diagnostics.Debug.dll"; "System.Diagnostics.Tools.dll";
               "System.Dynamic.Runtime.dll"; "System.Globalization.dll"; "System.IO.dll";
               "System.Linq.Expressions.dll"; "System.Linq.Queryable.dll"; "System.Linq.dll";
               "System.Net.NetworkInformation.dll"; "System.Net.Primitives.dll";
               "System.Net.Requests.dll"; "System.Net.dll"; "System.ObjectModel.dll";
               "System.Reflection.Extensions.dll"; "System.Reflection.Primitives.dll";
               "System.Reflection.dll"; "System.Resources.ResourceManager.dll";
               "System.Runtime.Extensions.dll";
               "System.Runtime.InteropServices.WindowsRuntime.dll";
               "System.Runtime.Serialization.Json.dll";
               "System.Runtime.Serialization.Primitives.dll";
               "System.Runtime.Serialization.Xml.dll"; "System.Runtime.Serialization.dll";
               "System.Runtime.dll"; "System.Security.Principal.dll";
               "System.ServiceModel.Web.dll"; "System.Text.Encoding.Extensions.dll";
               "System.Text.Encoding.dll"; "System.Text.RegularExpressions.dll";
               "System.Threading.Tasks.dll"; "System.Threading.dll"; "System.Windows.dll";
               "System.Xml.Linq.dll"; "System.Xml.ReaderWriter.dll";
               "System.Xml.Serialization.dll"; "System.Xml.XDocument.dll";
               "System.Xml.XmlSerializer.dll"; "System.Xml.dll"; "System.dll"; "mscorlib.dll"|])

    checkOption options.OtherOptions "--targetprofile:netcore"

[<Test>]
let ``Project file parsing -- Exe with a PCL reference``() =

    let f = normalizePath(__SOURCE_DIRECTORY__ + @"../../../test-data/sqlite-net-spike/sqlite-net-spike.fsproj")

    let p = getFSharpProjectOptions(f)
    let references = getReferencedFilenames p.OtherOptions |> set
    references |> shouldContain "FSharp.Core.dll"
    references |> shouldContain "SQLite.Net.Platform.Generic.dll"
    references |> shouldContain "SQLite.Net.Platform.Win32.dll"
    references |> shouldContain "SQLite.Net.dll"
    references |> shouldContain "System.Collections.Concurrent.dll"
    references |> shouldContain "System.Linq.Queryable.dll"
    references |> shouldContain "System.Resources.ResourceManager.dll"
    references |> shouldContain "System.Threading.dll"
    references |> shouldContain "System.dll"
    references |> shouldContain "mscorlib.dll"
    references |> shouldContain "System.Reflection.dll"
    references |> shouldContain "System.Reflection.Emit.Lightweight.dll"


//[<Test>]
//let ``Project file parsing -- project file contains project reference to out-of-solution project and is used in release mode``() =
//    let projectFileName = normalizePath(__SOURCE_DIRECTORY__ + @"../../../test-data/Test2.fsproj")
//    let opts = getFSharpProjectOptions(projectFileName,[("Configuration","Release")])
//    let references = getReferencedFilenamesAndContainingFolders opts.OtherOptions |> set
//    // Check the reference is to a release DLL
//    references |> should contain ("Test1.dll", "Release")
//
//[<Test>]
//let ``Project file parsing -- project file contains project reference to out-of-solution project and is used in debug mode``() =
//
//    let projectFileName = normalizePath(__SOURCE_DIRECTORY__ + @"../../../test-data/Test2.fsproj")
//    let opts = getFSharpProjectOptions(projectFileName,[("Configuration","Debug")])
//    let references = getReferencedFilenamesAndContainingFolders opts.OtherOptions |> set
//    // Check the reference is to a debug DLL
//    references |> should contain ("Test1.dll", "Debug")

[<Test>]
let ``Project file parsing -- space in file name``() =
  let opts = getFSharpProjectOptions(__SOURCE_DIRECTORY__ + @"../../../test-data/Space in name.fsproj")
  CollectionAssert.AreEqual (["Test2File2.fs"; "Test2File1.fs"], opts.ProjectFileNames |> Array.map Path.GetFileName)
  CollectionAssert.IsEmpty (getCompiledFilenames opts.OtherOptions)

[<Test>]
let ``Project file parsing -- report files``() =
  let programFilesx86Folder = System.Environment.GetEnvironmentVariable("PROGRAMFILES(X86)")
  if not runningOnMono then

   let dirRefs = programFilesx86Folder + @"\Reference Assemblies\Microsoft\FSharp\"
   printfn "Enumerating %s" dirRefs
   if Directory.Exists(dirRefs) then 
    for f in Directory.EnumerateFiles(dirRefs,"*",SearchOption.AllDirectories) do 
     printfn "File: %s" f

   let dir40 = programFilesx86Folder + @"\Microsoft SDKs\F#\4.0\"
   printfn "Enumerating %s" dir40
   if Directory.Exists(dir40) then 
    for f in Directory.EnumerateFiles(dir40,"*",SearchOption.AllDirectories) do 
     printfn "File: %s" f

   let dir41 = programFilesx86Folder + @"\Microsoft SDKs\F#\4.1\"
   printfn "Enumerating %s" dir41
   if Directory.Exists(dir41) then 
    for f in Directory.EnumerateFiles(dir41,"*",SearchOption.AllDirectories) do 
     printfn "File: %s" f



//[<Test>]
//let ``Test ProjectFileNames order for GetProjectOptionsFromScript`` () = // See #594
//    let test scriptName expected =
//        let scriptPath = __SOURCE_DIRECTORY__ + @"../../../test-data/ScriptProject/" + scriptName + ".fsx"
//        let scriptSource = File.ReadAllText scriptPath
//        let projOpts =
//            checker.GetProjectOptionsFromScript(scriptPath, scriptSource)
//            |> Async.RunSynchronously
//        projOpts.ProjectFileNames
//        |> Array.map Path.GetFileNameWithoutExtension
//        |> (=) expected
//        |> shouldEqual true
//    test "Main1" [|"BaseLib1"; "Lib1"; "Lib2"; "Main1"|]
//    test "Main2" [|"BaseLib1"; "Lib1"; "Lib2"; "Lib3"; "Main2"|]
//    test "Main3" [|"Lib3"; "Lib4"; "Main3"|]
//    test "Main4" [|"BaseLib2"; "Lib5"; "BaseLib1"; "Lib1"; "Lib2"; "Main4"|]
//
//


#if INTERACTIVE
;;
let p = getFSharpProjectOptions(__SOURCE_DIRECTORY__ + @"../../../test-data/Test2.fsproj")

let references = getReferencedFilenames p.OtherOptions

#endif