namespace fsprojectile

open System
open System.IO
open System.Collections.Generic
open System.Runtime.Versioning
open Microsoft.Build
open Microsoft.Build.Evaluation
open Microsoft.Build.Execution


[<AutoOpen>]
module Extensions =

    type ProjectInstance with

        member self.TryGetPropertyValue propertyName =
            let value = self.GetPropertyValue propertyName
            if String.IsNullOrEmpty value then None else Some value


/// Specifies the version of the F# compiler that should be used
type LanguageVersion =
    | FSharp2
    | FSharp3
    | FSharp4

type PlatformType =
    | X86
    | X64
    | AnyCPU

    override self.ToString () = self |> function
        | X86    -> Constants.X86
        | X64    -> Constants.X64
        | AnyCPU -> Constants.AnyCPU

    static member Parse text =
        text |> function
        | EqualsIC Constants.X86 -> X86
        | EqualsIC Constants.X64 -> X64
        | EqualsIC "Any CPU" | EqualsIC Constants.AnyCPU -> AnyCPU
        | _ -> failwithf "Could not parse '%s' into a `PlatformType`" text

    static member TryParse text =
        text |> function
        | EqualsIC Constants.X86 -> Some X86
        | EqualsIC Constants.X64 -> Some X64
        | EqualsIC "Any CPU" | EqualsIC Constants.AnyCPU -> Some AnyCPU
        | _ -> Option.None

/// Determines the output of compiling the F# Project
type OutputType =
    ///  An .exe with an entry point and a console.
    | Exe
    ///   An .exe with an entry point but no console.
    | Winexe
    /// a dynamically linked library (.dll)
    | Library
    /// Build a module that can be added to another assembly (.netmodule)
    | Module

    override self.ToString() = self |> function
        | Exe     -> Constants.Exe
        | Winexe  -> Constants.Winexe
        | Library -> Constants.Library
        | Module  -> Constants.Module

    static member Parse text = text |> function
        | EqualsIC Constants.Exe     -> Exe
        | EqualsIC Constants.Winexe  -> Winexe
        | EqualsIC Constants.Library -> Library
        | EqualsIC Constants.Module  -> Module
        | _ -> failwithf "Could not parse '%s' into a `OutputType`" text

    static member TryParse text = text |> function
        | EqualsIC Constants.Exe     -> Some Exe
        | EqualsIC Constants.Winexe  -> Some Winexe
        | EqualsIC Constants.Library -> Some Library
        | EqualsIC Constants.Module  -> Some Module
        | _ -> None

// TODO - add another field to store `AdditionalDocuments` for use during ProjectInfo creation
type ProjectFileInfo = {
    ProjectGuid               : Guid option
    Name                      : string option
    ProjectFilePath           : string
    TargetFramework           : FrameworkName option
    FrameworkVersion          : string option
    AssemblyName              : string
    OutputPath                : string
    OutputType                : OutputType
    SignAssembly              : bool
    AssemblyOriginatorKeyFile : string option
    GenerateXmlDocumentation  : string option
    Options                   : string []
    PreprocessorSymbolNames   : string []
    CompileFiles              : string []
    ResourceFiles             : string []
    EmbeddedResourceFiles     : string []
    ContentFiles              : string []
    PageFiles                 : string []
    ScriptFiles               : string []
    OtherFiles                : string []
    References                : string []
    /// Collection of paths fsproj files for the project references
    ProjectReferences         : string []
    Analyzers                 : string []
//    LogOutput                 : string
} with
    member self.ProjectDirectory = Path.GetDirectoryName self.ProjectFilePath


