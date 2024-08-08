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

    public override bool propagateValues => false;

    protected override bool TryGetCustomPorts()
    {
        AddPort(true, nameof(input), typeof(Vector4), "In");
        AddPort(true, nameof(input), typeof(float), "x");
        AddPort(true, nameof(input), typeof(float), "y");
        AddPort(true, nameof(input), typeof(float), "z");
        AddPort(true, nameof(input), typeof(float), "w");
        AddPort(false, nameof(output), typeof(Vector4), "Out");
        AddPort(false, nameof(output), typeof(float), "x");
        AddPort(false, nameof(output), typeof(float), "y");
        AddPort(false, nameof(output), typeof(float), "z");
        AddPort(false, nameof(output), typeof(float), "w");
        return true;
    }
    protected override void Process()
	{
		output = input;
	}
    protected override bool TryGetOutputValue<T>(int index, out T value)
    {
        switch (index)
        {
            case 0:
                return TryConvertValue(ref output, out value);
            case 1:
                return TryConvertValue(ref output.x, out value);
            case 2:
                return TryConvertValue(ref output.y, out value);
            case 3:
                return TryConvertValue(ref output.z, out value);
            case 4:
                return TryConvertValue(ref output.w, out value);
        }
        value = default;
        return false;
    }
}
