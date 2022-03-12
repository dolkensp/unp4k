using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using unforge;

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

namespace System.Xml
{
    internal static class XmlExtensions
    {
        private static readonly Regex cleanString = new("[^a-zA-Z0-9.]");

        internal static XmlElement Rename(this XmlElement element, string name)
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

        internal static string GetPath(this XmlNode target)
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
}

namespace System.IO
{
    internal static class IOExtensions
    {
        internal static string ReadPString(this BinaryReader binaryReader, StringSizeEnum byteLength = StringSizeEnum.Int32)
        {
            var stringLength = byteLength switch
            {
                StringSizeEnum.Int8 => binaryReader.ReadByte(),
                StringSizeEnum.Int16 => binaryReader.ReadInt16(),
                StringSizeEnum.Int32 => binaryReader.ReadInt32(),
                _ => throw new NotSupportedException("Only Int8, Int16, and int string sizes are supported"),
            };
            // If there is actually a string to read
            if (stringLength > 0) return new string(binaryReader.ReadChars(stringLength));
            return null;
        }

        internal static string ReadCString(this BinaryReader binaryReader)
        {
            int stringLength = 0;
            while (binaryReader.BaseStream.Position < binaryReader.BaseStream.Length && binaryReader.ReadChar() != 0) stringLength++;
            long nul = binaryReader.BaseStream.Position;
            binaryReader.BaseStream.Seek(0 - stringLength - 1, SeekOrigin.Current);
            Char[] chars = binaryReader.ReadChars(stringLength + 1);
            binaryReader.BaseStream.Seek(nul, SeekOrigin.Begin);
            // Why is this necessary?
            if (stringLength > chars.Length) stringLength = chars.Length;
            // If there is actually a string to read
            if (stringLength > 0) return new string(chars, 0, stringLength).Replace("\u0000", "");
            return null;
        }

        internal static string ReadFString(this BinaryReader binaryReader, int stringLength)
        {
            char[] chars = binaryReader.ReadChars(stringLength);
            for (int i = 0; i < stringLength; i++) if (chars[i] == 0) return new string(chars, 0, i);
            return new string(chars);
        }

        public static byte[] ReadAllBytes(this Stream stream)
        {
            using MemoryStream ms = new();
            long oldPosition = stream.Position;
            stream.Position = 0;
            stream.CopyTo(ms);
            stream.Position = oldPosition;
            return ms.ToArray();
        }

        public static Guid? ReadGuid(this BinaryReader reader, Boolean nullable = true)
        {
            bool isNull = nullable && reader.ReadInt32() == -1;
            short c = reader.ReadInt16();
            short b = reader.ReadInt16();
            int a = reader.ReadInt32();
            byte k = reader.ReadByte();
            byte j = reader.ReadByte();
            byte i = reader.ReadByte();
            byte h = reader.ReadByte();
            byte g = reader.ReadByte();
            byte f = reader.ReadByte();
            byte e = reader.ReadByte();
            byte d = reader.ReadByte();
            if (isNull) return null;
            return new Guid(a, b, c, d, e, f, g, h, i, j, k);
        }
    }
}