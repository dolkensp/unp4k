using System;
using System.IO;

namespace unforge
{
    internal class BinaryDataMappingArray
    {
        private readonly DataForge _document;
        private readonly long _start;
        private readonly int _elementSize;
        public int Count { get; }

        public BinaryDataMappingArray(DataForge document, int count)
        {
            _document = document;
            Count = count;
            _elementSize = (document.FileVersion >= 5) ? 8 : 4;
            _start = _document._br.BaseStream.Position;
            _document._br.BaseStream.Seek((long)_elementSize * count, SeekOrigin.Current);
        }

        public DataForgeDataMapping this[int index]
        {
            get
            {
                if (index < 0 || index >= Count)
                    throw new ArgumentOutOfRangeException(nameof(index));
                var current = _document._br.BaseStream.Position;
                _document._br.BaseStream.Seek(_start + index * _elementSize, SeekOrigin.Begin);
                var value = new DataForgeDataMapping(_document);
                _document._br.BaseStream.Seek(current, SeekOrigin.Begin);
                return value;
            }
        }
    }
}
