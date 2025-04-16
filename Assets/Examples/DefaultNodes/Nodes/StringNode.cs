using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GraphProcessor;
using System.Linq;

[System.Serializable, NodeMenuItem("String")]
public class StringNode : BaseNode
{
	[Output(name = "Out"), SerializeField]
	public string				output;

	public override string		name => "String";

	protected override bool TryGetOutputValue<T>(int index, out T value, int edgeIndex)
	{
		return TryConvertValue(ref output, out value);
	}
}
