using System;
using ReleaseProcessAutomation.Jira.Utility;

namespace ReleaseProcessAutomation.Jira;

public class Jira
    : IJira
{

  public IJiraVersionCreator VersionCreator { get; }

  public IJiraVersionReleaser VersionReleaser { get; }

  public Jira (IJiraVersionCreator versionCreator, IJiraVersionReleaser versionReleaser)
  {
    VersionCreator = versionCreator;
    VersionReleaser = versionReleaser;
  }
}