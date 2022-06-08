using ReleaseProcessAutomation.Configuration.Data;
using ReleaseProcessAutomation.Jira.CredentialManagement;
using ReleaseProcessAutomation.Jira.ServiceFacadeImplementations;
using RestSharp.Authenticators;

namespace ReleaseProcessAutomation.Jira.Utility;

public class JiraRestClientProvider
    : IJiraRestClientProvider
{
  private JiraRestClient? _jiraRestClient;

  private readonly Config _config;

  private readonly IJiraCredentialManager _jiraCredentialManager;

  public JiraRestClientProvider (Config config, IJiraCredentialManager jiraCredentialManager)
  {
    _jiraCredentialManager = jiraCredentialManager;
    _config = config;
  }

  public JiraRestClient GetJiraRestClient()
  {
    if (_jiraRestClient != null)
      return _jiraRestClient;
    
    if (_config.Jira.UseNTLM)
    {
      _jiraRestClient = new JiraRestClient(_config.Jira.JiraURL, new NtlmAuthenticator());
    }
    else
    {
      var credentials = _jiraCredentialManager.GetCredential(_config.Jira.JiraURL);
      _jiraRestClient = new JiraRestClient(_config.Jira.JiraURL, new HttpBasicAuthenticator(credentials.Username, credentials.Password));
    }
    return _jiraRestClient;
  }
}