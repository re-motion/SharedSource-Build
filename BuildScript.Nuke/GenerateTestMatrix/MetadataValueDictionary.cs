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

namespace Remotion.BuildScript.GenerateTestMatrix;

/// <summary>
///   Wraps a <see cref="IDictionary{TKey,TValue}" /> and performs case-insensitive access operations, emulating default
///   msbuild behavior.
/// </summary>
public class MetadataValueDictionary
{
  private readonly StringComparer _dictionaryStringComparer = StringComparer.OrdinalIgnoreCase;
  private readonly IReadOnlyDictionary<string, string> _rawDictionary;

  public MetadataValueDictionary (IDictionary<string, string> rawDictionary)
  {
    _rawDictionary = new Dictionary<string, string>(rawDictionary, _dictionaryStringComparer);
  }

  public IEnumerable<string> GetKeysForValue (string value)
  {
    return _rawDictionary
        .Where(kvp => _dictionaryStringComparer.Equals(kvp.Value, value))
        .Select(kvp => kvp.Key);
  }

  public bool ContainsKey (string key) => _rawDictionary.ContainsKey(key);

  public IEnumerable<KeyValuePair<string, string>> Intersect (IEnumerable<string> possibleKeys)
  {
    var enumeratedPossibleKeys = possibleKeys.ToArray();

    return _rawDictionary.Keys
        .Intersect(enumeratedPossibleKeys, _dictionaryStringComparer)
        .Select(key => new KeyValuePair<string, string>(key, _rawDictionary[key]));
  }
}