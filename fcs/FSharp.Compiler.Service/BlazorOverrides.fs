namespace System.Diagnostics

open System

type Debug =
    static member inline Assert (_: bool, _: string) = ()
    static member inline Assert (_: bool) = ()

type Trace =
    static member inline TraceInformation(_: string, [<ParamArray>] _args: obj[]) = ()
