using System.Collections.Immutable;
using System.Linq;
using Remotion.BuildScript;
using Remotion.BuildScript.Test;
using Remotion.BuildScript.Test.Dimensions;
using static Remotion.BuildScript.Test.Dimensions.Configurations;
using static Remotion.BuildScript.Test.Dimensions.ExecutionRuntimes;
using static Remotion.BuildScript.Test.Dimensions.Platforms;
using static Remotion.BuildScript.Test.Dimensions.TargetFrameworks;

public class Build : RemotionBuild
{
  public static int Main() => Execute<Build>();

  public override void ConfigureProjects(ProjectsBuilder projects)
  {
    var normalTestConfiguration = new TestConfiguration(
      DefaultTestExecutionRuntimeFactory.Instance,
      TestMatrices.Single(),
      ImmutableArray<ITestExecutionWrapper>.Empty);

    projects.AddReleaseProject("SdkTestProject");
    projects.AddReleaseProject("NonSdkTestProject");
    projects.AddReleaseProject("MultiTargetFrameworksTestProject");
    projects.AddUnitTestProject("UnitTestNet48Project", normalTestConfiguration);
  }

  public override void ConfigureSupportedTestDimensions(SupportedTestDimensionsBuilder supportedTestDimensions)
  {
    supportedTestDimensions.AddSupportedDimension<ExecutionRuntimes>(LocalMachine);
    supportedTestDimensions.AddSupportedDimension<TargetFrameworks>(NET48, NET8_0);
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
        { LocalMachine, NET8_0, Debug, x86 },
        { LocalMachine, NET8_0, Release, x64 }
      });
  }
}
