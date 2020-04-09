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
//
using NUnit.Framework;
using Remotion.BuildScript.BuildTasks;

namespace Remotion.BuildScript.UnitTests
{
  [TestFixture]
  public class TargetRuntimeConverterTest
  {
    [Test]
    [TestCase ("NET48", "NET-4.8")]
    [TestCase ("net48", "NET-4.8")]
    [TestCase ("nEt48", "NET-4.8")]
    public void ToNUnitFormat_WithSingleDecimalPlace_CaseInsensitive (string input, string expected)
    {
      var result = TargetRuntimeConverter.ToNUnitFormat (input);

      Assert.That (result, Is.EqualTo (expected));
    }

    [Test]
    [TestCase ("NET472", "NET-4.7.2")]
    [TestCase ("net472", "NET-4.7.2")]
    [TestCase ("nEt472", "NET-4.7.2")]
    public void ToNUnitFormat_WithMultipleDecimalPlaces_CaseInsensitive (string input, string expected)
    {
      var result = TargetRuntimeConverter.ToNUnitFormat (input);

      Assert.That (result, Is.EqualTo (expected));
    }

    [Test]
    [TestCase ("NET-4.8", "NET48")]
    [TestCase ("net-4.8", "NET48")]
    [TestCase ("nEt-4.8", "NET48")]
    public void ToTargetFrameworkMoniker_WithSingleDecimalPlace_CaseInsensitive (string input, string expected)
    {
      var result = TargetRuntimeConverter.ToTargetFrameworkMoniker (input);

      Assert.That (result, Is.EqualTo (expected));
    }

    [Test]
    [TestCase ("NET-4.7.2", "NET472")]
    [TestCase ("net-4.7.2", "NET472")]
    [TestCase ("nEt-4.7.2", "NET472")]
    public void ToTargetFrameworkMoniker_WithMultipleDecimalPlaces_CaseInsensitive (string input, string expected)
    {
      var result = TargetRuntimeConverter.ToTargetFrameworkMoniker (input);

      Assert.That (result, Is.EqualTo (expected));
    }
  }
}