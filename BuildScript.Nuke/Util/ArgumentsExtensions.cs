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
using Nuke.Common.Utilities;

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
  /// Converts the <see cref="IArguments"/> to <see cref="Arguments"/>.
  /// </summary>
  public static Arguments AsArguments (this IArguments arguments)
  {
    return (Arguments) arguments;
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

  /// <summary>
  /// Renders the specified <see cref="Arguments"/> as an array.
  /// This is useful, for example, when using the <see cref="Arguments"/> to start a process where we need an array of strings.
  /// </summary>
  public static string[] RenderAsArray (this Arguments arguments)
  {
    var internalArgumentsList = GetInternalArgumentsList(arguments);

    var argumentList = new List<string>();
    foreach (var argumentPair in internalArgumentsList)
    {
      // The key is either '--option {0}', '{0}', or 'argument'/'--rm'
      // but we need to convert this to an array, so we split before replacing the value
      var nameParts = argumentPair.Key.Split(' ', StringSplitOptions.RemoveEmptyEntries);

      foreach (var argument in argumentPair.Value)
      {
        foreach (var namePart in nameParts)
        {
          var value = namePart == "{0}"
              ? argument.TrimMatchingDoubleQuotes() // Arguments will add double quotes if necessary, which need to be removed for the array
              : namePart;

          argumentList.Add(value);
        }
      }
    }

    return argumentList.ToArray();
  }
}