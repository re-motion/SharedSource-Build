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
using System.Xml;
using Remotion.BuildScript;

namespace BuildScript.Nuke.IntegrationTests;

public class BuildConfigurationSetup
{
  private readonly string _customizationPath;
  private readonly string _testSolutionPath;

  public BuildConfigurationSetup (string testSolutionPath, string customizationPath)
  {
    _testSolutionPath = testSolutionPath;
    _customizationPath = customizationPath;
  }

  public void SetVersionProps (string version)
  {
    var xmlDocument = new XmlDocument();
    xmlDocument.Load($"{_customizationPath}/Version.props");
    SetPropertiesInnerText("Version", version, xmlDocument);
    xmlDocument.Save($"{_customizationPath}/Version.props");
  }

  public void SetPropertiesProps (AssemblyMetadata assemblyMetadata)
  {
    var xmlDocument = new XmlDocument();
    xmlDocument.Load($"{_customizationPath}/Properties.props");
    SetPropertiesInnerText("AssemblyInfoFile", assemblyMetadata.AssemblyInfoFile, xmlDocument);
    SetPropertiesInnerText("CompanyName", assemblyMetadata.CompanyName, xmlDocument);
    SetPropertiesInnerText("CompanyUrl", assemblyMetadata.CompanyUrl, xmlDocument);
    SetPropertiesInnerText("Copyright", assemblyMetadata.Copyright, xmlDocument);
    SetPropertiesInnerText("ProductName", assemblyMetadata.ProductName, xmlDocument);
    xmlDocument.Save($"{_customizationPath}/Properties.props");
  }

  public void SetNormalTestConfigurationInProjectProps (string testConfig)
  {
    var xmlDocument = new XmlDocument();
    xmlDocument.Load($"{_customizationPath}/Projects.props");
    SetPropertiesInnerText("NormalTestConfiguration", testConfig, xmlDocument);
    xmlDocument.Save($"{_customizationPath}/Projects.props");
  }

  public void SetProjectsProps (IReadOnlyCollection<ProjectOutput> projectReleaseOutputs, IReadOnlyCollection<ProjectOutput> projectTestOutputs)
  {
    var xmlDocument = new XmlDocument();
    xmlDocument.Load($"{_customizationPath}/Projects.props");
    var itemGroupNode = xmlDocument.GetElementsByTagName("ItemGroup")[0];
    itemGroupNode!.RemoveAll();

    foreach (var projectReleaseOutput in projectReleaseOutputs)
    {
      var releaseProjectFilesNode = xmlDocument.CreateElement("ReleaseProjectFiles");
      releaseProjectFilesNode.SetAttribute("Include", $@"$(SolutionDirectory)\{projectReleaseOutput.Name}\{projectReleaseOutput.Name}.csproj");
      if (projectReleaseOutput.IsDocumentationProject)
      {
        var createDocumentationFileNode = xmlDocument.CreateElement("CreateDocumentationFile");
        createDocumentationFileNode.InnerText = "True";
        releaseProjectFilesNode.AppendChild(createDocumentationFileNode);
      }

      itemGroupNode.AppendChild(releaseProjectFilesNode);
    }

    foreach (var projectTestOutput in projectTestOutputs)
    {
      var testProjectFilesNode = xmlDocument.CreateElement("UnitTestProjectFiles");
      testProjectFilesNode.SetAttribute("Include", $@"$(SolutionDirectory)\{projectTestOutput.Name}\{projectTestOutput.Name}.csproj");
      var testConfigurationNode = xmlDocument.CreateElement("TestConfiguration");
      testConfigurationNode.InnerText = "$(NormalTestConfiguration)";
      testProjectFilesNode.AppendChild(testConfigurationNode);
      itemGroupNode.AppendChild(testProjectFilesNode);
    }

    xmlDocument.Save($"{_customizationPath}/Projects.props");
  }

  public void SetBuildConfigurationProps (IDictionary<string, string> properties)
  {
    var xmlDocument = new XmlDocument();
    xmlDocument.Load($"{_customizationPath}/BuildConfiguration.props");
    var itemGroupNode = xmlDocument.GetElementsByTagName("ItemGroup")[0];
    itemGroupNode!.RemoveAll();
    foreach (var keyValuePair in properties)
    {
      var buildConfigurationNode = xmlDocument.CreateElement(keyValuePair.Key);
      buildConfigurationNode.SetAttribute("Include", keyValuePair.Value);
      itemGroupNode.AppendChild(buildConfigurationNode);
    }

    xmlDocument.Save($"{_customizationPath}/BuildConfiguration.props");
  }

  public IReadOnlyCollection<ProjectOutput> CreateReleaseProjectOutputs (ProjectOutputConfiguration projectOutputConfiguration)
  {
    return new[]
           {
               new ProjectOutput(
                   "SdkTestProject",
                   _testSolutionPath,
                   new[]
                   {
                       @$"{_testSolutionPath}/SdkTestProject/bin/Debug/netstandard2.1/",
                       @$"{_testSolutionPath}/SdkTestProject/bin/Release/netstandard2.1/"
                   },
                   projectOutputConfiguration
               ),
               new ProjectOutput(
                   "NonSdkTestProject",
                   _testSolutionPath,
                   new[]
                   {
                       @$"{_testSolutionPath}/NonSdkTestProject/bin/Debug/",
                       @$"{_testSolutionPath}/NonSdkTestProject/bin/Release/"
                   },
                   projectOutputConfiguration,
                   false),
               new ProjectOutput(
                   "MultiTargetFrameworksTestProject",
                   _testSolutionPath,
                   new[]
                   {
                       @$"{_testSolutionPath}/MultiTargetFrameworksTestProject/bin/Debug/netstandard2.1/",
                       @$"{_testSolutionPath}/MultiTargetFrameworksTestProject/bin/Debug/net45/",
                       @$"{_testSolutionPath}/MultiTargetFrameworksTestProject/bin/Release/netstandard2.1/",
                       @$"{_testSolutionPath}/MultiTargetFrameworksTestProject/bin/Release/net45/"
                   },
                   projectOutputConfiguration),
               new ProjectOutput(
                   "DocumentationTestProject",
                   _testSolutionPath,
                   new[]
                   {
                       @$"{_testSolutionPath}/DocumentationTestProject/bin/Debug/net45/",
                       @$"{_testSolutionPath}/DocumentationTestProject/bin/Release/net45/"
                   },
                   projectOutputConfiguration
               ),
           };
  }

  public IReadOnlyCollection<TestProjectOutput> CreateTestProjectOutputs (ProjectOutputConfiguration projectOutputConfiguration)
  {
    return new[]
           {
               new TestProjectOutput(
                   "UnitTestNet48Project",
                   _testSolutionPath,
                   new[]
                   {
                       @$"{_testSolutionPath}/UnitTestNet48Project/bin/Debug/net48/",
                       @$"{_testSolutionPath}/UnitTestNet48Project/bin/Release/net48/"
                   },
                   projectOutputConfiguration),
               new TestProjectOutput(
                   "UnitTestNet45Project",
                   _testSolutionPath,
                   new[]
                   {
                       @$"{_testSolutionPath}/UnitTestNet45Project/bin/Debug/net45/",
                       @$"{_testSolutionPath}/UnitTestNet45Project/bin/Release/net45/"
                   },
                   projectOutputConfiguration)
           };
  }

  private void SetPropertiesInnerText (string tagName, string value, XmlDocument xmlDocument)
  {
    var node = xmlDocument.GetElementsByTagName(tagName)[0];
    node!.InnerText = value;
  }
}
