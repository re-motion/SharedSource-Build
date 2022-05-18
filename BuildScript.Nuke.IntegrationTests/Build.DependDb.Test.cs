using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace BuildScript.Nuke.IntegrationTests;

public class BuildDependDbTest : BuildBaseTest
{
  [Test]
  public void DependDb_WithAllReleaseProjectsWithDebugRelease_OutputsDependDb ()
  {
    var releaseProjectOutputs = BuildConfigurationSetup.CreateReleaseProjectOutputs(ProjectOutputConfiguration.ReleaseDebug).Take(1).ToList();
    BuildConfigurationSetup.SetProjectsProps(releaseProjectOutputs, Array.Empty<ProjectOutput>());
    DeleteCleanDirectories(releaseProjectOutputs);
    var arguments = @" ";

    var exitCode = RunTarget("DependDb", arguments);

    Assert.That(exitCode, Is.EqualTo(0));
    TestCorrectDependDbOutputPaths(releaseProjectOutputs);
  }

  private void TestCorrectDependDbOutputPaths (IReadOnlyCollection<ProjectOutput> outputs)
  {
    var dependDbBuildProcessorConfig = $"{TestBuildOutputPath}/temp/DependDb.BuildProcessor.config.xml";
    Assert.That(dependDbBuildProcessorConfig, Does.Exist);
    var dependDbOutputPath = $"{TestBuildOutputPath}/DependDb/";
    Assert.That(Directory.GetFiles(dependDbOutputPath), Is.Not.Empty);

    var dependDbOutputFile = new FileInfo(Directory.GetFiles(dependDbOutputPath)[0]);
    Assert.That(dependDbOutputFile.Length, Is.GreaterThan(0));
  }
}
