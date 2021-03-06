$Location = $PSScriptRoot

function Load-Dependency-Dll ()
{
  $MSBuildTasksPackagePath = "$Location/../../lib/net461/"
    
  $BuildTools = "Remotion.ReleaseProcessScript.dll"
  $RestSharp = "RestSharp.dll"

  $BuildToolsDllPath = Join-Path $MSBuildTasksPackagePath $BuildTools
  $RestSharpDllPath = Join-Path $MSBuildTasksPackagePath $RestSharp

  if (-not (Test-Path $BuildToolsDllPath))
  {
    throw [System.IO.FileNotFoundException] "Could not find '$($BuildToolsDllPath)'."
  }

  if (-not (Test-Path $RestSharpDllPath))
  {
    throw [System.IO.FileNotFoundException] "Could not find '$($RestSharpDllPath)'."
  }

  Load-Dll ($BuildToolsDllPath) > $NULL
  Load-Dll ($RestSharpDllPath) > $NULL
}

function Load-Dll ($Path)
{
  #Load Dll as bytestream so file does not get locked
  $FileStream = ([System.IO.FileInfo] (Get-Item $Path)).OpenRead()
  $AssemblyBytes = New-Object byte[] $FileStream.Length
  $FileStream.Read($AssemblyBytes, 0, $FileStream.Length)
  $FileStream.Close()

  $AssemblyLoaded = [System.Reflection.Assembly]::Load($AssemblyBytes)
}