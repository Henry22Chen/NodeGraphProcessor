using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GraphProcessor;

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
    public NodeField<float> output;

    [Input("In"),ShowAsDrawer]
    public NodeField<float> input;

    public override string name => "Float";

    protected override void Process() => output.RuntimeValue = input.RuntimeValue;
}