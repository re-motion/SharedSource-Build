namespace ReleaseProcessAutomation.Jira;

public interface IJira
{
  IJiraVersionCreator VersionCreator { get; }

  IJiraVersionReleaser VersionReleaser { get; }
}