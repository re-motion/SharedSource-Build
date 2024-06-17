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
using Nuke.Common.IO;

namespace Remotion.BuildScript;

public class ProjectBuilder
{
  public string Name { get; }

  public AbsolutePath Path { get; }

  public ImmutableDictionary<string, object>.Builder Metadata { get; } = ImmutableDictionary.CreateBuilder<string, object>();

  public ProjectBuilder (string name, AbsolutePath path)
  {
    Name = name;
    Path = path;
  }

  public ProjectBuilder SetMetadata (string key, object value)
  {
    Metadata[key] = value;
    return this;
  }

  public ProjectMetadata Build ()
  {
    return new ProjectMetadata(
        Name,
        Path,
        Metadata.ToImmutableDictionary());
  }
}