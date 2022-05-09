using Moq;
using NUnit.Framework;
using ReleaseProcessAutomation.Jira;
using ReleaseProcessAutomation.Configuration.Data;
using Spectre.Console;

namespace ReleaseProcessAutomation.Tests.Jira;
[TestFixture]
public class JiraTests
{
  [Test]
  public void JiraUrlWithPostfix_WithSlash_ReturnsProperUrl ()
  {
    var config = new Configuration.Data.Config();
    config.Jira = new JiraConfig();
    config.Jira.JiraURL = "JIRA_URL/";
    var consoleStub = new Mock<IAnsiConsole>();
    var postFix = "POSTFIX";
    var jira = new JiraEntrancePoint(config, consoleStub.Object, postFix);

    var output = jira.JiraUrlWithPostfix();
    
    Assert.That(output, Is.EqualTo($"{config.Jira.JiraURL}{postFix}"));
  }
  
  [Test]
  public void JiraUrlWithPostfix_WithoutSlash_ReturnsProperUrl ()
  {
    var config = new Configuration.Data.Config();
    config.Jira = new JiraConfig();
    config.Jira.JiraURL = "JIRA_URL";
    var consoleStub = new Mock<IAnsiConsole>();
    var postFix = "POSTFIX";
    var jira = new JiraEntrancePoint(config, consoleStub.Object, postFix);

    var output = jira.JiraUrlWithPostfix();
    
    Assert.That(output, Is.EqualTo($"{config.Jira.JiraURL}/{postFix}"));
  }
}