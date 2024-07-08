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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Nuke.Common.IO;

namespace Remotion.BuildScript;

public class AppConfig
{
  public static AppConfig FromText (string text, params (string prefix, string uri)[] namespaces)
  {
    var xDocument = XDocument.Parse(text);
    return new AppConfig(xDocument, CreateXmlNamespaceManager(xDocument, namespaces));
  }

  public static AppConfig Read (AbsolutePath path, params (string prefix, string uri)[] namespaces)
  {
    var xDocument = XDocument.Load(path);
    return new AppConfig(xDocument, CreateXmlNamespaceManager(xDocument, namespaces));
  }

  private static XmlNamespaceManager? CreateXmlNamespaceManager (XDocument xDocument, (string prefix, string uri)[] namespaces)
  {
    XmlNamespaceManager? xmlNamespaceManager = null;

    if (namespaces?.Length > 0)
    {
      var reader = xDocument.CreateReader();
      if (reader.NameTable != null)
      {
        xmlNamespaceManager = new XmlNamespaceManager(reader.NameTable);
        foreach (var (prefix, uri) in namespaces)
          xmlNamespaceManager.AddNamespace(prefix, uri);
      }
    }

    return xmlNamespaceManager;
  }

  private readonly XDocument _xDocument;
  private readonly XmlNamespaceManager? _xmlNamespaceManager;

  public AppConfig (XDocument xDocument, XmlNamespaceManager? xmlNamespaceManager)
  {
    _xDocument = xDocument;
    _xmlNamespaceManager = xmlNamespaceManager;
  }

  public string? GetAppSetting (string key)
  {
    var attribute = (XAttribute?) EvaluateXPathSingleOrDefault($"/configuration/appSettings/add[@key='{key}']/@value");

    return attribute?.Value;
  }

  public void SetAppSetting (string key, string value)
  {
    var attribute = (XAttribute?) EvaluateXPathSingleOrDefault($"/configuration/appSettings/add[@key='{key}']/@value");

    if (attribute != null)
    {
      attribute.Value = value;
    }
    else
    {
      var appSettingsElement = (XElement) EvaluateXPathSingle("/configuration/appSettings");

      var newEntry = new XElement("add");
      newEntry.SetAttributeValue("key", key);
      newEntry.SetAttributeValue("value", value);
      appSettingsElement.Add(newEntry);
    }
  }

  public string? GetAttribute (string xPath, string attribute)
  {
    var element = (XElement?) EvaluateXPathSingleOrDefault(xPath);
    return element?.Attribute(attribute)?.Value;
  }

  public void SetOrAddAttribute (string xPath, string attribute, string value)
  {
    var xElement = (XElement) EvaluateXPathSingle(xPath);

    var xAttribute = xElement.Attribute(attribute);
    if (xAttribute != null)
    {
      xAttribute.Value = value;
    }
    else
    {
      var newXAttribute = new XAttribute(attribute, value);
      xElement.Add(newXAttribute);
    }
  }

  public void WriteToFile (AbsolutePath absolutePath)
  {
    var writerSettings = new XmlWriterSettings { OmitXmlDeclaration = _xDocument.Declaration == null, Encoding = Encoding.UTF8 };
    using var xmlWriter = XmlWriter.Create(absolutePath, writerSettings);
    _xDocument.Save(xmlWriter);
  }

  private XObject? EvaluateXPathSingleOrDefault (string xPath)
  {
    var results = EvaluateXPath(xPath).ToList();
    if (results.Count > 1)
      throw new InvalidOperationException($"XPath expression '{xPath}' returned {results.Count} elements but 0 or 1 were expected.");

    return results.Count > 0
        ? results[0]
        : null;
  }

  private XObject EvaluateXPathSingle (string xPath)
  {
    var results = EvaluateXPath(xPath).ToList();
    if (results.Count != 1)
      throw new InvalidOperationException($"XPath expression '{xPath}' returned {results.Count} elements but exactly 1 was expected.");

    return results[0];
  }

  private IEnumerable<XObject> EvaluateXPath (string xPath)
  {
    return ((IEnumerable) _xDocument.XPathEvaluate(xPath, _xmlNamespaceManager)).Cast<XObject>().ToList();
  }
}