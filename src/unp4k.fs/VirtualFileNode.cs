using System;
using System.ComponentModel.DataAnnotations;

namespace unp4k.fs
{
	internal class VirtualFileNode : VirtualNode
	{
		public Int64? Length { get; set; }

		[Required]
		public Func<Byte[]> GetContent { get; set; }

		public override string ToString()
		{
			return $"{this.Path} (File)";
		}
	}
}
