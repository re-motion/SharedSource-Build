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

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Nuke.Common.IO;

namespace Remotion.BuildScript;

public class ProjectMetadata
{
  public string Name { get; }

  public AbsolutePath Path { get; }

  public ImmutableDictionary<string, object> Metadata { get; }

  public ProjectMetadata (string name, AbsolutePath path, ImmutableDictionary<string, object> metadata)
  {
    Name = name;
    Path = path;
    Metadata = metadata;
  }

  public T GetMetadata<T> (ProjectMetadataProperty<T> property)
  {
    return TryGetMetadata(property, out var result)
        ? result
        : property.HasDefaultValue
            ? property.DefaultValue!
            : throw new InvalidOperationException($"Cannot find the specified project metadata '{property}' on project '{Name}'.");
  }

  public T? GetMetadataOrDefault<T> (ProjectMetadataProperty<T> property)
  {
    return TryGetMetadata(property, out var result)
        ? result
        : property.HasDefaultValue
            ? property.DefaultValue
            : default;
  }

  public bool TryGetMetadata<T> (ProjectMetadataProperty<T> property, [NotNullWhen(true)] out T? result)
  {
    if (Metadata.TryGetValue(property.Name, out var rawResult))
    {
      result = (T) rawResult;
      return true;
    }

    result = default;
    return false;
  }
}