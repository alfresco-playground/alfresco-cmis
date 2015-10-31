#I "packages/FAKE/tools"
#r "FakeLib.dll"

open Fake
open Fake.FscHelper
open Fake.FileHelper

let init() =
    CreateDir "release"
    "library/DotCmis.dll" |> CopyFile "release"

Target "build" (fun _ ->
        ["src/CmisClient.fsx"]
        |> Fsc (fun p -> { p with Output = "release/program.exe"; FscTarget = FscTarget.Exe})
)

init()
RunTargetOrDefault "build"