using System;
using Nuke.Common.IO;

#pragma warning disable CS8618

internal class ProjectMetadata
{
  public AbsolutePath Path { get; init; }
  public string ToolsVersion { get; init; }
  public bool IsMultiTargetFramework { get; init; }
}

#pragma warning restore CS8618