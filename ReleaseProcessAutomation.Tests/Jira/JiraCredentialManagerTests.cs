using System.Net;
using AdysTech.CredentialManager;
using Moq;
using NUnit.Framework;
using ReleaseProcessAutomation.Configuration.Data;
using ReleaseProcessAutomation.Jira;
using ReleaseProcessAutomation.Jira.CredentialManagement;
using ReleaseProcessAutomation.Jira.ServiceFacadeImplementations;
using ReleaseProcessAutomation.ReadInput;
using RestSharp.Authenticators;
using Spectre.Console.Testing;

namespace ReleaseProcessAutomation.Tests.Jira;

[TestFixture]
public class JiraCredentialManagerTests
{
  private Mock<IInputReader> _inputReaderMock;
  private Mock<IJira> _jiraMock;
  private TestConsole _console;
  private Configuration.Data.Config _config;

  private const string c_userName = "user";
  private const string c_password = "password";
  private const string c_target = "target";
  private const string c_postfix = "postfix";

  [SetUp]
  public void Setup ()
  {
    _inputReaderMock = new Mock<IInputReader>();
    _jiraMock = new Mock<IJira>();
    _console = new TestConsole();
    _config = new Configuration.Data.Config();
    _config.Jira = new JiraConfig();
    _config.Jira.JiraURL = "https://www.JiraURL.com/url";
    _config.Jira.JiraProjectKey = "JiraProjectKey";
    _jiraMock.Setup(
        _ => _.AuthenticationWrapper.CheckAuthentication(
            It.IsAny<Credentials>(),
            _config.Jira.JiraProjectKey,
            _config.Jira.JiraURL));
  }

  [TearDown]
  public void TearDown ()
  {
    try
    {
      CredentialManager.RemoveCredentials(c_target);
    }
    catch
    {
      //ignore
    }
  }

  [Test]
  public void GetCredential_CredentialsInCredentialManager_ReturnsCredentials ()
  {
    var jiraCredentialManager = new JiraCredentialManager(_config, _inputReaderMock.Object, _console, _jiraMock.Object, c_postfix);

    var cred = new NetworkCredential(c_userName, c_password);

    CredentialManager.SaveCredentials(c_target, cred);

    var output = jiraCredentialManager.GetCredential(c_target);

    Assert.That(output.Username, Is.EqualTo(c_userName));
    Assert.That(output.Password, Is.EqualTo(c_password));
  }

  [Test]
  public void GetCredential_NoCredentialsInCredentialManager_AsksUser ()
  {
    var jiraCredentialManager = new JiraCredentialManager(_config, _inputReaderMock.Object, _console,  _jiraMock.Object, c_postfix);

    _inputReaderMock.Setup(_ => _.ReadString(It.IsAny<string>())).Returns(c_userName);
    _inputReaderMock.Setup(_ => _.ReadHiddenString(It.IsAny<string>())).Returns(c_password);
    _inputReaderMock.Setup(_ => _.ReadConfirmation(It.IsAny<bool>())).Returns(false);

    jiraCredentialManager.GetCredential(c_target);

    _inputReaderMock.Verify(_ => _.ReadString(It.IsAny<string>()));
    _inputReaderMock.Verify(_ => _.ReadHiddenString(It.IsAny<string>()));
    _inputReaderMock.Verify(_ => _.ReadConfirmation(It.IsAny<bool>()));
  }

  [Test]
  public void GetCredential_AsksUser_SavesPassword ()
  {
    var jiraCredentialManager = new JiraCredentialManager(_config, _inputReaderMock.Object, _console,  _jiraMock.Object, c_postfix);

    _inputReaderMock.Setup(_ => _.ReadString(It.IsAny<string>())).Returns(c_userName);
    _inputReaderMock.Setup(_ => _.ReadHiddenString(It.IsAny<string>())).Returns(c_password);
    _inputReaderMock.Setup(_ => _.ReadConfirmation(It.IsAny<bool>())).Returns(true);

    jiraCredentialManager.GetCredential(c_target);

    var createdCredentials = CredentialManager.GetCredentials(c_target);

    Assert.That(createdCredentials.UserName, Is.EqualTo(c_userName));
    Assert.That(createdCredentials.Password, Is.EqualTo(c_password));
  }

