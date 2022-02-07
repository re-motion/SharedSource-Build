using System;
using Nuke.Common.IO;

public class DirectoryHelper
{
  /// <summary>
  ///   Path to the project folder
  /// </summary>
  public AbsolutePath SolutionDirectory { get; }

  /// <summary>
  ///   <see cref="SolutionDirectory" />/BuildOutput/
  ///   Subfolder of the solution directory named "BuildOutput"
  ///   Is handed over to MSBuild during the compile step <see cref="Build.CompileReleaseBuild" />
  /// </summary>
  public AbsolutePath OutputDirectory { get; }

  /// <summary>
  ///   <see cref="OutputDirectory" />/temp/
  ///   Temporary subfolder of the solution directory
  ///   Is handed over to MSBuild during the compile step <see cref="Build.CompileReleaseBuild" />
  /// </summary>
  public AbsolutePath TempDirectory { get; }

  /// <summary>
  ///   <see cref="OutputDirectory" />/log/
  ///   Is handed over to MSBuild during the compile step <see cref="Build.CompileReleaseBuild" />
  /// </summary>
  public AbsolutePath LogDirectory { get; }

  /// <summary>
  ///   <see cref="SolutionDirectory" />/remotion.snk
  ///   Is handed over to MSBuild during the compile step <see cref="Build.CompileReleaseBuild" />
  /// </summary>
  public AbsolutePath SolutionKeyFile { get; }

  /// <summary>
  ///   <see cref="SolutionDirectory" />/BuildScript.Nuke/Customizations
  ///   Contains configuration files for the build
  ///   Which are loaded in the <see cref="Build.ImportPropertiesDefinition" />
  /// </summary>
  public AbsolutePath CustomizationPath { get; }

  public DirectoryHelper (AbsolutePath rootDirectory)
  {
    SolutionDirectory = rootDirectory;
    OutputDirectory = SolutionDirectory / "BuildOutput/";
    TempDirectory = OutputDirectory / "temp/";
    LogDirectory = OutputDirectory / "log/";
    SolutionKeyFile = SolutionDirectory / "remotion.snk";
    CustomizationPath = SolutionDirectory / "BuildScript.Nuke" / "Customizations";
  }
}