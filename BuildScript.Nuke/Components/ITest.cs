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
using System.Collections.Generic;
using System.Collections.Immutable;
using JetBrains.Annotations;
using Nuke.Common;
using Remotion.BuildScript.Test;
using Remotion.BuildScript.Test.Dimensions;
using Remotion.BuildScript.Util;
using Serilog;

namespace Remotion.BuildScript.Components;

public interface ITest : IBuild, IProjectMetadata, ITestMatrix, ITestParameters
{
  private class ParallelGroupIdentifier
  {
    public static readonly ParallelGroupIdentifier Empty = new(ImmutableHashSet<string>.Empty);

    private int? _hashCode;

    public ImmutableHashSet<string> Values { get; }

    public ParallelGroupIdentifier (ImmutableHashSet<string> values)
    {
      Values = values;
    }

    public override int GetHashCode ()
    {
      // ReSharper disable NonReadonlyMemberInGetHashCode
      if (_hashCode != null)
        return _hashCode.Value;

      // Not ideal hash code combination but we want to ensure that the order of
      // elements does not matter and I am not sure that HashCode.Combine guarantees that
      var hashCode = 0;
      foreach (var element in Values)
      {
        unchecked
        {
          hashCode += element.GetHashCode();
        }
      }

      _hashCode = hashCode;
      return hashCode;
    }

    public override bool Equals (object? obj)
    {
      if (obj is ParallelGroupIdentifier other)
        return Values.SetEquals(other.Values);

      return false;
    }
  }

  [Parameter("Executes only tests that match the specified test filter.")]
  public string TestFilter => TryGetValue(() => TestFilter) ?? "";

  [PublicAPI]
  public Target Test => _ => _
      .DependsOn<IProjectMetadata>()
      .DependsOn<ITestMatrix>()
      .DependsOn<ITestParameters>()
      .DependsOn<IBuild>()
      .Description("Runs all tests")
      .Executes(() =>
      {
        var testGroups = CreateTestGroups();
        using (GroupingBlock.Start("Test execution plan"))
        {
          foreach (var testGroup in testGroups)
            PrintTestGroupsRecursive(testGroup, " ");
        }

        var testContext = CreateTestContext();
        foreach (var testGroup in testGroups)
          testGroup.Execute(testContext);

        var finalMessage = $"Test execution finished. Failed: {testContext.FailedTestCount},"
                           + $" Passed: {testContext.PassedTestCount}, Total: {testContext.TotalTestCount}";
        if (testContext.FailedTestCount > 0)
        {
          Log.Error(finalMessage);
        }
        else
        {
          Log.Information(finalMessage);
        }

        Assert.False(testContext.FatalFailure, "One or more test projects failed fatally.");
      });

  ITestContext CreateTestContext () => new TestContext(this);

  ImmutableArray<TestGroup> CreateTestGroups ()
  {
    // Each project has a list of shared resources (string identifiers) that it has a dependency on
    // Projects that do not depend on the same shared resource can run in parallel.
    // Creating a perfect parallel execution plan is a bit complex so we do a simpler version that should still be pretty good:
    //  1. Group all projects with the same set of shared resources into SerialTestGroups
    //  2. Now we sort all the groups by their number of shared resources low -> high (keeping the 0 shared resource projects separate)
    //  3. Picking the first one, we try to add more groups as long as the shared resources don't overlap, creating ParallelTestGroups
    //     Elements with 0 shared resources can be added to any parallel group. Repeat this step until no more groups are left.
    var serialTestGroups = ImmutableArray.CreateBuilder<TestGroup>();
    var parallelGroupLookup = new Dictionary<ParallelGroupIdentifier, ImmutableArray<TestGroup>.Builder>();
    var testCases = ImmutableArray.CreateBuilder<ITestItem>();

    foreach (var projectMetadata in ProjectMetadata)
    {
      var testConfiguration = projectMetadata.GetMetadataOrDefault(RemotionBuildMetadataProperties.TestConfiguration);
      if (testConfiguration == null)
        continue;

      var testMatrix = testConfiguration.TestMatrix;
      if (testMatrix.IsEmpty)
      {
        Log.Information($"Skipped test project '{projectMetadata.Name}' as there are no test configurations.");
        continue;
      }

      var supportedTargetFrameworks = projectMetadata.GetMetadata(RemotionBuildMetadataProperties.TargetFrameworks);
      foreach (var row in testMatrix.Rows)
      {
        var targetFramework = row.GetDimensionOrDefault<TargetFrameworks>();
        if (targetFramework != null && !supportedTargetFrameworks.Contains(targetFramework.Identifier))
        {
          Log.Warning($"Skipped test configuration '{row}' as the target framework '{targetFramework.Identifier}' is not supported. "
                      + $"Supported are: '{supportedTargetFrameworks}'");
          continue;
        }

        var testCase = new TestCase(
            row.ToString(),
            projectMetadata,
            row,
            testConfiguration.TestExecutionRuntimeFactory,
            testConfiguration.TestExecutionWrappers);
        testCases.Add(testCase);
      }

      if (testCases.Count > 0)
      {
        var projectTestGroup = new SerialTestGroup(projectMetadata.Name, testCases.ToImmutable());
        testCases.Clear();

        // 1) Group according to their parallel identifier
        var parallelGroupIdentifier = GetParallelGroupIdentifier(projectMetadata);
        if (parallelGroupIdentifier == null)
        {
          serialTestGroups.Add(projectTestGroup);
        }
        else
        {
          if (!parallelGroupLookup.TryGetValue(parallelGroupIdentifier, out var parallelGroup))
          {
            parallelGroup = ImmutableArray.CreateBuilder<TestGroup>();
            parallelGroupLookup.Add(parallelGroupIdentifier, parallelGroup);
          }

          parallelGroup.Add(projectTestGroup);
        }
      }
    }



    return testGroups.ToImmutable();
  }

  private static void PrintTestGroupsRecursive (TestGroup testGroup, string prefix)
  {
    Console.WriteLine($"{prefix}> {testGroup}");
    foreach (var testItem in testGroup.TestItems)
    {
      if (testItem is TestGroup childItem)
      {
        PrintTestGroupsRecursive(childItem, prefix + "|");
      }
      else
      {
        Console.WriteLine($"{prefix} - '{testItem.Name}'");
      }
    }
  }

  private static ParallelGroupIdentifier GetParallelGroupIdentifier (ProjectMetadata project)
  {
    project.GetMetadata(RemotionBuildMetadataProperties.)
  }
}