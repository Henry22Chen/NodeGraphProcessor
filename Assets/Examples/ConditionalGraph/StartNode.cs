using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GraphProcessor;

namespace NodeGraphProcessor.Examples
{
	[System.Serializable, NodeMenuItem("Conditional/Start")]
	public class StartNode : BaseNode, IConditionalNode
	{
		[Output(name = "Executes")]
		public ConditionalLink		executes;

		public override string		name => "Start";

		public IEnumerable<IConditionalNode>	GetExecutedNodes()
		{
			// Return all the nodes connected to the executes port
			return GetOutputNodes().Where(n => n is IConditionalNode).Select(n => n as IConditionalNode);
		}

		public override FieldInfo[] GetNodeFields() => base.GetNodeFields();
	}
}
