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
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography.X509Certificates;
using System.Xml.Schema;
using ReleaseProcessAutomation.Configuration.Data;
using ReleaseProcessAutomation.Jira.ServiceFacadeImplementations;
using ReleaseProcessAutomation.Jira.Utility;
using ReleaseProcessAutomation.SemanticVersioning;
using RestSharp.Authenticators;
using Serilog;
using Spectre.Console;

namespace ReleaseProcessAutomation.Jira;

public interface IJiraEntrancePoint
{
  void CreateAndReleaseJiraVersion (SemanticVersion currentVersion, SemanticVersion nextVersion, bool squashUnreleased = false);

}

public class JiraEntrancePoint : JiraWithPostfix, IJiraEntrancePoint
{
  private readonly JiraRestClient _jiraRestClient;
  private readonly Config _config;
  private readonly IAnsiConsole _console;
  private readonly ILogger _log = Log.ForContext<JiraEntrancePoint>();

  public JiraEntrancePoint (Config config, IAnsiConsole console, IJiraCredentialManager jiraCredentialManager, string jiraUrlPostfix) : base(config, jiraUrlPostfix)
  {
    _config = config;
    _console = console;

    if (config.Jira.UseNTLM)
    {
      _jiraRestClient = new JiraRestClient(config.Jira.JiraURL, new NtlmAuthenticator());
    }
    else
    {
      var credentials = jiraCredentialManager.GetCredential(config.Jira.JiraURL);
      _jiraRestClient = new JiraRestClient(config.Jira.JiraURL, new HttpBasicAuthenticator(credentials.Username, credentials.Password));
    }
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
    var creator = new JiraVersionCreator(_jiraRestClient);
    return creator.CreateNewVersionWithVersionNumber(_config.Jira.JiraProjectKey, version.ToString());
  }

  private void ReleaseVersion (string currentVersionID, string nextVersionID, bool squashUnreleased)
  {
    var releaser = new JiraVersionReleaser(_jiraRestClient);
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