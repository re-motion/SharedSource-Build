using System.Collections.Immutable;
using System.Linq;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Remotion.BuildScript;
using Remotion.BuildScript.Components;
using Remotion.BuildScript.GenerateSbom;
using Remotion.BuildScript.Test;
using Remotion.BuildScript.Test.Dimensions;
using static Remotion.BuildScript.Test.Dimensions.Configurations;
using static Remotion.BuildScript.Test.Dimensions.ExecutionRuntimes;
using static Remotion.BuildScript.Test.Dimensions.Platforms;
using static Remotion.BuildScript.Test.Dimensions.TargetFrameworks;

public class Build : RemotionBuild
{
  public static int Main() => Execute<Build>();

  [Parameter("Enables SBOM generation and writes the SBOM to the specified file (.xml or .json).")]
  readonly AbsolutePath SbomOutputFile;

  [Parameter("Includes the npm dependencies of the specified package.json file in the generated SBOM.")]
  readonly AbsolutePath SbomPackageJsonFile;

  public override ISbomGeneratorBuilder ConfigureSbomGenerationInfoBuilder(Solution solution)
  {
    if (SbomOutputFile == null)
      return base.ConfigureSbomGenerationInfoBuilder(solution);

    var builder = new SolutionSbomGeneratorBuilder(
        solution,
        "1.2.3",
        ((IBaseBuild)this).TempFolder / "Sbom",
        SbomOutputFile,
        githubUsername: "",
        githubAccessToken: "")
        // The SBOM cleaner requires a .deps.json file, which is not generated for .NET Framework projects
        .WithProjectsBlackListed("DocumentationTestProject", "UnitTestNet48Project");

    if (SbomPackageJsonFile != null)
      builder.WithPackageJsonFile(SbomPackageJsonFile);

    return builder;
  }

  public override void ConfigureProjects(ProjectsBuilder projects)
  {
    var normalTestConfiguration = new TestConfiguration(
      DefaultTestExecutionRuntimeFactory.Instance,
      TestMatrices.Single(),
      ImmutableArray<ITestExecutionWrapper>.Empty);

    projects.AddReleaseProject("SdkTestProject");
    projects.AddReleaseProject("DocumentationTestProject");
    projects.AddReleaseProject("MultiTargetFrameworksTestProject");
    projects.AddUnitTestProject("UnitTestNet48Project", normalTestConfiguration);
  }

  public override void ConfigureSupportedTestDimensions(SupportedTestDimensionsBuilder supportedTestDimensions)
  {
    supportedTestDimensions.AddSupportedDimension<ExecutionRuntimes>(LocalMachine);
    supportedTestDimensions.AddSupportedDimension<TargetFrameworks>(NET48, NET10_0);
    supportedTestDimensions.AddSupportedDimension<Configurations>(Debug, Release);
    supportedTestDimensions.AddSupportedDimension<Platforms>(x86, x64);
  }

  public override void ConfigureTestMatrix(TestMatricesBuilder builder)
  {
    builder.AddTestMatrix(
      "NormalTestMatrix",
      new TestDimension[,]
      {
        { LocalMachine, NET48, Debug, x86 },
        { LocalMachine, NET48, Release, x64 },
        { LocalMachine, NET10_0, Debug, x86 },
        { LocalMachine, NET10_0, Release, x64 }
      });
  }
}
