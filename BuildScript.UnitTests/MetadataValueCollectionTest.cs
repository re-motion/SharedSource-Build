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
using Remotion.BuildScript.BuildTasks;

namespace Remotion.BuildScript.UnitTests
{
  [TestFixture]
  public class MetadataValueCollectionTest
  {
    [Test]
    [TestCase ("AAA", "AAA")]
    [TestCase ("AAA", "aaa")]
    [TestCase ("AaA", "aAa")]
    public void Initialize_MultipleValues (string value1, string value2)
    {
      var rawCollection = new[] { value1, value2, "BBB", "BBB" };

      Assert.That (
          () => new MetadataValueCollection (rawCollection),
          Throws.ArgumentException.With.Message.Contains ($"Duplicate values found in collection: '{value1}','BBB'.")
          );
    }

    [Test]
    public void Single_Exists_ValueNotNull ()
    {
      var rawCollection = new[] { "A", "B" };
      var collection = new MetadataValueCollection (rawCollection);

      var value = collection.Single (new[] { "B", "C" }, () => new Exception());

      Assert.That (value, Is.EqualTo ("B"));
    }

    [Test]
    public void Single_DoesNotExist_ThrowsException ()
    {
      var rawCollection = new[] { "A", "B" };
      var collection = new MetadataValueCollection (rawCollection);

      Assert.That (
          () => collection.Single (new[] { "C", "D" }, () => new Exception()),
          Throws.Exception);
    }

    [Test]
    [TestCase ("a", "A")]
    [TestCase ("A", "a")]
    [TestCase ("MiXeD", "mIxEd")]
    public void Single_IgnoresCase (string actualValue, string searchValue)
    {
      var rawCollection = new[] { actualValue, "dEf" };
      var collection = new MetadataValueCollection (rawCollection);

      var value = collection.Single (new[] { searchValue, "ghi" }, () => new Exception());

      Assert.That (value, Is.EqualTo (actualValue));
    }

    [Test]
    [TestCase ("AAA", "BBB")]
    [TestCase ("aaa", "bbb")]
    [TestCase ("AAA", "bbb")]
    [TestCase ("AaA", "bBb")]
    public void Single_MultipleKeys (string searchKey1, string searchKey2)
    {
      var rawCollection = new[] { "AAA", "BBB" };
      var collection = new MetadataValueCollection (rawCollection);
      var searchKeys = new[] { searchKey1, searchKey2, "RegularSearchKey" };

      Assert.That (
          () => collection.Single (searchKeys, () => new Exception()),
          Throws.InvalidOperationException.With.Message.EqualTo ("Found more than one entry in the collection matching the set of possible values. Values: 'AAA','BBB'")
          );
    }

    [Test]
    [TestCase ("a", "A")]
    [TestCase ("A", "a")]
    [TestCase ("MiXeD", "mIxEd")]
    public void Contains_IgnoresCase (string actualValue, string searchValue)
    {
      var rawCollection = new[] { actualValue, "B" };
      var collection = new MetadataValueCollection (rawCollection);

      var result = collection.Contains (searchValue);

      Assert.That (result, Is.True);
    }

    [Test]
    public void IsEmpty ()
    {
      var rawCollection = new string[0];
      var collection = new MetadataValueCollection (rawCollection);

      var result = collection.IsEmpty();

      Assert.That (result, Is.True);
    }
  }
}