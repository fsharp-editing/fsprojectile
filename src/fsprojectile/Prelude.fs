[<AutoOpen>]
module fsprojectile.Prelude

open System
open System.IO
open System.Diagnostics


type FileName = string
type FilePath = string

let inline debugfn msg = Printf.kprintf Debug.WriteLine msg
let inline failfn msg = Printf.kprintf Debug.Fail msg

let inline isNull v =
    match v with
    | null -> true
    | _ -> false

let inline isNotNull v = not (isNull v)

let unquote (text:string) = 
    let text = text.Trim()
    if text.StartsWith "\"" && text.EndsWith "\"" then
        text.TrimStart('"').TrimEnd('"')
    else text

let fullpath str = Path.GetFullPath str

/// String Equals Ordinal Ignore Case
let (|EqualsIC|_|) (str : string) arg =
    if String.Compare(str, arg, StringComparison.OrdinalIgnoreCase) = 0 then Some()
    else None


let (|LessThan|_|) a b  = if a < b then Some() else None

let tryCast<'T> (o: obj): 'T option =
    match o with
    | null -> None
    | :? 'T as a -> Some a
    | _ ->
        debugfn "Cannot cast %O to %O" (o.GetType()) typeof<'T>.Name
        None

/// Null coalescing operator, return non null a, otherwise b
let (?|?) a b = if isNull a then b else a

let (^) = (<|)

/// OR predicate combinator
let inline (|?|) (pred1:'a->bool) (pred2:'a->bool)  =
    fun a -> pred1 a || pred2 a

/// AND predicate combinator
let inline (|&|) (pred1:'a->bool) (pred2:'a->bool)  =
    fun a -> pred1 a && pred2 a

let (</>) path1 path2 = Path.Combine (path1, path2)


/// If arg is null raise an `ArgumentNullException` with the argname
let inline checkNullArg arg argName =
    if isNull arg then nullArg argName


open System.Xml.Linq

let inline localName x = (^a:(member Name:XName) x).LocalName

let inline private matchName (name:string) x = name = (localName x)

/// Helper function to filter a seq of XElements by matching their local name against the provided string
let inline private nameFilter name sqs = sqs |> Seq.filter ^ matchName name

let inline private hasNamed name sqs = sqs |> Seq.exists ^ matchName name

let inline private getNamed name sqs = sqs |> Seq.find ^ matchName name

let  inline private tryGetNamed name sqs = 
    (None, sqs) ||> Seq.fold (fun acc elm ->
        match acc with
        | Some _ -> acc
        | None   -> if matchName name elm then Some elm else None
    )

