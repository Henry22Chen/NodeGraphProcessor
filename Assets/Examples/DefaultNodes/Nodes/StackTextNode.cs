using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GraphProcessor;
using System.Linq;

[System.Serializable, NodeMenuItem("Primitives/Text")]
[IsCompatibleWithStack(typeof(NonGenericStackNode))]
public class StackTextNode : BaseNode
{
	[SerializeField]
	public string				text;

	public override string		name => "Text";
}
