using ReleaseProcessAutomation.Jira.ServiceFacadeImplementations;

namespace ReleaseProcessAutomation.Jira;

public interface IJiraVersionReleaser
{
  void ReleaseVersion (string jiraUrl, string versionID, string nextVersionID, bool sortReleasedVersion);
  void ReleaseVersionAndSquashUnreleased (string jiraUrl, string jiraProjectKey, string versionID, string nextVersionID);
}