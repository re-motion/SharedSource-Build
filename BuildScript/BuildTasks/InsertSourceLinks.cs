// Copyright (c) rubicon IT GmbH, www.rubicon.eu
//
// See the NOTICE file distributed with this work for additional information
// regarding copyright ownership.  rubicon licenses this file to you under 
// the Apache License, Version 2.0 (the "License"); you may not use this 
// file except in compliance with the License.  You may obtain a copy of the 
// License at
//
//   http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software 
// distributed under the License is distributed on an "AS IS" BASIS, WITHOUT 
// WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.  See the 
// License for the specific language governing permissions and limitations
// under the License.
// 

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.Win32;

namespace Remotion.BuildScript.BuildTasks
{
  /// <summary>
  /// Inserts links to a source server into .pdb files. Those are used to download the source files, when the pdb is used externally. 
  /// </summary>
  public class InsertSourceLinks : ToolTask
  {
    [Required]
    public string VcsUrlTemplate { get; set; }

    [Required]
    public string ProjectBaseDirectory { get; set; }

    [Required]
    public ITaskItem BuildOutputFile { get; set; }

    /// <summary>
    /// If true the windows credentials are submitted to the source server when requesting the source code.
    /// This flag is ignored when the <see cref="VcsCommandTemplate"/> is overridden. 
    /// </summary>
    public bool UseWindowsCredentials { get; set; }

    public string VcsCommandTemplate { get; set; }

    protected override string GenerateFullPathToTool ()
    {
      string registryKeyPath = @"SOFTWARE\Microsoft\Windows Kits\Installed Roots";
      using (var registryHive = RegistryKey.OpenBaseKey (RegistryHive.LocalMachine, RegistryView.Registry32))
      {
        using (var key = registryHive.OpenSubKey (registryKeyPath))
        {
          if (key == null)
            throw new InvalidOperationException (string.Format ("Could not open Registry key '{0}'.", registryKeyPath));

          var kitsRootNames = key.GetValueNames().Where (n => n.StartsWith ("KitsRoot", StringComparison.InvariantCultureIgnoreCase)).ToArray();
          if (!kitsRootNames.Any())
            throw new InvalidOperationException (string.Format ("Could not find any 'KitsRoot*' entries in Registry key '{0}'.", registryKeyPath));

          var kitsRootPaths = kitsRootNames.Select (n => (string) key.GetValue (n)).Where (v => !string.IsNullOrEmpty (v)).ToArray();
          var toolPaths = kitsRootPaths.Select (p => Path.Combine (p, @"Debuggers\x86\srcsrv", ToolName)).Where (File.Exists).ToArray();

          if (!toolPaths.Any())
          {
            var lineSeparator = Environment.NewLine + "  * ";
            throw new InvalidOperationException (
                string.Format (
                    "Could not find Windows Debug SDK at the following locations:{0}{1}",
                    lineSeparator,
                    string.Join (lineSeparator, kitsRootPaths)));
          }

          var latestToolPath = toolPaths.OrderByDescending (GetFileVersion).First();
          return latestToolPath;
        }
      }
    }

    private Version GetFileVersion (string path)
    {
      Version version;
      if (Version.TryParse (FileVersionInfo.GetVersionInfo (path).FileVersion, out version))
        return version;
      return new Version();
    }

    protected override string GenerateCommandLineCommands ()
    {
      return string.Format ("-w -s:srcsrv -i:\"{0}\" -p:\"{1}\"", GetSrcsrvFile(), GetPdbFile());
    }

    public override bool Execute ()
    {
      if (string.IsNullOrEmpty (ToolPath))
      {
        try
        {
          ToolPath = Path.GetDirectoryName (GenerateFullPathToTool());
        }
        catch (Exception ex)
        {
          Log.LogErrorFromException (ex);
          return false;
        }
      }

      var pdbFile = GetPdbFile();
      if (!File.Exists (pdbFile))
      {
        Log.LogError ("No PDB-files was found for BuildOutputFile '{0}'.", BuildOutputFile);
        return false;
      }

      string srcsrvFile;
      try
      {
        srcsrvFile = CreateSrcsrvFile (pdbFile, ToolPath);
      }
      catch (Exception ex)
      {
        Log.LogErrorFromException (ex);
        return false;
      }

      try
      {
        return base.Execute();
      }
      finally
      {
        if (File.Exists (srcsrvFile))
          File.Delete (srcsrvFile);
      }
    }

    protected override string ToolName
    {
      get { return "pdbstr.exe"; }
    }

    private string GetSrcsrvFile ()
    {
      return Path.ChangeExtension (BuildOutputFile.ToString(), ".srcsrv");
    }

