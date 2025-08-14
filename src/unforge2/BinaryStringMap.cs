using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace unforge
{
    internal class BinaryStringMap
    {
        private readonly BinaryReader _br;
        private readonly long _start;
        private readonly long _end;
        private readonly Dictionary<uint, string> _cache = new Dictionary<uint, string>();

        public BinaryStringMap(BinaryReader br, uint length)
        {
            _br = br;
            _start = br.BaseStream.Position;
            _end = _start + length;
            br.BaseStream.Seek(length, SeekOrigin.Current);
        }

        public string this[uint offset]
        {
            get
            {
                if (!_cache.TryGetValue(offset, out var value))
                {
                    if (!ContainsKey(offset)) return string.Empty;
                    var current = _br.BaseStream.Position;
                    _br.BaseStream.Seek(_start + offset, SeekOrigin.Begin);
                    var bytes = new List<byte>();
                    byte b;
                    while (_br.BaseStream.Position < _end && (b = _br.ReadByte()) != 0)
                        bytes.Add(b);
                    value = Encoding.UTF8.GetString(bytes.ToArray());
                    _cache[offset] = value;
                    _br.BaseStream.Seek(current, SeekOrigin.Begin);
                }
                return value;
            }
        }

        public bool ContainsKey(uint offset)
        {
            return offset < _end - _start;
        }
    }
}
