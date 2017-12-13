#r @"packages/build/FAKE/tools/FakeLib.dll"

open System

open Fake
open SCPHelper
open FileSystemHelper

let serverPath = "./src/Server" |> FullName
let serverProj = serverPath </> "Server.fsproj"
let clientPath = "./src/Client" |> FullName

let platformTool tool winTool =
  let tool = if isUnix then tool else winTool
  tool
  |> ProcessHelper.tryFindFileOnPath
  |> function Some t -> t | _ -> failwithf "%s not found" tool

let nodeTool = platformTool "node" "node.exe"
let yarnTool = platformTool "yarn" "yarn.cmd"

let mutable dotnetCli = "dotnet"

let run cmd args workingDir =
  let result =
    ExecProcess (fun info ->
      info.FileName <- cmd
      info.WorkingDirectory <- workingDir
      info.Arguments <- args) TimeSpan.MaxValue
  if result <> 0 then failwithf "'%s %s' failed" cmd args

Target "Clean" DoNothing

Target "InstallDotNetCore" (fun _ ->
  dotnetCli <- DotNetCli.InstallDotNetSDK "2.0.3"
)

Target "InstallClient" (fun _ ->
  printfn "Node version:"
  run nodeTool "--version" __SOURCE_DIRECTORY__
  printfn "Yarn version:"
  run yarnTool "--version" __SOURCE_DIRECTORY__
  run yarnTool "install --frozen-lockfile" __SOURCE_DIRECTORY__
  run dotnetCli "restore" clientPath
)

Target "RestoreServer" (fun () -> 
  run dotnetCli "restore" serverPath
)

Target "Build" (fun () ->
  run dotnetCli "build" serverPath
  run dotnetCli "fable webpack -- -p" clientPath
)

Target "Go" (fun() -> 
  run dotnetCli "build --no-restore" serverPath
  !! "*.dll"
    |> SetBaseDir (serverPath </> "bin/linux-arm/Debug/netcoreapp2.0")
    |> Seq.map (fun file -> printfn "upload %s" file; file)
    |> Seq.iter (fun file -> SCP id file "pidom:srv")
  let sshConfig = (fun p -> { p with RemoteHost="pidom"; RemoteUser="peter" })
  SSH sshConfig "dotnet/dotnet srv/Server.dll"
)

Target "FullPush" (fun() -> 
  run dotnetCli "publish" serverPath
  SCP id (serverPath </> "bin/linux-arm/Debug/netcoreapp2.0/publish") "pidom:srv"
  let sshConfig = (fun p -> { p with RemoteHost="pidom"; RemoteUser="peter" })
  SSH sshConfig "killall -e dotnet"
  SSH sshConfig "dotnet/dotnet srv/Server.dll"
)

Target "Run" (fun () ->
  let server = async {
    run dotnetCli "watch run" serverPath
  }
  let client = async {
    run dotnetCli "fable webpack-dev-server" clientPath
  }
  let browser = async {
    Threading.Thread.Sleep 5000
    Diagnostics.Process.Start "http://localhost:8080" |> ignore
  }

  //[ server; client; browser]
  [client]
  |> Async.Parallel
  |> Async.RunSynchronously
  |> ignore
)

"Clean"
//  ==> "InstallDotNetCore"
//  ==> "InstallClient"
  ==> "Build"

"InstallClient"
  ==> "RestoreServer"
  ==> "Run"

RunTargetOrDefault "Build"