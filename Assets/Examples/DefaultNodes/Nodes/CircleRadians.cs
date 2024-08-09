using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GraphProcessor;
using System.Linq;

[System.Serializable, NodeMenuItem("Custom/CircleRadians")]
public class CircleRadians : BaseNode
{
	[Output(name = "In")]
    public List< float >		outputRadians;

	public override string		name => "CircleRadians";

    protected override bool TryGetOutputValue<T>(int index, out T value, int edgeIndex)
    {
		var val = outputRadians[edgeIndex];
		return TryConvertValue(ref val, out value);
    }
}
