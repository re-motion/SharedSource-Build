using ReleaseProcessAutomation.Configuration.Data;
using ReleaseProcessAutomation.Jira.CredentialManagement;
using ReleaseProcessAutomation.Jira.ServiceFacadeImplementations;
using RestSharp.Authenticators;

namespace ReleaseProcessAutomation.Jira.Utility;

public class JiraRestClientProvider
    : IJiraRestClientProvider
{
  private const string c_urlPostFix = "rest/api/2/";
  
  private JiraRestClient? _jiraRestClient;

  private readonly Config _config;

  private readonly IJiraCredentialManager _jiraCredentialManager;

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
      _jiraRestClient = new JiraRestClient(jiraUrlWithPostfix, new NtlmAuthenticator());
    }
    else
    {
      var credentials = _jiraCredentialManager.GetCredential(_config.Jira.JiraURL);
      _jiraRestClient = new JiraRestClient(jiraUrlWithPostfix, new HttpBasicAuthenticator(credentials.Username, credentials.Password));
    }
    
    return _jiraRestClient;
  }
  
  private string JiraUrlWithPostfix (string url)
  {
    return url.EndsWith("/") ? $"{url}{c_urlPostFix}" : $"{url}/{c_urlPostFix}";
  }
}