using System;
using ReleaseProcessAutomation.Jira.Utility;

namespace ReleaseProcessAutomation.Jira;

public class Jira
    : IJira
{
    private readonly IJiraRestClientProvider _jiraRestClientProvider;

    private JiraVersionCreator? _versionCreator;

    public IJiraVersionCreator VersionCreator => _versionCreator ??= new JiraVersionCreator(_jiraRestClientProvider.GetJiraRestClient());

    private JiraVersionReleaser? _versionReleaser;
    public IJiraVersionReleaser VersionReleaser => _versionReleaser ??= new JiraVersionReleaser(_jiraRestClientProvider.GetJiraRestClient());

    public Jira (IJiraRestClientProvider jiraRestClientProvider)
    {
        _jiraRestClientProvider = jiraRestClientProvider;
    }
}