  [Test]
  public void GetCredential_WithWrongCredentials_AsksUser ()
  {
    var username = "NotUserName";
    var password = "NotPassword";
    var networkCredential = new NetworkCredential(username, password);
    CredentialManager.SaveCredentials(c_target, networkCredential);

    var jiraCredentialManager = new JiraCredentialManager(_config, _inputReaderMock.Object, _console,  _jiraMock.Object, c_postfix);

    _jiraMock.Setup(_ => _.AuthenticationWrapper.CheckAuthentication(
            It.IsAny<Credentials>(), _config.Jira.JiraProjectKey, _config.Jira.JiraURL))
        .Throws(new JiraException("error") { HttpStatusCode = HttpStatusCode.Unauthorized });
    
    _inputReaderMock.Setup(_ => _.ReadString(It.IsAny<string>())).Returns(c_userName);
    _inputReaderMock.Setup(_ => _.ReadHiddenString(It.IsAny<string>())).Returns(c_password);
    _inputReaderMock.Setup(_ => _.ReadConfirmation(It.IsAny<bool>())).Returns(false);

    Assert.That(() => jiraCredentialManager.GetCredential(c_target), Throws.InstanceOf<JiraAuthenticationException>()
        .With.Message.EqualTo("Authentication not successful, user does not want to try again."));

    _inputReaderMock.Verify(_ => _.ReadString(It.IsAny<string>()));
    _inputReaderMock.Verify(_ => _.ReadHiddenString(It.IsAny<string>()));
    _inputReaderMock.Verify(_ => _.ReadConfirmation(It.IsAny<bool>()));

    _jiraMock.Verify(_ => _.AuthenticationWrapper.CheckAuthentication(
        It.IsAny<Credentials>(), _config.Jira.JiraProjectKey, _config.Jira.JiraURL));
  }

  [Test]
  public void GetCredential_WithWrongCredentials_SavesNewCredentials ()
  {
    var networkCredential = new NetworkCredential("NotUserName", "NotPassword");
    CredentialManager.SaveCredentials(c_target, networkCredential);

    var jiraCredentialManager = new JiraCredentialManager(_config, _inputReaderMock.Object, _console,  _jiraMock.Object, c_postfix);
    var sequence = new MockSequence();
    
    _jiraMock.InSequence(sequence).Setup(_ => _.AuthenticationWrapper.CheckAuthentication(
            It.IsAny<Credentials>(), _config.Jira.JiraProjectKey, _config.Jira.JiraURL))
        .Throws(new JiraException("error") { HttpStatusCode = HttpStatusCode.Unauthorized });
    
    _jiraMock.InSequence(sequence).Setup(_ => _.AuthenticationWrapper.CheckAuthentication(It.IsAny<Credentials>(), _config.Jira.JiraProjectKey, _config.Jira.JiraURL));

    _inputReaderMock.Setup(_ => _.ReadString(It.IsAny<string>())).Returns(c_userName);
    _inputReaderMock.Setup(_ => _.ReadHiddenString(It.IsAny<string>())).Returns(c_password);
    _inputReaderMock.Setup(_ => _.ReadConfirmation(It.IsAny<bool>())).Returns(true);

    jiraCredentialManager.GetCredential(c_target);

    var output = CredentialManager.GetCredentials(c_target);

    Assert.That(output.UserName, Is.EqualTo(c_userName));
    Assert.That(output.Password, Is.EqualTo(c_password));
    
    _jiraMock.Verify(_=> _.AuthenticationWrapper.CheckAuthentication(It.IsAny<Credentials>(), _config.Jira.JiraProjectKey, _config.Jira.JiraURL),Times.Exactly(2));
  }

  [Test]
  public void GetCredential_WithWrongCredentialInput_RepeatsFiveTimes ()
  {
    var jiraCredentialManager = new JiraCredentialManager(_config, _inputReaderMock.Object, _console,  _jiraMock.Object, c_postfix);

    _jiraMock.Setup(_ => _.AuthenticationWrapper.CheckAuthentication(
            It.IsAny<Credentials>(), _config.Jira.JiraProjectKey, _config.Jira.JiraURL))
        .Throws(new JiraException("error") { HttpStatusCode = HttpStatusCode.Unauthorized });

    _inputReaderMock.Setup(_ => _.ReadString(It.IsAny<string>())).Returns(c_userName);
    _inputReaderMock.Setup(_ => _.ReadHiddenString(It.IsAny<string>())).Returns(c_password);

    var callCount = 5;
    _inputReaderMock.SetupSequence(_ => _.ReadConfirmation(It.IsAny<bool>()))
        .Returns(true)
        .Returns(true)
        .Returns(true)
        .Returns(true)
        .Returns(false);

    Assert.That(() => jiraCredentialManager.GetCredential(c_target), Throws.InstanceOf<JiraAuthenticationException>()
        .With.Message.EqualTo("Authentication not successful, user does not want to try again."));

    _jiraMock.Verify(_ => _.AuthenticationWrapper.CheckAuthentication(
        It.IsAny<Credentials>(), _config.Jira.JiraProjectKey, _config.Jira.JiraURL), Times.Exactly(callCount));
  }
}