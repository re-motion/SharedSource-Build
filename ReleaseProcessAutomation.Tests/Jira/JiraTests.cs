using JetBrains.Annotations;
using Moq;
using NUnit.Framework;
using ReleaseProcessAutomation.Jira;
using ReleaseProcessAutomation.Configuration.Data;
using ReleaseProcessAutomation.Jira.Utility;
using Spectre.Console;

namespace ReleaseProcessAutomation.Tests.Jira;
[TestFixture]
public class JiraTests
{
  private const string c_postFix = "POSTFIX";

  internal class JiraTest : JiraWithPostfix
  {
    public JiraTest ([NotNull] Configuration.Data.Config config, [NotNull] string jiraUrlPostfix)
        : base(config, jiraUrlPostfix)
    {
    }

    public string JiraUrlWithPostfixWrapper (string jiraUrl)
    {
      return JiraUrlWithPostfix(jiraUrl);
    }
  }

  [Test]
  public void JiraUrlWithPostfix_WithSlash_ReturnsProperUrl ()
  {
    var config = new Configuration.Data.Config();
    config.Jira = new JiraConfig();
    config.Jira.JiraURL = "JIRA_URL/";
    
    
    var jira = new JiraTest(config, c_postFix);

    var output = jira.JiraUrlWithPostfixWrapper(config.Jira.JiraURL);
    
    Assert.That(output, Is.EqualTo($"{config.Jira.JiraURL}{c_postFix}"));
  }
  
  [Test]
  public void JiraUrlWithPostfix_WithoutSlash_ReturnsProperUrl ()
  {
    var config = new Configuration.Data.Config();
    config.Jira = new JiraConfig();
    config.Jira.JiraURL = "JIRA_URL";
    var jira = new JiraTest(config, c_postFix);


    var output = jira.JiraUrlWithPostfixWrapper(config.Jira.JiraURL);
    
    Assert.That(output, Is.EqualTo($"{config.Jira.JiraURL}/{c_postFix}"));
  }
}