    private string GetPdbFile ()
    {
      return Path.ChangeExtension (BuildOutputFile.ToString(), ".pdb");
    }

    /// <summary>
    /// See http://msdn.microsoft.com/en-us/library/windows/hardware/ff551958.aspx for specification of file format.
    /// Note: Version 2 does not seem to work.
    /// </summary>
    private string CreateSrcsrvFile (string pdbFile, string toolPath)
    {
      string srcsrvFile = GetSrcsrvFile();
      try
      {
        using (var fileStream = File.CreateText (srcsrvFile))
        {
          var sourceServerFileUrl = string.Format (VcsUrlTemplate, "%var2%");
          var useWindowsCredentialsPowershellString = UseWindowsCredentials ? "$True" : "$False";

          var sourceServerCommandTemplate = string.IsNullOrEmpty (VcsCommandTemplate)
              ? "powershell.exe -NoProfile -Command \"& "
                + "{{$wc = (New-Object System.Net.WebClient); $wc.UseDefaultCredentials = "
                + useWindowsCredentialsPowershellString
                + "; $wc.DownloadFile('{0}','{1}')}}\""
              : VcsCommandTemplate;

          fileStream.WriteLine ("SRCSRV: ini ------------------------------------------------");
          fileStream.WriteLine ("VERSION=1");
          fileStream.WriteLine ("SRCSRV: variables ------------------------------------------");
          fileStream.WriteLine ("SRCSRVTRG=%targ%\\{0}\\%fnbksl%(%var2%)", Guid.NewGuid());
          fileStream.WriteLine ("SRCSRVCMD=" + string.Format (sourceServerCommandTemplate, sourceServerFileUrl, "%SRCSRVTRG%"));
          fileStream.WriteLine ("SRCSRV: source files ---------------------------------------");

          var sourceFileCount = 0;
          foreach (var kvp in GetSourceFiles (pdbFile, toolPath))
          {
            fileStream.Write (kvp.Key);
            fileStream.Write ('*');
            fileStream.Write (kvp.Value); // represents the value for %var2%
            fileStream.WriteLine();
            sourceFileCount++;
          }
          if (sourceFileCount == 0)
          {
            Log.LogWarning (
                "No source files have been indexed for BuildOutputFile '{0}' and ProjectBaseDirectory '{1}'.",
                BuildOutputFile,
                ProjectBaseDirectory);
          }
          else
          {
            Log.LogMessage ("Applied source indices for {0} source files of BuildOutputFile '{1}'.", sourceFileCount, BuildOutputFile);
          }
          fileStream.WriteLine ("SRCSRV: end ------------------------------------------------");
        }
        return srcsrvFile;
      }
      catch
      {
        File.Delete (srcsrvFile);
        throw;
      }
    }

    private IEnumerable<KeyValuePair<string,string>> GetSourceFiles (string pdbFile, string toolPath)
    {
      var projectBaseUrl = new Uri (Path.GetFullPath (ProjectBaseDirectory + "\\"));

      int exitCode;
      string extractedCommandOutput = Execute (Path.Combine (toolPath, "srctool.exe"), "-r " + pdbFile, true, out exitCode);

      using (var reader = new StringReader (extractedCommandOutput))
      {
        while (true)
        {
          var line = reader.ReadLine();
          if (line == null)
            yield break;

          if (string.IsNullOrWhiteSpace (line))
            continue;

          // last line is srctool.exe summary.
          Uri sourceFileUrl;
          if (!Uri.TryCreate (line, UriKind.Absolute, out sourceFileUrl))
            continue;

          if (!projectBaseUrl.IsBaseOf (sourceFileUrl))
            continue;

          var relativeSourceFileUrl = projectBaseUrl.MakeRelativeUri (sourceFileUrl);
          yield return new KeyValuePair<string, string> (line, relativeSourceFileUrl.ToString());
        }
      }
    }

    private string Execute (string cmd, string args, bool ignoreExitCode, out int exitCode)
    {
      Process p = new Process
                  {
                      StartInfo = new ProcessStartInfo (cmd, args)
                                  {
                                      UseShellExecute = false,
                                      RedirectStandardOutput = true,
                                      CreateNoWindow = true
                                  }
                  };

      p.Start();
      string output = p.StandardOutput.ReadToEnd();
      p.WaitForExit();
      exitCode = p.ExitCode;

      if (!ignoreExitCode && p.ExitCode != 0)
      {
        var errorMessage = string.Format ("Executable '{0}' failed with error code {1}, output: {2}", cmd, p.ExitCode, output);
        throw new InvalidOperationException (errorMessage);
      }

      return output;
    }
  }
}