using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GraphProcessor;
using System;

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

    protected override void Process() => output = input;

    protected override void OnReadInput(int index)
    {
        ReadValueForField(index, ref input);
    }
    protected override IEnumerable<Delegate> InitializeOutputReaders()
    {
        yield return CreateOutputReader(()=>output);
    }
}