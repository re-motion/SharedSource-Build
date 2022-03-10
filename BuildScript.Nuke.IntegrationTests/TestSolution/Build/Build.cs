using Nuke.Common.Execution;
using Remotion.BuildScript;

[CheckBuildProjectConfigurations]
class Build : BaseBuild
{
  public static int Main () => Execute<Build>(x => x.RunTests);
}
