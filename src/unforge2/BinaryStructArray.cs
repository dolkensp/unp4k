using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace unforge
{
    internal class BinaryStructArray : IEnumerable<XmlElement>
    {
        private readonly DataForge _document;
        private readonly DataForgeStructDefinition _struct;
        private readonly string _name;
        private readonly long[] _offsets;
        private readonly XmlElement[] _cache;

        public int Count { get; }

        public BinaryStructArray(DataForge document, DataForgeStructDefinition dataStruct, string name, int count)
        {
            _document = document;
            _struct = dataStruct;
            _name = name;
            Count = count;
            _offsets = new long[count];
            _cache = new XmlElement[count];
            for (int i = 0; i < count; i++)
            {
                _offsets[i] = _document._br.BaseStream.Position;
                _struct.Skip();
            }
        }

        public XmlElement this[int index]
        {
            get
            {
                if (index < 0 || index >= Count)
                    throw new ArgumentOutOfRangeException(nameof(index));
                if (_cache[index] == null)
                {
                    var current = _document._br.BaseStream.Position;
                    _document._br.BaseStream.Seek(_offsets[index], SeekOrigin.Begin);
                    _cache[index] = _struct.Read(_name);
                    _document._br.BaseStream.Seek(current, SeekOrigin.Begin);
                }
                return _cache[index];
            }
            set
            {
                _cache[index] = value;
            }
        }

        public IEnumerator<XmlElement> GetEnumerator()
        {
            for (int i = 0; i < Count; i++)
            {
                yield return this[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
