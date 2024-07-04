// Copyright (c) rubicon IT GmbH, www.rubicon.eu
// 
// See the NOTICE file distributed with this work for additional information
// regarding copyright ownership.  rubicon licenses this file to you under
// the Apache License, Version 2.0 (the "License"); you may not use this
// file except in compliance with the License.  You may obtain a copy of the
// License at
// 
//   http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
// WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.  See the
// License for the specific language governing permissions and limitations
// under the License.

using NUnit.Framework;
using Remotion.BuildScript;

namespace BuildScript.Nuke.UnitTests;

[TestFixture]
public class AppConfigTest
{
  const string c_exampleAppConfig =
      """
      <configuration>
        <appSettings>
          <add key="MyKey" value="Value" />
        </appSettings>
        <remotion.webTesting
            xmlns="http://www.re-motion.org/WebTesting/Configuration/2.0"
            browser="Chrome"
            searchTimeout="00:00:30"
            verifyWebApplicationStartedTimeout="00:00:10"
            retryInterval="00:00:00.025"
            asyncJavaScriptTimeout="00:00:10"
            headless="true"
            webApplicationRoot="http://localhost:60402/"
            screenshotDirectory=".\WebTestingOutput"
            logsDirectory=".\WebTestingOutput"
            closeBrowserWindowsOnSetUpAndTearDown="false"
            requestErrorDetectionStrategy="AspNet">
          <hosting name="IisExpress" type="IisExpress" port="60402" />
        </remotion.webTesting>
      </configuration>
      """;

  static readonly (string, string)[] s_defaultNamespaces = new[] { ("rwt", "http://www.re-motion.org/WebTesting/Configuration/2.0") };

  [Test]
  public void GetAppSetting_ExistingKey ()
  {
    var appConfig = AppConfig.FromText(c_exampleAppConfig);

    Assert.That(appConfig.GetAppSetting("MyKey"), Is.EqualTo("Value"));
  }

  [Test]
  public void GetAppSetting_NonExistentKey ()
  {
    var appConfig = AppConfig.FromText(c_exampleAppConfig);

    Assert.That(appConfig.GetAppSetting("MyKey2"), Is.Null);
  }

  [Test]
  public void SetAppSetting_ExistingKey ()
  {
    var appConfig = AppConfig.FromText(c_exampleAppConfig);
    appConfig.SetAppSetting("MyKey", "CustomValue");

    Assert.That(appConfig.GetAppSetting("MyKey"), Is.EqualTo("CustomValue"));
  }

  [Test]
  public void SetAppSetting_NonExistentKey ()
  {
    var appConfig = AppConfig.FromText(c_exampleAppConfig);
    appConfig.SetAppSetting("MyKey2", "CustomValue");

    Assert.That(appConfig.GetAppSetting("MyKey2"), Is.EqualTo("CustomValue"));
  }

  [Test]
  public void GetAttribute_ExistingKey ()
  {
    var appConfig = AppConfig.FromText(c_exampleAppConfig, s_defaultNamespaces);

    Assert.That(appConfig.GetAttribute("/configuration/rwt:remotion.webTesting", "browser"), Is.EqualTo("Chrome"));
  }

  [Test]
  public void GetAttribute_NonExistentKey ()
  {
    var appConfig = AppConfig.FromText(c_exampleAppConfig);

    Assert.That(appConfig.GetAttribute("/configuration/remotion.webTesting", "name"), Is.Null);
  }

  [Test]
  public void SetOrAddAttribute_ExistingKey ()
  {
    var appConfig = AppConfig.FromText(c_exampleAppConfig, s_defaultNamespaces);
    appConfig.SetOrAddAttribute("/configuration/rwt:remotion.webTesting", "browser", "Edge");

    Assert.That(appConfig.GetAttribute("/configuration/rwt:remotion.webTesting", "browser"), Is.EqualTo("Edge"));
  }

  [Test]
  public void SetOrAddAttribute_NonExistentKey ()
  {
    var appConfig = AppConfig.FromText(c_exampleAppConfig, s_defaultNamespaces);
    appConfig.SetOrAddAttribute("/configuration/remotion.webTesting", "name", "Test");

    Assert.That(appConfig.GetAttribute("/configuration/rwt:remotion.webTesting", "name"), Is.EqualTo("Test"));
  }
}