using System;
using System.Collections.Generic;

namespace unp4k.fs
{
	internal class VirtualDirectoryNode : VirtualNode
	{
		public Dictionary<string, VirtualNode> Children { get; } = new Dictionary<string, VirtualNode>(StringComparer.OrdinalIgnoreCase);

		public override string ToString()
		{
			return $"{this.Path} (Directory[{this.Children.Count}])";
		}
	}
}
