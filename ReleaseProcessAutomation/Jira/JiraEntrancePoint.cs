using System;
using System.Net;
using System.Runtime.CompilerServices;
using System.Security;
using System.Security.Cryptography;
using System.Xml;
using AdysTech.CredentialManager;
using ReleaseProcessAutomation.Configuration.Data;
using ReleaseProcessAutomation.ReadInput;
using ReleaseProcessAutomation.SemanticVersioning;
using Remotion.ReleaseProcessScript.Jira.ServiceFacadeImplementations;
using Serilog;
using Serilog.Events;
using Spectre.Console;

namespace ReleaseProcessAutomation.Jira;

public interface IJiraEntrancePoint
{
  void CreateAndReleaseJiraVersion (SemanticVersion currentVersion, SemanticVersion nextVersion, bool squashUnreleased = false);

  void CheckJiraCredentials (Credentials credentials);
}

public class JiraEntrancePoint : IJiraEntrancePoint
{
  private readonly Config _config;
  private readonly IAnsiConsole _console;
  private readonly string _jiraUrlPostfix;
  private readonly ILogger _log = Log.ForContext<JiraEntrancePoint>();
  private readonly IJiraCredentialManager _jiraCredentialManager;

  public Jira (Config config, IAnsiConsole console, IJiraCredentialManager jiraCredentialManager, string jiraUrlPostfix)
  {
    _jiraCredentialManager = jiraCredentialManager;
    _config = config;
    _console = console;
    _jiraUrlPostfix = jiraUrlPostfix;
  }
  
  public void CreateAndReleaseJiraVersion (SemanticVersion currentVersion, SemanticVersion nextVersion, bool squashUnreleased = false)
  {
    var currentVersionID = CreateVersion(currentVersion);
    var nextVersionID = CreateVersion(nextVersion);

    var releaseMessage = $"Releasing version '{currentVersion}' on JIRA. ";
    _log.Information(releaseMessage);
    _console.WriteLine(releaseMessage);
    
    var moveMessage = $"Moving open issues to '{nextVersion}'.";
    _log.Information(moveMessage);
    _console.WriteLine(moveMessage);

    ReleaseVersion(currentVersionID, nextVersionID, squashUnreleased);
  }

  private string CreateVersion (SemanticVersion version)
  {
    var jiraCreateVersion = new JiraCreateNewVersionWithVersionNumber
                            {
                                JiraUrl = JiraUrlWithPostfix(),
                                JiraProject = _config.Jira.JiraProjectKey,
                                VersionNumber = version.ToString()
                            };
    
    AddAuthentication(jiraCreateVersion);
    
    jiraCreateVersion.Execute();

    return jiraCreateVersion.CreatedVersionID ?? throw new InvalidOperationException("The created version did not have a version id assigned.");
  }

  public string JiraUrlWithPostfix ()
  {
    var url = _config.Jira.JiraURL;
    return url.EndsWith("/") ? $"{url}{_jiraUrlPostfix}" : $"{url}/{_jiraUrlPostfix}";
  }

  private void AddAuthentication (JiraTask task)
  {
    if (_config.Jira.UseNTLM)
      return;
    
    var credential = _jiraCredentialManager.GetCredential(_config.Jira.JiraURL);
    task.JiraUsername = credential.Username;
    task.JiraPassword = credential.Password;
  }

  private void ReleaseVersion (string currentVersionID, string nextVersionID, bool squashUnreleased)
  {
    if (squashUnreleased)
    {
      var releaseVersion = new JiraReleaseVersionAndSquashUnreleased
                           {
                               JiraUrl = JiraUrlWithPostfix(),
                               VersionID = currentVersionID,
                               NextVersionID = nextVersionID,
                               ProjectKey = _config.Jira.JiraProjectKey
                           };
      AddAuthentication(releaseVersion);
      releaseVersion.Execute();
    }
    else
    {
      var releaseVersion = new JiraReleaseVersion
                           {
                               JiraUrl = JiraUrlWithPostfix(),
                               VersionID = currentVersionID,
                               NextVersionID = nextVersionID
                           };
      AddAuthentication(releaseVersion);
      releaseVersion.Execute();
    }
  }
  public void CheckJiraCredentials (Credentials credentials)
  {
    var jiraCheckAuthentication = new JiraCheckAuthentication
                                  { 
                                      JiraUrl = JiraUrlWithPostfix(),
                                      JiraUsername = credentials.Username,
                                      JiraPassword = credentials.Password,
                                      JiraProject = _config.Jira.JiraProjectKey
                                  };

    try
    {
      jiraCheckAuthentication.Execute();
    }
    catch (Exception e)
    {
      var errorMessage =
          $"Jira Check Authentication has failed. Maybe wrong credentials? \nAlso be advised that the ProjectKey is case sensitive '{_config.Jira.JiraProjectKey}'\nJira Url: '{_config.Jira.JiraURL}'. \nException Message: '{e.Message}'";
      _log.Warning(errorMessage);
      _console.WriteLine(errorMessage);
      throw;
    }
  }
}