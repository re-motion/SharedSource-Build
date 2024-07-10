using System;
using System.Collections.Immutable;
using System.Linq;
using Nuke.Common.Tools.GitVersion;
using Remotion.BuildScript;
using Remotion.BuildScript.Components;
using Remotion.BuildScript.Test;
using Remotion.BuildScript.Test.Dimensions;
using static Remotion.BuildScript.Test.Dimensions.Configurations;
using static Remotion.BuildScript.Test.Dimensions.ExecutionRuntimes;
using static Remotion.BuildScript.Test.Dimensions.Platforms;
using static Remotion.BuildScript.Test.Dimensions.TargetFrameworks;

// ReSharper disable RedundantTypeArgumentsOfMethod

class Build : RemotionBuild, IBuildMetadata
{
  [GitVersion(UpdateBuildNumber = false)]
  GitVersion GitVersion;

  public static int Main () => Execute<Build>();

  string IBuildMetadata.GetBaseVersion ()
  {
      return GitVersion.MajorMinorPatch;
  }

  public override void ConfigureProjects (ProjectsBuilder projects)
  {
    var normalTestConfiguration = new TestConfiguration(
        DefaultTestExecutionRuntimeFactory.Instance,
        TestMatrices.Single(e => e.Name == "NormalTestMatrix"),
        ImmutableArray<ITestExecutionWrapper>.Empty);
    var localOnlyTestConfiguration = new TestConfiguration(
        DefaultTestExecutionRuntimeFactory.Instance,
        TestMatrices.Single(e => e.Name == "LocalOnlyTestMatrix"),
        ImmutableArray<ITestExecutionWrapper>.Empty);

    projects.AddReleaseProject("BuildScript.Nuke");
    projects.AddUnitTestProject("BuildScript.Nuke.IntegrationTests", localOnlyTestConfiguration);
    projects.AddUnitTestProject("BuildScript.Nuke.UnitTests", normalTestConfiguration);
  }

  public override void ConfigureSupportedTestDimensions (SupportedTestDimensionsBuilder supportedTestDimensions)
  {
    supportedTestDimensions.AddSupportedDimension<ExecutionRuntimes>(
        LocalMachine,
        EnforcedLocalMachine,
        Docker_Win_NET8_0);
    supportedTestDimensions.AddSupportedDimension<TargetFrameworks>(NET8_0);
    supportedTestDimensions.AddSupportedDimension<Configurations>(Debug, Release);
    supportedTestDimensions.AddSupportedDimension<Platforms>(x64);
  }

  public override void ConfigureTestMatrix (TestMatricesBuilder builder)
  {
    builder.AddTestMatrix(
        "NormalTestMatrix",
        new TestDimension[,]
        {
            { Docker_Win_NET8_0, NET8_0, Debug, x64 },
            { Docker_Win_NET8_0, NET8_0, Release, x64 },

            // Local
            { LocalMachine, NET8_0, Debug, x64 },
            { LocalMachine, NET8_0, Release, x64 },
        },
        allowEmpty: true);

    builder.AddTestMatrix(
        "LocalOnlyTestMatrix",
        new TestDimension[,]
        {
            // TODO RMSRCBUILD-308: Fix integration tests on TeamCity
            // { EnforcedLocalMachine, NET8_0, Debug, x64 },
            // { EnforcedLocalMachine, NET8_0, Release, x64 },

            // Local
            { LocalMachine, NET8_0, Debug, x64 },
            { LocalMachine, NET8_0, Release, x64 },
        },
        allowEmpty: true);
  }
}