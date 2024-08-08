using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GraphProcessor;
using System;
using System.Reflection;

[System.Serializable, NodeMenuItem("Primitives/Float")]
public class FloatNode : BaseNode
{
    [Output("Out")]
	public float		output;
	
    [Input("In")]
	public float		input;

	public override string name => "Float";

	protected override void Process() => output = input;
}
[NodeMenuItem("Primitives/Float2")]
public class Float2Node : BaseNode
{
    [Output("Out")]
    public float output;

    [Input("In"),ShowAsDrawer]
    public float input;

    public override string name => "Float";

    public override bool propagateValues => false;

    protected override void Process()
    {
        if (!TryReadInputValue(0, ref input))
        {
            TryReadInputValue<Vector4, float>(0, ref input);
        }
        output = input;
    }

    protected override bool TryGetOutputValue<T>(int index, out T value)
    {
        return TryConvertValue(ref this.output, out value);
    }
}