using System;
using ReleaseProcessAutomation.Configuration.Data;
using ReleaseProcessAutomation.SemanticVersioning;
using Serilog;
using Spectre.Console;

namespace ReleaseProcessAutomation.Jira;

public interface IJiraEntrancePoint
{
  void CreateAndReleaseJiraVersion (SemanticVersion currentVersion, SemanticVersion nextVersion, bool squashUnreleased = false);
}

public class JiraEntrancePoint : IJiraEntrancePoint
{
  private readonly Config _config;
  private readonly IAnsiConsole _console;
  private readonly string _jiraUrlPostfix;
  private readonly ILogger _log = Log.ForContext<JiraEntrancePoint>();
  public JiraEntrancePoint (Config config, IAnsiConsole console, string jiraUrlPostfix)
  {
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
    
    var credential = GetCredential();
    task.JiraUsername = credential.username;
    task.JiraPassword = credential.password;
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

  private (string username, string password) GetCredential ()
  {
    throw new System.NotImplementedException();
  }
}