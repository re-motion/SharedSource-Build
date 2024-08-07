﻿// Copyright (c) rubicon IT GmbH, www.rubicon.eu
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

namespace Remotion.BuildScript.Test;

public abstract class TestDimension
    : IEquatable<TestDimension>
{
  public string Name { get; }

  public string Value { get; }

  protected TestDimension (string name, string value)
  {
    Name = name;
    Value = value;
  }

  public bool Equals (TestDimension? other)
  {
    if (ReferenceEquals(null, other))
      return false;
    if (ReferenceEquals(this, other))
      return true;

    return Name == other.Name
        && Value == other.Value;
  }

  public override bool Equals (object? obj)
  {
    if (ReferenceEquals(null, obj))
      return false;
    if (ReferenceEquals(this, obj))
      return true;
    if (obj is not TestDimension otherTestDimension)
      return false;

    return Equals(otherTestDimension);
  }

  public override int GetHashCode () => Value.GetHashCode();

  public override string ToString () => Value;

  public static bool operator == (TestDimension? left, TestDimension? right) => Equals(left, right);

  public static bool operator != (TestDimension? left, TestDimension? right) => !Equals(left, right);
}