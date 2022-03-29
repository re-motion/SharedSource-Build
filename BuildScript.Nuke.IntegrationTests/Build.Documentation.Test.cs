using System;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace BuildScript.Nuke.IntegrationTests;

public class BuildDocumentationTest : BuildBaseTest
{
  [Test]
  public void GenerateDocumentation_WithAllReleaseProjects_BuildsAllDocumentationProject ()
  {
    var releaseProjectOutputs = BuildConfigurationSetup.CreateReleaseProjectOutputs(ProjectOutputConfiguration.Debug);
    var documentationProject = releaseProjectOutputs.First();
    documentationProject.IsDocumentationProject = true;
    BuildConfigurationSetup.SetProjectsProps(releaseProjectOutputs, Array.Empty<ProjectOutput>());
    DeleteCleanDirectories(releaseProjectOutputs);
    var arguments = @" ";

    var exitCode = RunTarget("GenerateDocumentation", arguments);

    Assert.That(exitCode, Is.EqualTo(0));
    TestDocumentationExists(documentationProject);
  }

  [Test]
  public void GenerateDocumentation_SkipDocumentation ()
  {
    var releaseProjectOutputs = BuildConfigurationSetup.CreateReleaseProjectOutputs(ProjectOutputConfiguration.Debug);
    var documentationProject = releaseProjectOutputs.First();
    documentationProject.IsDocumentationProject = true;
    BuildConfigurationSetup.SetProjectsProps(releaseProjectOutputs, Array.Empty<ProjectOutput>());
    DeleteCleanDirectories(releaseProjectOutputs);
    var arguments = @" --skip-documentation ";

    var exitCode = RunTarget("GenerateDocumentation", arguments);

    Assert.That(exitCode, Is.EqualTo(0));
    TestDocumentationDoesNotExist();
  }

  private void TestDocumentationExists (ProjectOutput projectDocumentation)
  {
    var documentationOutputFilePath = $"{TestBuildOutputPath}/temp/doc/Output/";
    Assert.That(documentationOutputFilePath, Does.Exist);
    Assert.That(Directory.GetFiles(documentationOutputFilePath), Is.Not.Empty);
    var documentOutputFile = new FileInfo(Directory.GetFiles(documentationOutputFilePath)[0]);
    Assert.That(documentOutputFile.Length, Is.GreaterThan(0));
    var outputFiles = Directory.GetFiles($"{TestBuildOutputPath}/NuGetWithDebugSymbols/Debug/");
    Assert.That(outputFiles[0], Does.Contain(projectDocumentation.Name).And.Contain(".nupkg"));
  }

  private void TestDocumentationDoesNotExist ()
  {
    var documentationOutputFilePath = $"{TestBuildOutputPath}/temp/doc/Output/";
    Assert.That(documentationOutputFilePath, Does.Not.Exist);
    Assert.That($"{TestBuildOutputPath}/NuGetWithDebugSymbols/Debug/", Does.Not.Exist);
  }
}
