using ReleaseProcessAutomation.Configuration.Data;

namespace ReleaseProcessAutomation.Jira.Utility;

public abstract class JiraWithPostfix
{
  private readonly Config _config;
  private readonly string _jiraUrlPostfix;

  protected JiraWithPostfix (Config config, string jiraUrlPostfix)
  {
    _config = config;
    _jiraUrlPostfix = jiraUrlPostfix;
  }
  
  protected string JiraUrlWithPostfix (string url)
  {
    return url.EndsWith("/") ? $"{url}{_jiraUrlPostfix}" : $"{url}/{_jiraUrlPostfix}";
  }
  
  protected string JiraUrlWithPostfix()
  {
    return JiraUrlWithPostfix(_config.Jira.JiraURL);
  }
}