using System;
using ReleaseProcessAutomation.Configuration.Data;
using ReleaseProcessAutomation.Jira.CredentialManagement;
using ReleaseProcessAutomation.Jira.ServiceFacadeImplementations;
using ReleaseProcessAutomation.Jira.Utility;
using RestSharp.Authenticators;

namespace ReleaseProcessAutomation.Jira;

public class Jira
    : IJira
{
    private readonly Config _config;
    private readonly IJiraCredentialManager _jiraCredentialManager;
    private readonly IJiraRestClientProvider _jiraRestClientProvider;

    private JiraVersionCreator? _versionCreator;

    public IJiraVersionCreator VersionCreator => _versionCreator ??= new JiraVersionCreator(_jiraRestClientProvider.GetJiraRestClient());

    private JiraVersionReleaser? _versionReleaser;
    public IJiraVersionReleaser VersionReleaser => _versionReleaser ??= new JiraVersionReleaser(_jiraRestClientProvider.GetJiraRestClient());
    
    public IJiraAuthenticationWrapper AuthenticationWrapper { get; }
    


    public Jira (Config config, IJiraCredentialManager jiraCredentialManager, IJiraAuthenticationWrapper authenticationWrapper, IJiraRestClientProvider jiraRestClientProvider)
    {
        _config = config;
        _jiraCredentialManager = jiraCredentialManager;
        _jiraRestClientProvider = jiraRestClientProvider;
        AuthenticationWrapper = authenticationWrapper;
    }
}