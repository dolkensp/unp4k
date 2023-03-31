using System.Text.RegularExpressions;
/// <summary>
/// The MIT License (MIT)
/// 
/// Copyright (c) 2008 Peter Dolkens
/// 
/// Permission is hereby granted, free of charge, to any person obtaining a copy
/// of this software and associated documentation files (the "Software"), to deal
/// in the Software without restriction, including without limitation the rights
/// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
/// copies of the Software, and to permit persons to whom the Software is
/// furnished to do so, subject to the following conditions:
/// 
/// The above copyright notice and this permission notice shall be included in
/// all copies or substantial portions of the Software.
/// 
/// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
/// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
/// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
/// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
/// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
/// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
/// THE SOFTWARE.
/// </summary>
namespace System.Xml;

public static class XmlExtensions
{
    private static readonly Regex cleanString = new("[^a-zA-Z0-9.]");

    public static XmlElement Rename(this XmlElement element, string name)
    {
        XmlElement buffer = element.OwnerDocument.CreateElement(cleanString.Replace(name, "_"));
        while (element.ChildNodes.Count > 0) buffer.AppendChild(element.ChildNodes[0]);
        while (element.Attributes.Count > 0)
        {
            XmlAttribute attribute = element.Attributes[0];
            buffer.Attributes.Append(attribute);
        }
        return buffer;
    }

    public static string GetPath(this XmlNode target)
    {
        List<KeyValuePair<string, int?>> path = new();
        while (target.ParentNode != null)
        {
            XmlNodeList? siblings = target.ParentNode.SelectNodes(target.Name);
            if (siblings.Count > 1)
            {
                int siblingIndex = 0;
                foreach (XmlNode sibling in siblings)
                {
                    if (sibling == target) path.Add(new KeyValuePair<string, int?>(target.ParentNode.Name, siblingIndex));
                    siblingIndex++;
                }
            }
            else path.Add(new KeyValuePair<string, int?>(target.ParentNode.Name, null));
            target = target.ParentNode;
        }
        path.Reverse();
        return string.Join(".", path.Skip(3).Select(p => p.Value.HasValue ? string.Format("{0}[{1}]", p.Key, p.Value) : p.Key));
    }
}