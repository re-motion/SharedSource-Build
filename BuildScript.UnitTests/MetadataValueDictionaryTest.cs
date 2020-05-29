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
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Remotion.BuildScript.BuildTasks;

namespace Remotion.BuildScript.UnitTests
{
  [TestFixture]
  public class MetadataValueDictionaryTest
  {
    [Test]
    public void Single_Exists_ValueNotNull ()
    {
      var rawCollection = new Dictionary<string, string> { { "A", "B" }, { "C", "D" } };
      var collection = new MetadataValueDictionary (rawCollection);

      var kvp = collection.Single (new[] { "A", "E" }, () => new Exception());

      Assert.That (kvp.Key, Is.EqualTo ("A"));
      Assert.That (kvp.Value, Is.EqualTo ("B"));
    }

    [Test]
    [TestCase ("A", "a")]
    [TestCase ("a", "A")]
    [TestCase ("MiXeD", "mIxEd")]
    public void Single_IgnoresCase (string actualKey, string searchKey)
    {
      var rawCollection = new Dictionary<string, string> { { actualKey, "B" }, { "C", "D" } };
      var collection = new MetadataValueDictionary (rawCollection);

      var kvp = collection.Single (new[] { searchKey, "e" }, () => new Exception());

      Assert.That (kvp.Key, Is.EqualTo (actualKey));
      Assert.That (kvp.Value, Is.EqualTo ("B"));
    }

    [Test]
    [TestCase ("AAA", "CCC")]
    [TestCase ("aaa", "ccc")]
    [TestCase ("AAA", "ccc")]
    [TestCase ("AaA", "cCc")]
    public void Single_MultipleKeys (string searchKey1, string searchKey2)
    {
      var rawCollection = new Dictionary<string, string> { { "AAA", "BBB" }, { "CCC", "DDD" } };
      var collection = new MetadataValueDictionary (rawCollection);
      var searchKeys = new[] { searchKey1, searchKey2, "RegularSearchKey" };

      Assert.That (
          () => collection.Single (searchKeys, () => new Exception()),
          Throws.InvalidOperationException.With.Message.EqualTo ("Found more than one entry in the dictionary for the set of possible keys. Keys: 'AAA','CCC'")
          );
    }

    [Test]
    public void Single_DoesNotExist_ThrowsException ()
    {
      var rawCollection = new Dictionary<string, string> { { "A", "B" }, { "C", "D" } };
      var collection = new MetadataValueDictionary (rawCollection);

      Assert.That (() => collection.Single (new[] { "E", "F" }, () => new Exception()), Throws.Exception);
    }

    [Test]
    [TestCase ("C", "c")]
    [TestCase ("c", "C")]
    [TestCase ("MiXeD", "mIxEd")]
    public void GetKeysForValue_IgnoresCase (string actualValue, string searchValue)
    {
      var rawCollection = new Dictionary<string, string> { { "A", actualValue }, { "D", actualValue } };
      var collection = new MetadataValueDictionary (rawCollection);

      var result = collection.GetKeysForValue (searchValue);

      Assert.That (result, Is.EqualTo (new[] { "A", "D" }));
    }

    [Test]
    [TestCase ("C", "c")]
    [TestCase ("c", "C")]
    [TestCase ("MiXeD", "mIxEd")]
    public void ContainsKey_IgnoresCase (string actualKey, string searchKey)
    {
      var rawCollection = new Dictionary<string, string> { { "A", "B" }, { actualKey, "D" } };
      var collection = new MetadataValueDictionary (rawCollection);

      var result = collection.ContainsKey (searchKey);

      Assert.That (result, Is.True);
    }

    [Test]
    [TestCase ("AAA", "CCC")]
    [TestCase ("aaa", "ccc")]
    [TestCase ("AAA", "ccc")]
    [TestCase ("AaA", "cCc")]
    public void Intersect_IsCaseInsensitive (string searchKey1, string searchKey2)
    {
      var rawCollection = new Dictionary<string, string> { { "AAA", "BBB" }, { "CCC", "DDD" } };
      var collection = new MetadataValueDictionary (rawCollection);
      var searchKeys = new[] { searchKey1, searchKey2, "RegularSearchKey" };
      var expected = new[] { new KeyValuePair<string, string> ("AAA", "BBB"), new KeyValuePair<string, string> ("CCC", "DDD") };

      var intersection = collection.Intersect (searchKeys).ToArray();

      Assert.That (intersection, Is.EquivalentTo (expected));
    }
  }
}