[<RequireQualifiedAccess>]
module XDoc =

    let elements (xdoc:#XDocument) = xdoc.Elements()

    let hasElement name (xdoc:#XDocument) =
        elements xdoc |> hasNamed name

    let getElement name (xdoc:#XDocument)  =
        elements xdoc |> getNamed name

    let tryGetElement name (xdoc:#XDocument)  =
        elements xdoc |> tryGetNamed name

    let getElements name (xdoc:#XDocument)  =
        elements xdoc |> nameFilter name

[<RequireQualifiedAccess>]
module XAttr =
    let value (xattr:XAttribute) = xattr.Value
    let parent (xattr:XAttribute) = xattr.Parent
    let previous (xattr:XAttribute) = xattr.PreviousAttribute
    let next (xattr:XAttribute) = xattr.NextAttribute

[<RequireQualifiedAccess>]
/// Functions for operating on XElements
module XElem =
    let getAttribute name (xelem:#XElement) =
        xelem.Attribute ^ XName.Get name


[<RequireQualifiedAccess>]
module Seq =
    let sortWithDescending fn source =
        source |> Seq.sortWith (fun a b -> fn b a)

[<RequireQualifiedAccess>]
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module String =
    let inline toCharArray (str : string) = str.ToCharArray()

    let lowerCaseFirstChar (str : string) =
        if String.IsNullOrEmpty str || Char.IsLower(str, 0) then str
        else
            let strArr = toCharArray str
            match Array.tryHead strArr with
            | None -> str
            | Some c ->
                strArr.[0] <- Char.ToLower c
                String strArr

    let inline contains (target : string) (str : string) = str.Contains target
    let inline equalsIgnoreCase (str1 : string) (str2 : string) = str1.Equals(str2, StringComparison.OrdinalIgnoreCase)


    /// Remove all trailing and leading whitespace from the string
    /// return null if the string is null
    let trim (value : string) =
        if isNull value then null
        else value.Trim()

    /// Splits a string into substrings based on the strings in the array separators
    let split options (separator : string []) (value : string) =
        if isNull value then null
        else value.Split(separator, options)

    let (|StartsWith|_|) pattern value =
        if String.IsNullOrWhiteSpace value then None
        elif value.StartsWith pattern then Some()
        else None

    let (|Contains|_|) pattern value =
        if String.IsNullOrWhiteSpace value then None
        elif value.Contains pattern then Some()
        else None

    open System.IO

    let getLines (str : string) =
        use reader = new StringReader(str)
        [| let line = ref (reader.ReadLine())
           while isNotNull (!line) do
               yield !line
               line := reader.ReadLine()
           if str.EndsWith "\n" then
               // last trailing space not returned
               // http://stackoverflow.com/questions/19365404/stringreader-omits-trailing-linebreak
               yield String.Empty |]

    let getNonEmptyLines (str : string) =
        use reader = new StringReader(str)
        [| let line = ref (reader.ReadLine())
           while isNotNull (!line) do
               if (!line).Length > 0 then yield !line
               line := reader.ReadLine() |]

    /// match strings with ordinal ignore case
    let inline equalsIC (str1:string) (str2:string) = str1.Equals(str2,StringComparison.OrdinalIgnoreCase)

    /// Parse a string to find the first nonempty line
    /// Return null if the string was null or only contained empty lines
    let firstNonEmptyLine (str : string) =
        use reader = new StringReader(str)

        let rec loop (line : string) =
            if isNull line then None
            elif line.Length > 0 then Some line
            else loop (reader.ReadLine())
        loop (reader.ReadLine())

open System.Text

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module StringBuilder =
    /// Pipelining function for appending a string to a stringbuilder
    let inline append (str : string) (sb : StringBuilder) = sb.Append str

    /// Pipelining function for appending a string with a '\n' to a stringbuilder
    let inline appendLine (str : string) (sb : StringBuilder) = sb.AppendLine str

    /// SideEffecting function for appending a string to a stringbuilder
    let inline appendi (str : string) (sb : StringBuilder) = sb.Append str |> ignore

    /// SideEffecting function for appending a string with a '\n' to a stringbuilder
    let inline appendLinei (str : string) (sb : StringBuilder) = sb.AppendLine str |> ignore

[<RequireQualifiedAccess>]
module Dict =
    open System.Collections.Generic


    let add key value (dict:#IDictionary<_,_>) =
        dict.[key] <- value
        dict


    let remove (key:'k) (dict:#IDictionary<_,_>) =
        dict.Remove key |> ignore
        dict


    let tryFind key (dict:#IDictionary<_,_>) =
        match dict.TryGetValue key with
        | true, value -> Some value
        | false, _ -> None


    let tryAdd key value (dict:#IDictionary<_,_>) =
        if dict.ContainsKey key then false else
        dict.Add(key,value)
        true


    let contains key (dict:#IDictionary<_,_>) = dict.ContainsKey key

    let ofSeq (xs : ('k * 'v) seq) =
        (Dictionary(), xs ) ||> Seq.fold ^ fun d (k,v) -> d.[k] <- v; d




module PropertyConverter =
    // TODO - railway this
    let toGuid propertyValue =
        match Guid.TryParse propertyValue with
        | true, value -> Some value
        | _ -> None

    let toDefineConstants propertyValue =
        if String.IsNullOrWhiteSpace propertyValue then [||]
        else propertyValue.Split([| ';' |], StringSplitOptions.RemoveEmptyEntries)

    // TODO - railway this
    let toBoolean propertyValue =
        if propertyValue = String.Empty then false else
        match Boolean.TryParse propertyValue with
        | true, value -> value
        | _ -> failwithf "Couldn't parse '%s' into a Boolean" propertyValue

    let toBooleanOr propertyValue defaultArg =
        match Boolean.TryParse propertyValue with
        | true, value -> value
        | _ -> defaultArg


[<RequireQualifiedAccess>]
module Directory =

    let fromPath path = (FileInfo path).Directory.FullName