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
///   Wraps a <see cref="IReadOnlyCollection{T}" /> and performs case-insensitive access operations, emulating default
///   msbuild behavior.
/// </summary>
public class MetadataValueCollection
{
  private readonly StringComparer _collectionStringComparer = StringComparer.OrdinalIgnoreCase;
  private readonly ISet<string> _rawCollection;

  public MetadataValueCollection (IReadOnlyCollection<string> rawCollection)
  {
    var duplicateValues = rawCollection.GetDuplicateValues(_collectionStringComparer).ToList();
    if (duplicateValues.Any())
    {
      var jointDuplicateValues = string.Join(",", duplicateValues.Select(x => $"'{x}'"));
      throw new ArgumentException($"Duplicate values found in collection: {jointDuplicateValues}.", nameof(rawCollection));
    }

    _rawCollection = new HashSet<string>(rawCollection, _collectionStringComparer);
  }

  public string Single (IEnumerable<string> possibleValues, Func<Exception> createEmptySequenceException)
  {
    var enumeratedPossibleValues = possibleValues.ToArray();
    var intersection = _rawCollection.Intersect(enumeratedPossibleValues, _collectionStringComparer).ToArray();

    if (intersection.Length > 1)
    {
      var jointIntersection = string.Join(",", intersection.Select(x => $"'{x}'"));

      throw new InvalidOperationException(
          $"Found more than one entry in the collection matching the set of possible values. Values: {jointIntersection}");
    }

    if (intersection.Length == 0)
      throw createEmptySequenceException();
    return intersection[0];
  }

  public bool Contains (string value) => _rawCollection.Contains(value);

  public bool IsEmpty () => !_rawCollection.Any();
}
