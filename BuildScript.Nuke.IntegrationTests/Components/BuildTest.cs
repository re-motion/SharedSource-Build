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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Nuke.Common.IO;
using NUnit.Framework;

namespace BuildScript.Nuke.IntegrationTests.Components;

[TestFixture]
public class BuildTest : IntegrationTestBase
{
  [Flags]
  private enum Configurations
  {
    Debug = 1,
    Release = 2,
    DebugAndRelease = Debug | Release
  }

  [Test]
  public void Build_UsesDebugAndReleaseByDefault ()
  {
    var relevantDirectories = GetProjectOutputDirectories(Configurations.DebugAndRelease).ToArray();
    foreach (var relevantDirectory in relevantDirectories)
      Assert.That(relevantDirectory, Does.Not.Exist);

    RunBuildCmd("Build");

    foreach (var relevantDirectory in relevantDirectories)
      Assert.That(relevantDirectory, Does.Exist);
  }

  [Test]
  public void Build_ExplicitDebugConfiguration ()
  {
    var relevantDirectories = GetProjectOutputDirectories(Configurations.Debug).ToArray();
    var irrelevantDirectories = GetProjectOutputDirectories(Configurations.Release).ToArray();
    foreach (var relevantDirectory in relevantDirectories.Concat(irrelevantDirectories))
      Assert.That(relevantDirectory, Does.Not.Exist);

    RunBuildCmd("Build --Configurations Debug");

    foreach (var relevantDirectory in relevantDirectories)
      Assert.That(relevantDirectory, Does.Exist);
    foreach (var irrelevantDirectory in irrelevantDirectories)
      Assert.That(irrelevantDirectory, Does.Not.Exist);
  }

  [Test]
  public void Build_ExplicitReleaseConfiguration ()
  {
    var relevantDirectories = GetProjectOutputDirectories(Configurations.Release).ToArray();
    var irrelevantDirectories = GetProjectOutputDirectories(Configurations.Debug).ToArray();
    foreach (var relevantDirectory in relevantDirectories.Concat(irrelevantDirectories))
      Assert.That(relevantDirectory, Does.Not.Exist);

    RunBuildCmd("Build --Configurations Release");

    foreach (var relevantDirectory in relevantDirectories)
      Assert.That(relevantDirectory, Does.Exist);
    foreach (var irrelevantDirectory in irrelevantDirectories)
      Assert.That(irrelevantDirectory, Does.Not.Exist);
  }

  [Test]
  [TestCase("Debug")]
  [TestCase("Release")]
  public void Build_SetsVersionAndSignsAssembly (string configuration)
  {
    RunBuildCmd("Build");

    var assemblyPath = TestSolutionDirectory / "SdkTestProject" / "bin" / configuration / "netstandard2.1" / "SdkTestProject.dll";
    Assert.That(assemblyPath.ToString(), Does.Exist);

    var assemblyName = AssemblyName.GetAssemblyName(assemblyPath);
    Assert.That(
        assemblyName.FullName,
        Is.EqualTo("SdkTestProject, Version=3.0.0.0, Culture=neutral, PublicKeyToken=44bc870013f0fe6f"));

    AssemblyLoadContext? assemblyLoadContext = null;
    WeakReference? assemblyWeakRef = null;
    try
    {
      assemblyLoadContext = new AssemblyLoadContext(null, isCollectible: true);
      AssertAssemblyAttributesInOtherLoadContext(
          assemblyLoadContext,
          assemblyPath,
          configuration,
          out assemblyWeakRef);
    }
    finally
    {
      assemblyLoadContext?.Unload();

      for (var i = 0; i < 10 && (assemblyWeakRef?.IsAlive ?? false); i++)
      {
        GC.Collect();
        GC.WaitForPendingFinalizers();
      }

      if (assemblyWeakRef != null && assemblyWeakRef.IsAlive)
        Assert.Fail("Could not unload test assembly.");
    }
  }

  private static void AssertAssemblyAttributesInOtherLoadContext (
      AssemblyLoadContext assemblyLoadContext,
      string assemblyPath,
      string configuration,
      out WeakReference? assemblyWeakRef)
  {
    // Do not load assembly directly to prevent file locking
    var memoryStream = new MemoryStream(File.ReadAllBytes(assemblyPath), writable: false);
    var assembly = assemblyLoadContext.LoadFromStream(memoryStream);
    assemblyWeakRef = new WeakReference(assembly, trackResurrection: true);

    Assert.That(
        assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version,
        Is.EqualTo("3.0.0.24001"));
    Assert.That(
        assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,
        Is.EqualTo($"3.0.0-x.1.0+{configuration}.Commit-eaeaeaeaeaeaeaeaeaeaeaeaeaeaeaeaeaeaeaea"));
  }

  private IEnumerable<string> GetProjectOutputDirectories (Configurations configurations)
  {
    foreach (var project in TestSolutionModel.Projects)
    {
      foreach (var targetFramework in project.TargetFrameworks)
      {
        if ((configurations & Configurations.Debug) != 0)
          yield return project.BinFolder / "Debug" / targetFramework;

        if ((configurations & Configurations.Release) != 0)
          yield return project.BinFolder / "Release" / targetFramework;
      }
    }
  }
}