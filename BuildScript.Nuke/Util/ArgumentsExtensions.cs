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
using System.Reflection;
using System.Reflection.Emit;
using Nuke.Common;
using Nuke.Common.Tooling;

namespace Remotion.BuildScript.Util;

/// <summary>
/// Provides helper extension methods for NUKE's <see cref="Arguments"/> type.
/// </summary>
public static class ArgumentsExtensions
{
  private static Func<Arguments, List<KeyValuePair<string, List<string>>>>? s_getInternalArgumentsListFunction;

  private static List<KeyValuePair<string, List<string>>> GetInternalArgumentsList (Arguments arguments)
  {
    if (s_getInternalArgumentsListFunction != null)
      return s_getInternalArgumentsListFunction(arguments);

    var fieldInfo = arguments.GetType().GetField("_arguments", BindingFlags.Instance | BindingFlags.NonPublic)!.NotNull();

    var dynamicMethod = new DynamicMethod(string.Empty, typeof(List<KeyValuePair<string, List<string>>>), new[] { typeof(Arguments) });
    var ilGenerator = dynamicMethod.GetILGenerator();
    ilGenerator.Emit(OpCodes.Ldarg_0); // load Arguments argument
    ilGenerator.Emit(OpCodes.Ldfld, fieldInfo); // access the _arguments field
    ilGenerator.Emit(OpCodes.Ret); // return the field value

    s_getInternalArgumentsListFunction = dynamicMethod.CreateDelegate<Func<Arguments, List<KeyValuePair<string, List<string>>>>>();
    return s_getInternalArgumentsListFunction(arguments);
  }

  /// <summary>
  /// Inserts an argument value in an <see cref="Arguments"/> instance.
  /// This is a workaround to allow inserting arguments when the command wrapper does not support that argument.
  /// </summary>
  public static Arguments InsertAt (this Arguments arguments, int index, string value)
  {
    var internalArgumentsList = GetInternalArgumentsList(arguments);
    internalArgumentsList.Insert(index, new KeyValuePair<string, List<string>>(value, new List<string> { true.ToString() }));

    return arguments;
  }
}