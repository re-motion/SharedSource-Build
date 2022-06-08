// Copyright (c) rubicon IT GmbH, www.rubicon.eu
//
// See the NOTICE file distributed with this work for additional information
// regarding copyright ownership.  rubicon licenses this file to you under 
// the Apache License, Version 2.0 (the "License"); you may not use this 
// file except in compliance with the License.  You may obtain a copy of the 
// License at
//
//   http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software 
// distributed under the License is distributed on an "AS IS" BASIS, WITHOUT 
// WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.  See the 
// License for the specific language governing permissions and limitations
// under the License.
//

using System;
using System.Net;
using AdysTech.CredentialManager;
using ReleaseProcessAutomation.Configuration.Data;
using ReleaseProcessAutomation.Jira.Authentication;
using ReleaseProcessAutomation.Jira.Utility;
using ReleaseProcessAutomation.ReadInput;
using Serilog;
using Spectre.Console;

namespace ReleaseProcessAutomation.Jira.CredentialManagement;

public class JiraCredentialManager
    : JiraWithPostfix, IJiraCredentialManager
{
  private readonly Config _config;
  private readonly IInputReader _inputReader;
  private readonly IAnsiConsole _console;
  private readonly IJiraAuthenticator _jiraAuthenticator;
  private readonly ILogger _log = Log.ForContext<JiraCredentialManager>();

  public JiraCredentialManager (
      Config config,
      IInputReader inputReader,
      IAnsiConsole console,
      IJiraAuthenticator jiraAuthenticator,
      string jiraUrlPostfix)
      : base(config, jiraUrlPostfix)
  {
    _config = config;
    _inputReader = inputReader;
    _console = console;
    _jiraAuthenticator = jiraAuthenticator;
  }

  public Credentials GetCredential (string target)
  {
    var cred = CredentialManager.GetCredentials(target);

    if (cred == null)
      return AskForCredentials(target);

    var credentials = new Credentials
                      {
                          Username = cred.UserName,
                          Password = cred.Password
                      };

    try
    {
      CheckJiraCredentials(credentials);
    }
    catch
    {
      _console.WriteLine("Invalid Jira Credentials saved.");
      return AskForCredentials(target);
    }

    return credentials;
  }

  private Credentials AskForCredentials (string target)
  {
    while (true)
    {
      var tmpCredentials = new Credentials
                           {
                               Username = _inputReader.ReadString("Please enter your Jira username"),
                               Password = _inputReader.ReadHiddenString("Please enter your Jira password")
                           };

      try
      {
        CheckJiraCredentials(tmpCredentials);
      }
      catch (Exception e)
      {
        _console.WriteLine("The input credentials didnt match, do you want to try again?");
        if (_inputReader.ReadConfirmation())
          continue;
        throw new JiraAuthenticationException("Authentication not successful, user does not want to try again.", e);
      }

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

  private void CheckJiraCredentials (Credentials credentials)
  {
    try
    {
      _jiraAuthenticator.CheckAuthentication(credentials, _config.Jira.JiraProjectKey, _config.Jira.JiraURL);
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