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
	
	[Input(name = "In"), ShowAsDrawer]
	public Vector4				input;

    public bool testValue;

	public override string		name => "Vector";
    protected override bool hasCustomInputs => true;
    protected override bool hasCustomOutputs => true;

    protected override IEnumerable<PortData> GetCustomInputPorts()
    {
        yield return BuildCustomPort(nameof(input), typeof(Vector4), "In");
        yield return BuildCustomPort(nameof(input), typeof(Vector4), "x", false);
        yield return BuildCustomPort(nameof(input), typeof(Vector4), "y", false);
        yield return BuildCustomPort(nameof(input), typeof(Vector4), "z", false);
        yield return BuildCustomPort(nameof(input), typeof(Vector4), "w", false);
    }

    protected override IEnumerable<PortData> GetCustomOutputPorts()
    {
        yield return BuildCustomPort(nameof(output), typeof(Vector4), "Out", true, true);
        yield return BuildCustomPort(nameof(output), typeof(Vector4), "x", false, true);
        yield return BuildCustomPort(nameof(output), typeof(Vector4), "y", false, true);
        yield return BuildCustomPort(nameof(output), typeof(Vector4), "z", false, true);
        yield return BuildCustomPort(nameof(output), typeof(Vector4), "w", false, true);
    }
    protected override void Process()
	{
        TryReadInputValue(0, ref input);
        TryReadInputValue(1, ref input.x);
        TryReadInputValue(2, ref input.y);
        TryReadInputValue(3, ref input.z);
        TryReadInputValue(4, ref input.w);
        output = input;
	}
    protected override bool TryGetOutputValue<T>(int index, out T value, int edgeIdx)
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
