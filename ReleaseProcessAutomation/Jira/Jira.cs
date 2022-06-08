using System;
using ReleaseProcessAutomation.Jira.Utility;

namespace ReleaseProcessAutomation.Jira;

public class Jira
    : IJira
{
  private readonly IJiraRestClientProvider _jiraRestClientProvider;

  public IJiraVersionCreator VersionCreator { get; }

  public IJiraVersionReleaser VersionReleaser { get; }

  public Jira (IJiraRestClientProvider jiraRestClientProvider, IJiraVersionCreator versionCreator, IJiraVersionReleaser versionReleaser)
  {
    _jiraRestClientProvider = jiraRestClientProvider;
    VersionCreator = versionCreator;
    VersionReleaser = versionReleaser;
  }
}