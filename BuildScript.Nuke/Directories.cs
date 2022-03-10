using System;
using Nuke.Common.IO;

public class Directories
{
  /// <summary>
  ///   Path to the project folder
  /// </summary>
  public AbsolutePath Solution { get; }

  /// <summary>
  ///   <see cref="Solution" />/BuildOutput/
  ///   Subfolder of the solution directory named "BuildOutput"
  ///   Is handed over to MSBuild during the compile step <see cref="BaseBuild.CompileReleaseBuild" />
  /// </summary>
  public AbsolutePath Output { get; }

  /// <summary>
  ///   <see cref="Output" />/temp/
  ///   Temporary subfolder of the solution directory
  ///   Is handed over to MSBuild during the compile step <see cref="Build.CompileReleaseBuild" />
  /// </summary>
  public AbsolutePath Temp { get; }

  /// <summary>
  ///   <see cref="Output" />/log/
  ///   Is handed over to MSBuild during the compile step <see cref="Build.CompileReleaseBuild" />
  /// </summary>
  public AbsolutePath Log { get; }

  /// <summary>
  ///   <see cref="Solution" />/remotion.snk
  ///   Is handed over to MSBuild during the compile step <see cref="BaseBuild.CompileReleaseBuild" />
  /// </summary>
  public AbsolutePath SolutionKeyFile { get; }

  /// <summary>
  ///   <see cref="Solution" />/BuildScript.Nuke/Customizations
  ///   Contains configuration files for the build
  ///   Which are loaded in the <see cref="Build.ImportPropertiesDefinition" />
  /// </summary>
  public AbsolutePath CustomizationPath { get; }

  public Directories (AbsolutePath root, AbsolutePath buildProjectDirectory)
  {
    Solution = root;
    Output = Solution / "BuildOutput/";
    Temp = Output / "temp/";
    Log = Output / "log/";
    SolutionKeyFile = Solution / "remotion.snk";
    CustomizationPath = buildProjectDirectory / "Customizations";
  }
}