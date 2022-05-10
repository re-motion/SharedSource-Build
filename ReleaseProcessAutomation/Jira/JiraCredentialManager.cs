using System;
using System.Net;
using AdysTech.CredentialManager;
using ReleaseProcessAutomation.Jira.ServiceFacadeImplementations;
using ReleaseProcessAutomation.ReadInput;
using Serilog;
using Spectre.Console;

namespace ReleaseProcessAutomation.Jira;

public interface IJiraCredentialManager
{
  Credentials GetCredential (string target);
}

public class JiraCredentialManager
    : IJiraCredentialManager
{
  private IJiraEntrancePoint _jira;
  private readonly IInputReader _inputReader;
  private readonly IAnsiConsole _console;
  private readonly ILogger _log = Log.ForContext<JiraCredentialManager>();

  public JiraCredentialManager (IJiraEntrancePoint jira, IInputReader inputReader, IAnsiConsole console)
  {
    _jira = jira;
    _inputReader = inputReader;
    _console = console;
  }

  public Credentials GetCredential (string target)
  {
    var cred = CredentialManager.GetCredentials(target);

    if (cred == null)
    {
      return AskForCredentials(target);
    }
    
    var credentials = new Credentials
                      {
                          Username = cred.UserName,
                          Password = cred.Password
                      };
    
    if (CheckJiraAuthentication(credentials))
    {
      return credentials;
    }

    _console.WriteLine("Invalid Jira Credentials saved.");
    return AskForCredentials(target);

  }

  private Credentials AskForCredentials (string target)
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
        _console.WriteLine("Do you want to save the login information to the credential manager?");
        if (_inputReader.ReadConfirmation())
        {
          SaveCredentials(tmpCredentials, target);

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

  private void SaveCredentials (Credentials tmpCredentials, string target)
  {
    var cred = new NetworkCredential(tmpCredentials.Username, tmpCredentials.Password);
    try
    {
      CredentialManager.RemoveCredentials(target);
    }
    catch
    {
      // ignored
    }

    CredentialManager.SaveCredentials(target, cred);
  }

  private bool CheckJiraAuthentication (Credentials credentials)
  {
    try
    {
      _jira.CheckJiraCredentials(credentials);
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

  
}