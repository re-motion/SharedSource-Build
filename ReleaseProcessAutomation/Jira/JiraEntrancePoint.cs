using System;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography.X509Certificates;
using System.Xml.Schema;
using ReleaseProcessAutomation.Configuration.Data;
using ReleaseProcessAutomation.Jira.ServiceFacadeImplementations;
using ReleaseProcessAutomation.SemanticVersioning;
using Serilog;
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

  public JiraEntrancePoint (Config config, IAnsiConsole console, IJiraCredentialManager jiraCredentialManager, string jiraUrlPostfix)
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
  
  public void CheckJiraCredentials (Credentials credentials)
  {
    var checkAuthentication = new JiraCheckAuthentication(credentials.Username, credentials.Password);
    try
    {
      checkAuthentication.CheckAuthentication(JiraUrlWithPostfix(), _config.Jira.JiraProjectKey);
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

  private string CreateVersion (SemanticVersion version)
  {
    var creator = GetNewCreator();
    return creator.CreateNewVersionWithVersionNumber(JiraUrlWithPostfix(), _config.Jira.JiraProjectKey, version.ToString());
  }

  public string JiraUrlWithPostfix (string url)
  {
    return url.EndsWith("/") ? $"{url}{_jiraUrlPostfix}" : $"{url}/{_jiraUrlPostfix}";
  }
  
  private string JiraUrlWithPostfix()
  {
    return JiraUrlWithPostfix(_config.Jira.JiraURL);
  }


  private JiraVersionCreator GetNewCreator ()
  {
    if (_config.Jira.UseNTLM)
    {
      return new JiraVersionCreator(null, null);
    }

    var credential = _jiraCredentialManager.GetCredential(_config.Jira.JiraURL);
    
    return new JiraVersionCreator(credential.Username, credential.Password);
  }
  
  private JiraVersionReleaser GetNewReleaser ()
  {
    if (_config.Jira.UseNTLM)
    {
      return new JiraVersionReleaser(null, null);
    }

    var credential = _jiraCredentialManager.GetCredential(_config.Jira.JiraURL);
    
    return new JiraVersionReleaser(credential.Username, credential.Password);
  }

  private void ReleaseVersion (string currentVersionID, string nextVersionID, bool squashUnreleased)
  {
    var releaser = GetNewReleaser();
    if (squashUnreleased)
    {
      releaser.ReleaseVersionAndSquashUnreleased(JiraUrlWithPostfix(), _config.Jira.JiraProjectKey, currentVersionID, nextVersionID);
    }
    else
    {
      releaser.ReleaseVersion(JiraUrlWithPostfix(), currentVersionID, nextVersionID, false);
    }
  }
  
}