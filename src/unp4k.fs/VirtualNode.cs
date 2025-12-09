using System;
using System.ComponentModel.DataAnnotations;

namespace unp4k.fs
{
	internal class VirtualNode
	{
		[Required]
		public String Path { get; set; }
	}
}
