namespace ReleaseProcessAutomation.Jira;

public interface IJiraVersionReleaser
{
  void ReleaseVersion (string versionID, string nextVersionID, bool sortReleasedVersion);
  void ReleaseVersionAndSquashUnreleased (string jiraProjectKey, string versionID, string nextVersionID);
}