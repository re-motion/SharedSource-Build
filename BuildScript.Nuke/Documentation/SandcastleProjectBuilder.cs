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
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Serilog;

namespace Remotion.BuildScript.Documentation;

public class SandcastleProjectBuilder
{
  private readonly string _file;
  private readonly XNamespace _namespace = "http://schemas.microsoft.com/developer/msbuild/2003";
  private readonly IReadOnlyCollection<string> _namespaceSummaryFiles;
  private readonly IReadOnlyCollection<ProjectMetadata> _projectMetadataCollection;

  public SandcastleProjectBuilder (
      string file,
      IReadOnlyCollection<ProjectMetadata> projectMetadataCollection,
      IReadOnlyCollection<string> namespaceSummaryFiles)
  {
    _file = file;
    _projectMetadataCollection = projectMetadataCollection;
    _namespaceSummaryFiles = namespaceSummaryFiles;
  }

  public bool UpdateSandcastleProject ()
  {
    XDocument projectFile;
    try
    {
      projectFile = XDocument.Load(_file, LoadOptions.None);

      var propertyGroup = new XElement(_namespace + "PropertyGroup");
      propertyGroup.Add(GetDocumentationSources());

      var project = projectFile.Descendants(_namespace + "Project").Single();
      project.Add(propertyGroup);

      projectFile.Save(_file, SaveOptions.None);

      Log.Information($"Generated sandcastle project file '{_file}'.");
      return true;
    }
    catch (Exception ex)
    {
      Log.Error(ex, "Error occurred while building sandcastle project");
      return false;
    }
  }

  private XElement? GetDocumentationSources ()
  {
    try
    {
      var sources = new XElement(_namespace + "DocumentationSources");

      foreach (var projectMetadata in _projectMetadataCollection)
      foreach (var assemblyPath in projectMetadata.AssemblyPaths)
      {
        var documentation = GetAssemblyDocumentation(assemblyPath);
        if (documentation != null)
        {
          sources.Add(GetDocumentationSource(assemblyPath));
          sources.Add(GetDocumentationSource(documentation));
        }
      }

      if (_namespaceSummaryFiles != null)
      {
        foreach (var namespaceSummary in _namespaceSummaryFiles)
          sources.Add(GetDocumentationSource(namespaceSummary));
      }

      return sources;
    }
    catch (Exception ex)
    {
      Log.Error(ex, "Error occurred while getting documentation sources");
      return null;
    }
  }

  private XElement GetDocumentationSource (string path) => new(_namespace + "DocumentationSource", new XAttribute("sourceFile", path));

  private string? GetAssemblyDocumentation (string assemblyLocation)
  {
    var assemblyDocumentation = Path.ChangeExtension(assemblyLocation, "xml");

    if (!File.Exists(assemblyDocumentation))
    {
      Log.Information($"Assembly '{assemblyLocation}' does not contain documentation. Assembly will be ignored.");
      return null;
    }

    Log.Information($"Added assembly '{assemblyLocation}'.");
    return assemblyDocumentation;
  }
}
