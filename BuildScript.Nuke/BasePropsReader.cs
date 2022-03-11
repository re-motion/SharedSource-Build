using System;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Project = Microsoft.Build.Evaluation.Project;

public class BasePropsReader
{
  private const string c_solutionDirectoryProperty = "SolutionDirectory";

  protected readonly AbsolutePath _customizationDirectoryPath;
  private readonly AbsolutePath _solutionDirectoryPath;

  protected BasePropsReader (
      AbsolutePath solutionDirectoryPath,
      AbsolutePath customizationDirectoryPath)
  {
    _solutionDirectoryPath = solutionDirectoryPath;
    _customizationDirectoryPath = customizationDirectoryPath;
  }

  protected Project LoadProjectWithSolutionDirectoryPropertySet (string configFileName)
  {
    var project = ProjectModelTasks.ParseProject(_customizationDirectoryPath / configFileName);
    project.SetGlobalProperty(c_solutionDirectoryProperty, _solutionDirectoryPath);
    project.ReevaluateIfNecessary();
    return project;
  }
}