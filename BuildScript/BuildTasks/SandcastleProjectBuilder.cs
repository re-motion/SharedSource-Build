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
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Remotion.BuildScript.BuildTasks
{
  public class SandcastleProjectBuilder : Task
  {
    private readonly XNamespace _namespace = "http://schemas.microsoft.com/developer/msbuild/2003";

    [Required]
    public ITaskItem File { get; set; }

    [Required]
    public ITaskItem[] Assemblies { get; set; }

    public ITaskItem[] NamespaceSummaryFiles { get; set; }

    public override bool Execute ()
    {
      XDocument projectFile;
      try
      {
        projectFile = XDocument.Load (File.ItemSpec, LoadOptions.None);

        var propertyGroup = new XElement (_namespace + "PropertyGroup");
        propertyGroup.Add (GetDocumentationSources());

        var project = projectFile.Descendants (_namespace + "Project").Single ();
        project.Add (propertyGroup);

        projectFile.Save (File.ItemSpec, SaveOptions.None);
        
        Log.LogMessage (MessageImportance.Normal, "Generated sandcastle project file '{0}'.", File);
        return true;
      }
      catch (Exception ex)
      {
        Log.LogErrorFromException (ex);
        return false;
      }
    }

    private XElement GetDocumentationSources ()
    {
      try
      {
        var sources = new XElement (_namespace + "DocumentationSources");

        foreach (var assembly in Assemblies)
        {
          var documentation = GetAssemblyDocumentation (assembly);
          if (documentation != null)
          {
            sources.Add (GetDocumentationSource (assembly.ItemSpec));
            sources.Add (GetDocumentationSource (documentation));
          }
        }

        if (NamespaceSummaryFiles != null)
        {
          foreach (var namespaceSummary in NamespaceSummaryFiles)
            sources.Add (GetDocumentationSource (namespaceSummary.ItemSpec));
        }

        return sources;
      }
      catch (Exception ex)
      {
        Log.LogErrorFromException (ex);
        return null;
      }
    }

    private XElement GetDocumentationSource (string path)
    {
      return new XElement(_namespace + "DocumentationSource", new XAttribute("sourceFile", path));
    }

    private string GetAssemblyDocumentation (ITaskItem assembly)
    {
      var assemblyLocation = assembly.ItemSpec;
      var assemblyDocumention = Path.ChangeExtension (assemblyLocation, "xml");

      if (!System.IO.File.Exists (assemblyDocumention))
      {
        Log.LogMessage (MessageImportance.Normal, "Assembly '{0}' does not contain documentation. Assembly will be ignored.", assemblyLocation);
        return null;
      }

      Log.LogMessage (MessageImportance.Normal, "Added assembly '{0}'.", assemblyLocation);
      return assemblyDocumention;
    }
  }
}