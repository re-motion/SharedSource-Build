using System;
using ReleaseProcessAutomation.Configuration.Data;
using ReleaseProcessAutomation.Jira.CredentialManagement;
using ReleaseProcessAutomation.Jira.ServiceFacadeImplementations;
using RestSharp.Authenticators;

namespace ReleaseProcessAutomation.Jira;

public class Jira
    : IJira
{
    private readonly Config _config;
    private readonly IJiraCredentialManager _jiraCredentialManager;

    private JiraVersionCreator? _versionCreator;

    public IJiraVersionCreator VersionCreator => _versionCreator ??= new JiraVersionCreator(JiraRestClient);

    private JiraVersionReleaser? _versionReleaser;
    public IJiraVersionReleaser VersionReleaser => _versionReleaser ??= new JiraVersionReleaser(JiraRestClient);
    
    public IJiraAuthenticationWrapper AuthenticationWrapper { get; }

    private JiraRestClient? _jiraRestClient;
    private JiraRestClient JiraRestClient
    {
        get
        {
            if (_jiraRestClient == null)
            {
                if (_config.Jira.UseNTLM)
                {
                    _jiraRestClient = new JiraRestClient(_config.Jira.JiraURL, new NtlmAuthenticator());
                }
                else
                {
                    var credentials = _jiraCredentialManager.GetCredential(_config.Jira.JiraURL);
                    //var credentials = new Credentials { Username = "user", Password = "password" };
                    _jiraRestClient = new JiraRestClient(_config.Jira.JiraURL, new HttpBasicAuthenticator(credentials.Username, credentials.Password));
                }
            }

            return _jiraRestClient;
        }
    }


    public Jira (Config config, IJiraCredentialManager jiraCredentialManager, IJiraAuthenticationWrapper authenticationWrapper)
    {
        _config = config;
        _jiraCredentialManager = jiraCredentialManager;
        AuthenticationWrapper = authenticationWrapper;
    }
}