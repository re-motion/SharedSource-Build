using System;
using System.Net;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using ReleaseProcessAutomation.Configuration.Data;
using ReleaseProcessAutomation.ReadInput;
using ReleaseProcessAutomation.SemanticVersioning;
using Remotion.ReleaseProcessScript.Jira.ServiceFacadeImplementations;
using Serilog;
using Serilog.Events;
using Spectre.Console;

namespace ReleaseProcessAutomation.Jira;

public interface IJira
{
  void CreateAndReleaseJiraVersion (SemanticVersion currentVersion, SemanticVersion nextVersion, bool squashUnreleased = false);
}

public class Jira : IJira
{
  private readonly IInputReader _inputReader;
  private readonly Config _config;
  private readonly IAnsiConsole _console;
  private readonly string _jiraUrlPostfix;
  private readonly ILogger _log = Log.ForContext<Jira>();
  public Jira (IInputReader inputReader, Config config, IAnsiConsole console, string jiraUrlPostfix)
  {
    _inputReader = inputReader;
    _config = config;
    _console = console;
    _jiraUrlPostfix = jiraUrlPostfix;
  }

  internal struct Credentials
  {
    public string Username { get; set; } 
    
    public string Password { get; set; }
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
                                JiraProjectKey = _config.Jira.JiraProjectKey,
                                VersionNumber = version.ToString()
                            };
    
    AddAuthentication(jiraCreateVersion);
    
    jiraCreateVersion.Execute();

    return jiraCreateVersion.CreatedVersionID!;
  }

  private string JiraUrlWithPostfix ()
  {
    var url = _config.Jira.JiraURL;
    return url.EndsWith("/") ? $"{url}{_jiraUrlPostfix}" : $"{url}/{_jiraUrlPostfix}";
  }

  private void AddAuthentication (JiraTask task)
  {
    if (_config.Jira.UseNTLM)
      return;
    
    var credential = GetCredential();
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

  private Credentials GetCredential ()
  {

    var credentials = new Credentials
                      {
                        Username = "user",
                        Password = "password"
                      };

    if (CheckJiraAuthentication(credentials))
    {
      return credentials;
    }

    return AskForCredentials();

  }

  private Credentials AskForCredentials ()
  {
    var shouldContinue = true;
    while (shouldContinue)
    {
      var tmpCredentials = new Credentials
                           {
                               Username = _inputReader.ReadString("Please enter your Jira username"),
                               Password = _inputReader.ReadHiddenString("Please enter your Jira password")
                           };

      if (CheckJiraAuthentication(tmpCredentials))
      {
        _console.WriteLine("Do you want to save the password?");
        if (_inputReader.ReadConfirmation())
        {
          
          
          const string message = "Saved Password";
          _console.WriteLine(message);
          _log.Information(message);
        }
        return tmpCredentials;
      }
      _console.WriteLine("The input credentials didnt match, do you want to try again?");
      shouldContinue = _inputReader.ReadConfirmation();
    }

    throw new JiraAuthenticationException("Authentication not successful, user does not want to try again.");
  }

  private bool CheckJiraAuthentication (Credentials credentials)
  {
    try
    {
      CheckJiraCredentials(credentials);
    }
    catch (JiraException e)
    {
      if (e.HttpStatusCode.Equals(HttpStatusCode.Forbidden) || e.HttpStatusCode.Equals(HttpStatusCode.Unauthorized))
        return false;
      else
        throw;
    }

    return true;
  }

  private void CheckJiraCredentials (Credentials credentials)
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

  private void SaveCredentials (Credentials credentials)
  {
    DeleteCredentials();
  }

  private void DeleteCredentials ()
  {
    
  }
}