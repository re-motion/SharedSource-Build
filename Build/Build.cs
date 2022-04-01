using System;
using Nuke.Common.Execution;
using Remotion.BuildScript;
using Remotion.BuildScript.Components;

[CheckBuildProjectConfigurations]
class Build : BaseBuild
{
  public static int Main () => Execute<Build>(x => ((ICompile)x).CompileReleaseBuild);
}