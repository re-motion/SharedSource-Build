using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using JetBrains.Annotations;
using ReleaseProcessAutomation.Jira.ServiceFacadeImplementations;
using ReleaseProcessAutomation.Jira.ServiceFacadeInterfaces;
using RestSharp.Authenticators;

namespace ReleaseProcessAutomation.Jira;

public class JiraVersionCreator : JiraTask
{

  public JiraVersionCreator ([CanBeNull] string? jiraUsername, [CanBeNull] string? jiraPassword) : 
      base(jiraUsername, jiraPassword) { }

  public void CreateNewVersion (string jiraUrl, string jiraProject, string versionPattern, bool sortVersion, int versionComponentToIncrement, DayOfWeek versionReleaseWeekday)
  {
    if (string.IsNullOrEmpty(jiraUrl))
    {
      throw new InvalidOperationException("Jira url was not assigned.");
    }

    if (string.IsNullOrEmpty(jiraProject))
    {
      throw new InvalidOperationException("Jira project was not assigned.");
    }
      
    if (string.IsNullOrEmpty(versionPattern))
    {
      throw new InvalidOperationException("Version pattern was not assigned.");
    }
      
    JiraRestClient restClient = new JiraRestClient (jiraUrl, Authenticator);
    IJiraProjectVersionService service = new JiraProjectVersionService (restClient);
    IJiraProjectVersionFinder finder = new JiraProjectVersionFinder (restClient);
    var jiraProjectVersionRepairer = new JiraProjectVersionRepairer (service, finder);

    var createdVersionId = service.CreateSubsequentVersion (jiraProject, versionPattern, versionComponentToIncrement, versionReleaseWeekday);

    if (sortVersion)
      jiraProjectVersionRepairer.RepairVersionPosition (createdVersionId);
  }
  
  public string CreateNewVersionWithVersionNumber (string jiraUrl, string jiraProject, string versionNumber )
  {
    if (string.IsNullOrEmpty(jiraUrl))
    {
      throw new InvalidOperationException("Jira url was not assigned.");
    }
    if (string.IsNullOrEmpty(jiraProject))
    {
      throw new InvalidOperationException("Jira project was not assigned.");
    }
      
    JiraRestClient restClient = new JiraRestClient (jiraUrl, Authenticator);
    IJiraProjectVersionService service = new JiraProjectVersionService (restClient);
    IJiraProjectVersionFinder finder = new JiraProjectVersionFinder (restClient);
    var jiraProjectVersionRepairer = new JiraProjectVersionRepairer (service, finder);

    var versions = finder.FindVersions (jiraProject, "(?s).*").ToList();
    var jiraProjectVersion = versions.Where (x => x.name == versionNumber).DefaultIfEmpty().First();

    string createdVersionID;
    
    if (jiraProjectVersion != null)
    {
      if (jiraProjectVersion.released != null)
      {
        if (jiraProjectVersion.released.Value)
          throw new JiraException ("The Version '" + versionNumber + "' got already released in Jira.");
      }

      if (string.IsNullOrEmpty(jiraProjectVersion.id))
      {
        throw new InvalidOperationException("Jira project id was not assigned.");
      }
      createdVersionID = jiraProjectVersion.id;
    }
    else
    {
      if (string.IsNullOrEmpty(versionNumber))
      {
        throw new InvalidOperationException("Version number was not assigned.");
      }
      createdVersionID = service.CreateVersion (jiraProject, versionNumber, null);

      jiraProjectVersionRepairer.RepairVersionPosition (createdVersionID);
    }

    return createdVersionID;
  }
  
}