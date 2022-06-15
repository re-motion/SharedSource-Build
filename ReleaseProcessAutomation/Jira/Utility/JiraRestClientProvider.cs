using ReleaseProcessAutomation.Configuration.Data;
using ReleaseProcessAutomation.Jira.CredentialManagement;
using ReleaseProcessAutomation.Jira.ServiceFacadeImplementations;
using RestSharp.Authenticators;

namespace ReleaseProcessAutomation.Jira.Utility;

public class JiraRestClientProvider
    : IJiraRestClientProvider
{
  private const string c_urlPostFix = "rest/api/2/";
  
  private readonly Config _config;
  private readonly IJiraCredentialManager _jiraCredentialManager;

  private JiraRestClient? _jiraRestClient;

  public JiraRestClientProvider (Config config, IJiraCredentialManager jiraCredentialManager)
  {
    _jiraCredentialManager = jiraCredentialManager;
    _config = config;
  }

  public JiraRestClient GetJiraRestClient ()
  {
    if (_jiraRestClient != null)
      return _jiraRestClient;

    var jiraUrlWithPostfix = JiraUrlWithPostfix(_config.Jira.JiraURL);
    
    if (_config.Jira.UseNTLM)
    {
      _jiraRestClient = JiraRestClient.CreateWithNtlmAuthentication(jiraUrlWithPostfix);
    }
    else
    {
      var credentials = _jiraCredentialManager.GetCredential(_config.Jira.JiraURL);
      _jiraRestClient = JiraRestClient.CreateWithBasicAuthentication(jiraUrlWithPostfix, credentials);
    }
    
    return _jiraRestClient;
  }
  
  private string JiraUrlWithPostfix (string url)
  {
    return url.EndsWith("/") ? $"{url}{c_urlPostFix}" : $"{url}/{c_urlPostFix}";
  }
}