using System;
using System.IO;

namespace unforge
{
    internal class BinaryArray<T> where T : _DataForgeSerializable
    {
        private readonly DataForge _document;
        private readonly long _start;
        private readonly int _elementSize;
        public int Count { get; }

        public BinaryArray(DataForge document, int count)
        {
            _document = document;
            Count = count;
            _elementSize = GetElementSize(typeof(T));
            _start = _document._br.BaseStream.Position;
            _document._br.BaseStream.Seek((long)_elementSize * count, SeekOrigin.Current);
        }

        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= Count)
                    throw new ArgumentOutOfRangeException(nameof(index));
                var current = _document._br.BaseStream.Position;
                _document._br.BaseStream.Seek(_start + index * _elementSize, SeekOrigin.Begin);
                var value = (T)Activator.CreateInstance(typeof(T), _document);
                _document._br.BaseStream.Seek(current, SeekOrigin.Begin);
                return value;
            }
        }

        private static int GetElementSize(Type t)
        {
            if (t == typeof(DataForgeBoolean) || t == typeof(DataForgeInt8) || t == typeof(DataForgeUInt8)) return 1;
            if (t == typeof(DataForgeInt16) || t == typeof(DataForgeUInt16)) return 2;
            if (t == typeof(DataForgeInt32) || t == typeof(DataForgeUInt32) || t == typeof(DataForgeSingle) ||
                t == typeof(DataForgeStringLookup) || t == typeof(DataForgeLocale) || t == typeof(DataForgeEnum)) return 4;
            if (t == typeof(DataForgeInt64) || t == typeof(DataForgeUInt64) || t == typeof(DataForgeDouble) ||
                t == typeof(DataForgePointer)) return 8;
            if (t == typeof(DataForgeGuid)) return 16;
            if (t == typeof(DataForgeReference)) return 20;
            throw new NotSupportedException($"Unknown element size for {t}");
        }
    }
}
