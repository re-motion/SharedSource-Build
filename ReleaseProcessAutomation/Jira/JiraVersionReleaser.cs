using System;
using System.IO;
using JetBrains.Annotations;
using ReleaseProcessAutomation.Jira.ServiceFacadeImplementations;
using ReleaseProcessAutomation.Jira.ServiceFacadeInterfaces;

namespace ReleaseProcessAutomation.Jira;

public class JiraVersionReleaser : JiraTask
{
  public JiraVersionReleaser ([CanBeNull] string? jiraUsername, [CanBeNull] string? jiraPassword)
      : base(jiraUsername, jiraPassword) { }

  public void ReleaseVersion (string jiraUrl, string versionID, string nextVersionID, bool sortReleasedVersion)
  {
    var restClient = new JiraRestClient (jiraUrl, Authenticator);
    IJiraProjectVersionService service = new JiraProjectVersionService (restClient);
    var jiraProjectVersionFinder = new JiraProjectVersionFinder (restClient);
    var jiraProjectVersionRepairer = new JiraProjectVersionRepairer (service, jiraProjectVersionFinder);

    service.ReleaseVersion (versionID, nextVersionID);

    if (sortReleasedVersion)
    {
      jiraProjectVersionRepairer.RepairVersionPosition (versionID);
    }
  }
  
  public void ReleaseVersionAndSquashUnreleased (string jiraUrl, string jiraProjectKey, string versionID, string nextVersionID)
  {
    var restClient = new JiraRestClient(jiraUrl, Authenticator);
    IJiraProjectVersionService service = new JiraProjectVersionService(restClient);
    service.ReleaseVersionAndSquashUnreleased(versionID, nextVersionID, jiraProjectKey);
  }
  
  
  
  


}