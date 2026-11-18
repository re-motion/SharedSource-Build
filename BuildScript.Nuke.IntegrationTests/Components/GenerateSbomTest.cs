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

using System.IO;
using System.Linq;
using System.Xml.Linq;
using Nuke.Common.IO;
using Nuke.Common.Utilities;
using NUnit.Framework;

namespace BuildScript.Nuke.IntegrationTests.Components;

[TestFixture]
public class GenerateSbomTest : IntegrationTestBase
{
  // GenerateSbom only orders itself after CreateTestMatrix (partial build support), but the test solution's ConfigureProjects
  // requires the test matrix, so it has to be created explicitly, which in turn requires the enabled test dimensions.
  private const string GenerateSbomTargets =
      "CreateTestMatrix GenerateSbom"
      + " --TestConfigurations Debug+Release"
      + " --TestExecutionRuntimes LocalMachine"
      + " --TestPlatforms x86+x64"
      + " --TestTargetRuntimes NET48+NET8_0";

  [Test]
  public void GenerateSbom_WithoutConfiguration_DoesNotRun ()
  {
    var result = RunBuildCmd(GenerateSbomTargets);

    var sectionOutput = result.GetSectionOutput("GenerateSbom");
    Assert.That(sectionOutput, Does.Contain("Sbom generation has not been configured and will therefore not run."));
  }

  [Test]
  public void GenerateSbom_WithXmlOutputFile_CreatesSbom ()
  {
    var sbomFile = TestSolutionDirectory / "MySbom" / "sbom.xml";
    Assert.That(sbomFile.FileExists(), Is.False);

    RunBuildCmd($"{GenerateSbomTargets} --SbomOutputFile {sbomFile.ToString().DoubleQuote()}");

    Assert.That(sbomFile.FileExists(), Is.True);

    var document = XDocument.Load(sbomFile);
    var root = document.Root!;
    Assert.That(root.Name.LocalName, Is.EqualTo("bom"));
    Assert.That(root.Name.NamespaceName, Does.StartWith("http://cyclonedx.org/schema/bom/"));

    var metadataComponent = root.Element(root.Name.Namespace + "metadata")?.Element(root.Name.Namespace + "component");
    Assert.That(metadataComponent, Is.Not.Null);
    Assert.That(metadataComponent!.Element(root.Name.Namespace + "name")?.Value, Is.EqualTo("TestSolution"));
    Assert.That(metadataComponent.Element(root.Name.Namespace + "version")?.Value, Is.EqualTo("1.2.3"));
  }

  [Test]
  public void GenerateSbom_WithJsonOutputFile_CreatesSbom ()
  {
    var sbomFile = TestSolutionDirectory / "MySbom" / "sbom.json";
    Assert.That(sbomFile.FileExists(), Is.False);

    RunBuildCmd($"{GenerateSbomTargets} --SbomOutputFile {sbomFile.ToString().DoubleQuote()}");

    Assert.That(sbomFile.FileExists(), Is.True);

    using var document = System.Text.Json.JsonDocument.Parse(File.ReadAllText(sbomFile));
    var root = document.RootElement;
    Assert.That(root.GetProperty("bomFormat").GetString(), Is.EqualTo("CycloneDX"));

    var metadataComponent = root.GetProperty("metadata").GetProperty("component");
    Assert.That(metadataComponent.GetProperty("name").GetString(), Is.EqualTo("TestSolution"));
    Assert.That(metadataComponent.GetProperty("version").GetString(), Is.EqualTo("1.2.3"));
  }

  [Test]
  public void GenerateSbom_WithPackageJsonFile_CreatesCombinedSbom ()
  {
    var sbomFile = TestSolutionDirectory / "MySbom" / "sbom.xml";
    var packageJsonFile = TestSolutionDirectory / "JavaScriptDependencies" / "package.json";
    Assert.That(sbomFile.FileExists(), Is.False);

    RunBuildCmd(
        $"{GenerateSbomTargets}"
        + $" --SbomOutputFile {sbomFile.ToString().DoubleQuote()}"
        + $" --SbomPackageJsonFile {packageJsonFile.ToString().DoubleQuote()}");

    Assert.That(sbomFile.FileExists(), Is.True);

    var document = XDocument.Load(sbomFile);
    var ns = document.Root!.Name.Namespace;

    var metadataComponent = document.Root.Element(ns + "metadata")?.Element(ns + "component");
    Assert.That(metadataComponent, Is.Not.Null);
    Assert.That(metadataComponent!.Element(ns + "name")?.Value, Is.EqualTo("TestSolution-root-component"));
    Assert.That(metadataComponent.Element(ns + "version")?.Value, Is.EqualTo("1.2.3"));

    var componentNames = document.Root.Element(ns + "components")?.Elements(ns + "component").Select(e => e.Element(ns + "name")?.Value).ToArray();
    Assert.That(componentNames, Does.Contain("is-number"));
  }
}
