
#if INTERACTIVE
#r "../../Debug/fcs/net45/FSharp.Compiler.Service.dll" // note, run 'build fcs debug' to generate this, this DLL has a public API so can be used from F# Interactive
#r "../../packages/NUnit.3.5.0/lib/net45/nunit.framework.dll"
#load "FsUnit.fs"
#load "Common.fs"
#else
module Tests.Service.AnalyzeAndBuildTests
#endif

open Microsoft.FSharp.Compiler
open Microsoft.FSharp.Compiler.SourceCodeServices

open NUnit.Framework
open FsUnit
open System
open System.IO

open System
open System.Collections.Generic
open Microsoft.FSharp.Compiler.SourceCodeServices
open FSharp.Compiler.Service.Tests.Common

let internal checker = FSharpChecker.Create(keepAssemblyContents = true)

/// Extract range info 
let internal tups (m:Range.range) = (m.StartLine, m.StartColumn), (m.EndLine, m.EndColumn)


module internal Project1A = 
    open System.IO

    let fileName1 = Path.ChangeExtension(Path.GetTempFileName(), ".fs")
    let baseName = Path.GetTempFileName()
    let dllName = Path.ChangeExtension(baseName, ".dll")
    let projFileName = Path.ChangeExtension(baseName, ".fsproj")
    let fileSource1 = """
module Project1A

/// This is type C
type C() = 
    static member M(arg1: int, arg2: int, ?arg3 : int) = arg1 + arg2 + defaultArg arg3 4

/// This is x1
let x1 = C.M(arg1 = 3, arg2 = 4, arg3 = 5)

/// This is x2
let x2 = C.M(arg1 = 3, arg2 = 4, ?arg3 = Some 5)

/// This is type U
type U = 

   /// This is Case1
   | Case1 of int

   /// This is Case2
   | Case2 of string
    """
    File.WriteAllText(fileName1, fileSource1)

    let cleanFileName a = if a = fileName1 then "file1" else "??"

    let fileNames = [fileName1]
    let args = mkProjectCommandLineArgs (dllName, fileNames)
    let options =  checker.GetProjectOptionsFromCommandLineArgs (projFileName, args)



//-----------------------------------------------------------------------------------------
module internal Project1B = 
    open System.IO

    let fileName1 = Path.ChangeExtension(Path.GetTempFileName(), ".fs")
    let baseName = Path.GetTempFileName()
    let dllName = Path.ChangeExtension(baseName, ".dll")
    let projFileName = Path.ChangeExtension(baseName, ".fsproj")
    let fileSource1 = """
module Project1B

type A = B of xxx: int * yyy : int
let b = B(xxx=1, yyy=2)

let x = 
    match b with
    // does not find usage here
    | B (xxx = a; yyy = b) -> ()
    """
    File.WriteAllText(fileName1, fileSource1)

    let cleanFileName a = if a = fileName1 then "file1" else "??"

    let fileNames = [fileName1]
    let args = mkProjectCommandLineArgs (dllName, fileNames)
    let options =  checker.GetProjectOptionsFromCommandLineArgs (projFileName, args)


// A project referencing two sub-projects
module internal MultiProject1 = 
    open System.IO

    let fileName1 = Path.ChangeExtension(Path.GetTempFileName(), ".fs")
    let baseName = Path.GetTempFileName()
    let dllName = Path.ChangeExtension(baseName, ".dll")
    let projFileName = Path.ChangeExtension(baseName, ".fsproj")
    let fileSource1 = """

module MultiProject1

open Project1A
open Project1B

let p = (Project1A.x1, Project1B.b)
let c = C()
let u = Case1 3
    """
    File.WriteAllText(fileName1, fileSource1)

    let fileNames = [fileName1]
    let args = mkProjectCommandLineArgs (dllName, fileNames)
    let options = 
        let options =  checker.GetProjectOptionsFromCommandLineArgs (projFileName, args)
        { options with 
            OtherOptions = Array.append options.OtherOptions [| ("-r:" + Project1A.dllName); ("-r:" + Project1B.dllName) |]
        }
    let cleanFileName a = if a = fileName1 then "file1" else "??"

[<Test>]
let ``Test multi project 1 whole project errors`` () = 

    //let projectA = checker.ParseAndCheckProject(Project1A.options) |> Async.RunSynchronously
    //let errA, resA = checker.Compile(projectA) |> Async.RunSynchronously
    let errA, resA = checker.Compile(Project1A.args) |> Async.RunSynchronously
    
    //let projectB = checker.ParseAndCheckProject(Project1B.options) |> Async.RunSynchronously
    //let errB, resB = checker.Compile(projectB) |> Async.RunSynchronously     
    let errB, resB = checker.Compile(Project1B.args) |> Async.RunSynchronously     

    let wholeProjectResults = checker.ParseAndCheckProject(MultiProject1.options) |> Async.RunSynchronously

    for e in wholeProjectResults.Errors do 
        printfn "multi project 1 error: <<<%s>>>" e.Message

    wholeProjectResults .Errors.Length |> shouldEqual 0
    wholeProjectResults.ProjectContext.GetReferencedAssemblies().Length |> shouldEqual 6