[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module ProjectFileInfo =

    let getFullPath (projectItem : ProjectItemInstance) = projectItem.GetMetadataValue MetadataName.FullPath
    let isProjectReference (projectItem : ProjectItemInstance) : bool =
        projectItem.GetMetadataValue(MetadataName.ReferenceSourceTarget)
                   .Equals(ItemName.ProjectReference, StringComparison.OrdinalIgnoreCase)


    let create (projectFilePath:string) =
        if not (File.Exists projectFilePath) then failwithf "No project file found at '%s'" projectFilePath else

        let manager = BuildManager.DefaultBuildManager

        let buildParam = BuildParameters(DetailedSummary=true)
        let project = Project projectFilePath
        let projectInstance = project.CreateProjectInstance()


        let requestReferences =
            BuildRequestData (projectInstance,
                [|  "ResolveAssemblyReferences"
                    "ResolveProjectReferences"
                    "ResolveReferenceDependencies"
                |])

//        let fromBuildRes (targetName:string) (result:BuildResult) =
//            if not ^ result.ResultsByTarget.ContainsKey targetName then [||] else
//            result.ResultsByTarget.[targetName].Items

        let result = manager.Build(buildParam,requestReferences)


        let fromBuildRes targetName =
            if result.ResultsByTarget.ContainsKey targetName then
                result.ResultsByTarget.[targetName].Items
                |> Seq.map(fun r -> r.ItemSpec)
                |> Array.ofSeq
            else
                [||]

        let projectReferences = fromBuildRes "ResolveProjectReferences"

        let references = fromBuildRes "ResolveAssemblyReferences"

        let getItems itemType =
            if project.ItemTypes.Contains itemType then
                project.GetItems itemType
                |> Seq.map(fun item -> item.EvaluatedInclude)
            else
                Seq.empty

        let getProperty propName =
            let s = project.GetPropertyValue propName
            if  String.IsNullOrWhiteSpace s then None
            else Some s

        let outFileOpt =  getProperty "TargetPath"

        let getbool (s:string option) =
            match s with
            | None -> false
            | Some s ->
                match Boolean.TryParse s with
                | true, result -> result | false, _ -> false

        let split (s:string option) (cs:char[]) =
            match s with
            | None -> [||]
            | Some s ->
                if String.IsNullOrWhiteSpace s then [||]
                else s.Split(cs, StringSplitOptions.RemoveEmptyEntries)


        let fxVer               = getProperty "TargetFrameworkVersion"
        let optimize            = getProperty "Optimize" |> getbool
        let assemblyNameOpt     = getProperty "AssemblyName"
        let tailcalls           = getProperty "Tailcalls" |> getbool
        let outputPathOpt       = getProperty "OutputPath"
        let docFileOpt          = getProperty "DocumentationFile"
        let outputTypeOpt       = getProperty "OutputType"
        let debugTypeOpt        = getProperty "DebugType"
        let baseAddressOpt      = getProperty "BaseAddress"
        let sigFileOpt          = getProperty "GenerateSignatureFile"
        let keyFileOpt          = getProperty "KeyFile"
        let pdbFileOpt          = getProperty "PdbFile"
        let platformOpt         = getProperty "Platform"
        let targetTypeOpt       = getProperty "TargetType"
        let versionFileOpt      = getProperty "VersionFile"
        let targetProfileOpt    = getProperty "TargetProfile"
        let warnLevelOpt        = getProperty "Warn"
        let subsystemVersionOpt = getProperty "SubsystemVersion"
        let win32ResOpt         = getProperty "Win32ResourceFile"
        let heOpt               = getProperty "HighEntropyVA" |> getbool
        let win32ManifestOpt    = getProperty "Win32ManifestFile"
        let debugSymbols        = getProperty "DebugSymbols" |> getbool
        let prefer32bit         = getProperty "Prefer32Bit" |> getbool
        let warnAsError         = getProperty "TreatWarningsAsErrors" |> getbool
        let defines             = split (getProperty "DefineConstants") [| ';'; ','; ' ' |]
        let nowarn              = split (getProperty "NoWarn") [| ';'; ','; ' ' |]
        let warningsAsError     = split (getProperty "WarningsAsErrors") [| ';'; ','; ' ' |]
        let libPaths            = split (getProperty "ReferencePath") [| ';'; ',' |]
        let otherFlags          = split (getProperty "OtherFlags") [| ' ' |]
        let isLib               =
            match outputTypeOpt with
            | None -> false
            | Some prop -> prop ="Library"

        let pages = getItems "Page"
        let embeddedResources = getItems "EmbeddedResource"
        let files = getItems "Compile"
        let resources = getItems "Resource"
        let noaction = getItems "None"
        let content = getItems "Content"


        let fscFlag  str (opt:string option) = seq{
            match  opt with
            | None -> ()
            | Some s -> yield str + s
        }

        let fscFlags flag (ls:string []) = seq {
            for x in ls do
                if not (String.IsNullOrWhiteSpace x) then yield flag + x
        }

        let options = [
            yield "--simpleresolution"
            yield "--noframework"
            yield! fscFlag "--out:" outFileOpt
            yield! fscFlag  "--doc:" docFileOpt
            yield! fscFlag  "--baseaddress:" baseAddressOpt
            yield! fscFlag  "--keyfile:" keyFileOpt
            yield! fscFlag  "--sig:" sigFileOpt
            yield! fscFlag  "--pdb:" pdbFileOpt
            yield! fscFlag  "--versionfile:" versionFileOpt
            yield! fscFlag  "--warn:" warnLevelOpt
            yield! fscFlag "--subsystemversion:" subsystemVersionOpt
            if heOpt then yield "--highentropyva+"
            yield! fscFlag  "--win32res:" win32ResOpt
            yield! fscFlag  "--win32manifest:" win32ManifestOpt
            yield! fscFlag  "--targetprofile:"  targetProfileOpt
            yield "--fullpaths"
            yield "--flaterrors"
            if warnAsError then yield "--warnaserror"
            yield
                if isLib then "--target:library"
                else "--target:exe"
            yield! fscFlags "--define:" defines
            yield! fscFlags "--nowarn:" nowarn
            yield! fscFlags "--warnaserror:" warningsAsError
            yield if debugSymbols then "--debug+" else "--debug-"
            yield if optimize then "--optimize+"  else "--optimize-"
            yield if tailcalls then "--tailcalls+" else "--tailcalls-"
            match debugTypeOpt with
            | None -> ()
            | Some debugType ->
                match debugType.ToUpperInvariant() with
                | "NONE" -> ()
                | "PDBONLY" -> yield "--debug:pdbonly"
                | "FULL" -> yield "--debug:full"
                | _ -> ()
            match platformOpt |> Option.map (fun o -> o.ToUpperInvariant()), prefer32bit,
                    targetTypeOpt |> Option.map (fun o -> o.ToUpperInvariant()) with
            | Some "ANYCPU", true, Some "EXE" | Some "ANYCPU", true, Some "WINEXE" -> yield "--platform:anycpu32bitpreferred"
            | Some "ANYCPU", _, _ -> yield "--platform:anycpu"
            | Some "X86", _, _ -> yield "--platform:x86"
            | Some "X64", _, _ -> yield "--platform:x64"
            | Some "ITANIUM", _, _ -> yield "--platform:Itanium"
            | _ -> ()
            match targetTypeOpt |> Option.map (fun o -> o.ToUpperInvariant()) with
            | Some "LIBRARY" -> yield "--target:library"
            | Some "EXE" -> yield "--target:exe"
            | Some "WINEXE" -> yield "--target:winexe"
            | Some "MODULE" -> yield "--target:module"
            | _ -> ()
            yield! otherFlags
            yield! Seq.map((+)"--resource:") resources
            yield! Seq.map((+)"--lib:") libPaths
            yield! Seq.map((+)"--r:") references
            yield! files
        ]

        let getItemPaths itemName =
            projectInstance.GetItems itemName |> Seq.map getFullPath

        let filterItemPaths predicate itemName =
            projectInstance.GetItems itemName
            |> Seq.filter predicate
            |> Seq.map getFullPath

        let isScriptFile path =
            String.equalsIC (path |> Path.GetExtension) ".fsx"

        let sourceFiles = getItemPaths ItemName.Compile

        let otherFiles  =
            filterItemPaths (fun x -> not ^ isScriptFile x.EvaluatedInclude) ItemName.None

        let scriptFiles  =
            filterItemPaths (fun x -> isScriptFile x.EvaluatedInclude) ItemName.None

        let references =
            projectInstance.GetItems ItemName.ReferencePath
            |> Seq.filter (not<<isProjectReference)
            |> Seq.map getFullPath

        let projectReferences =
            projectInstance.GetItems ItemName.ProjectReference
            |> Seq.filter isProjectReference
            |> Seq.map getFullPath

        let analyzers = getItemPaths ItemName.Analyzer

        let projectGuid =
            projectInstance.TryGetPropertyValue Property.ProjectGuid
            |> Option.bind PropertyConverter.toGuid


        let defineConstants =
            projectInstance.GetPropertyValue Property.DefineConstants
            |> PropertyConverter.toDefineConstants


        let projectName     = projectInstance.TryGetPropertyValue Property.ProjectName
        let assemblyName    = projectInstance.GetPropertyValue Property.AssemblyName
        let targetPath      = projectInstance.GetPropertyValue Property.TargetPath
        let targetFramework = projectInstance.TryGetPropertyValue Property.TargetFrameworkMoniker |> Option.map FrameworkName
        let assemblyKeyFile = projectInstance.TryGetPropertyValue Property.AssemblyOriginatorKeyFile
        let signAssembly    = PropertyConverter.toBoolean <| projectInstance.GetPropertyValue Property.SignAssembly
        let outputType      = OutputType.Parse <| projectInstance.GetPropertyValue Property.OutputType
        let xmlDocs         = projectInstance.TryGetPropertyValue Property.DocumentationFile

        {   ProjectFilePath           = projectFilePath
            ProjectGuid               = projectGuid
            Name                      = projectName
            TargetFramework           = targetFramework
            FrameworkVersion          = fxVer
            AssemblyName              = assemblyName
            OutputPath                = targetPath
            OutputType                = outputType
            SignAssembly              = signAssembly
            AssemblyOriginatorKeyFile = assemblyKeyFile
            GenerateXmlDocumentation  = xmlDocs
            PreprocessorSymbolNames   = defineConstants   |> Array.ofSeq
            CompileFiles              = sourceFiles       |> Array.ofSeq
            PageFiles                 = pages             |> Array.ofSeq
            ContentFiles              = content           |> Array.ofSeq
            ScriptFiles               = scriptFiles       |> Array.ofSeq
            ResourceFiles             = resources         |> Array.ofSeq
            EmbeddedResourceFiles     = embeddedResources |> Array.ofSeq
            OtherFiles                = otherFiles        |> Array.ofSeq
            References                = references        |> Array.ofSeq
            ProjectReferences         = projectReferences |> Array.ofSeq
            Analyzers                 = analyzers         |> Array.ofSeq
            Options                   = options           |> Array.ofSeq
        }

#if NET46
    open Microsoft.FSharp.Compiler.SourceCodeServices
#else
    open ProjectOptionsShim
#endif


    let toFSharpProjectOptions (projInfoDict:Dictionary<string,ProjectFileInfo>) (projInfo:ProjectFileInfo) =
        // proj info store and proj opts store use the full path of the project file as their key
        let projOptsStore = Dictionary<string,FSharpProjectOptions>()

        let rec generate (projInfo:ProjectFileInfo) : FSharpProjectOptions =

            let getProjectRefs (projInfo:ProjectFileInfo): (string * FSharpProjectOptions)[] =
                let existingProjectRefPaths, newProjectRefPaths =
                    // check the projInfoDict (all the projects in the workspace/solution) for a referenced project
                    projInfo.ProjectReferences
                    |> Array.partition^ fun projRefPath -> Dict.contains projRefPath projInfoDict

                // this will ensure all projects exist in the projInfoDict with corresponding projectFileInfo
                newProjectRefPaths
                |> Array.iter ^ fun path -> create path |> fun pinfo -> projInfoDict.Add(pinfo.ProjectFilePath,pinfo)

                Seq.append existingProjectRefPaths newProjectRefPaths
                |> Seq.map ^ fun path ->
                    let projInfo = projInfoDict.[path]
                    // if we've already generated FSharpProjectOptions for this project, use those
                    if projOptsStore.ContainsKey projInfo.ProjectFilePath then
                        (projInfo.OutputPath , projOptsStore.[projInfo.ProjectFilePath])
                    else
                        let fsinfo = generate projInfo
                        projOptsStore.Add (projInfo.ProjectFilePath, fsinfo)
                        (projInfo.OutputPath, fsinfo)
                |> Array.ofSeq

            {   ProjectFileName                  = projInfo.ProjectFilePath
                ProjectFileNames                 = projInfo.CompileFiles |> Seq.map Path.GetFullPath |> Array.ofSeq
                OtherOptions                     = projInfo.Options
                ReferencedProjects               = getProjectRefs projInfo
                IsIncompleteTypeCheckEnvironment = false
                UseScriptResolutionRules         = false
                LoadTime                         = System.DateTime.Now
                UnresolvedReferences             = None
            }
        let fsprojOptions = generate projInfo
        fsprojOptions, projInfoDict



[<AutoOpen>]
module Utility =

    let getFSharpProjectOptions (projectPath:string) =
        ProjectFileInfo.create projectPath
        |> ProjectFileInfo.toFSharpProjectOptions (Dictionary())
        |> fst





//  with
//    static member Create (projectInfo:Proje)
