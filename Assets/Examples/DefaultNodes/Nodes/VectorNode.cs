using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GraphProcessor;
using System.Linq;
using System;

[System.Serializable, NodeMenuItem("Custom/Vector")]
public class VectorNode : BaseNode
{
	[Output(name = "Out")]
	public Vector4				output;
	
	[Input(name = "In"), SerializeField]
	public Vector4				input;

	public override string		name => "Vector";

	protected override void Process()
	{
		output = input;
	}
    protected override bool TryGetOutputValue<T>(int index, out T value)
    {
        return TryConvertValue(ref output, out value);
    }
}
