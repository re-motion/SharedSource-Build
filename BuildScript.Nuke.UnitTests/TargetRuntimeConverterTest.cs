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

using System;
using NUnit.Framework;
using Remotion.BuildScript.Nuke.GenerateTestMatrix;

namespace BuildScript.Nuke.UnitTests;

[TestFixture]
public class TargetRuntimeConverterTest
{
  [Test]
  public void ToNUnitFormatFromProjectFile_WithEmptyInput_ThrowsArgumentException ()
  {
    Assert.That(() => TargetRuntimeConverter.ToNUnitFormatFromProjectFile(""), Throws.InstanceOf<ArgumentException>());
  }

  [Test]
  public void ToNUnitFormatFromMoniker_WithEmptyInput_ThrowsArgumentException ()
  {
    Assert.That(() => TargetRuntimeConverter.ToNUnitFormatFromMoniker(""), Throws.InstanceOf<ArgumentException>());
  }

  [Test]
  public void ToTargetFrameworkMonikerFromNUnitFormat_WithEmptyInput_ThrowsArgumentException ()
  {
    Assert.That(() => TargetRuntimeConverter.ToTargetFrameworkMonikerFromNUnitFormat(""), Throws.InstanceOf<ArgumentException>());
  }

  [Test]
  [TestCase("net50", "NET-5.0")]
  [TestCase("net462", "NET-4.6.2")]
  public void ToNUnitFormatFromProjectFile_WithValidInput_ReturnsNUnitFormat (
      string targetFrameworkProjectFile,
      string expectedFormat)
  {
    var actualFormat = TargetRuntimeConverter.ToNUnitFormatFromProjectFile(targetFrameworkProjectFile);
    Assert.That(actualFormat, Is.EqualTo(expectedFormat));
  }

  [Test]
  [TestCase("NET462", "NET-4.6.2")]
  [TestCase("NET45", "NET-4.5")]
  public void ToNUnitFormatFromMoniker_WithValidInput_ReturnsNUnitFormat (
      string targetFrameworkMoniker,
      string expectedFormat)
  {
    var actualFormat = TargetRuntimeConverter.ToNUnitFormatFromMoniker(targetFrameworkMoniker);
    Assert.That(actualFormat, Is.EqualTo(expectedFormat));
  }

  [Test]
  [TestCase("NET-4.5", "NET45")]
  [TestCase("NET-4.8.5", "NET485")]
  public void ToTargetFrameworkMonikerFromNUnitFormat_WithValidInput_ReturnsTargetFrameworkMoniker (
      string nUnitFormat,
      string expectedFormat)
  {
    var actualFormat = TargetRuntimeConverter.ToTargetFrameworkMonikerFromNUnitFormat(nUnitFormat);
    Assert.That(actualFormat, Is.EqualTo(expectedFormat));
  }
}