using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GraphProcessor;
using System.Linq;

[System.Serializable, NodeMenuItem("Custom/MultiAdd")]
public class MultiAddNode : BaseNode
{
	[Input]
	public IEnumerable< float >	inputs = null;

	[Output]
	public float				output;

	public override string		name => "Add";

	protected override bool hasCustomInputs => true;

    protected override void Process()
	{
		var cnt = inputPorts[0].GetEdges().Count;
		output = 0;
		
		for(int i = 0; i < cnt; i++)
		{
			float val =0;
			if (TryReadInputValue(0, ref val, i))
				output += val;
		}
	}

    protected override bool TryGetOutputValue<T>(int index, out T value, int edgeIndex)
    {
		return TryConvertValue(ref output, out value);  
    }

    protected override IEnumerable<PortData> GetCustomInputPorts()
    {
		yield return BuildCustomPort(null, typeof(float), "In", allowMultiple: true);
    }
}
