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
    public float output;

    [Input("In")]
    public float input;

    public override string name => "Float";

    protected override void Process() => output = input;
}

[NodeMenuItem("Primitives/Float2")]
public class Float2Node : BaseNode
{
    [Output("Out")]
    public float output;

    [Input("In"), ShowAsDrawer]
    public float input;

    public override string name => "Float";

    protected override void Process()
    {
        if (!TryReadInputValue(0, ref input))
        {
            TryReadInputValue<Vector4, float>(0, ref input);
        }

        output = input;
    }

    protected override bool TryGetOutputValue<T>(int index, out T value, int edgeIdx)
    {
        return TryConvertValue(ref this.output, out value);
    }
}

[NodeMenuItem("Primitives/Float3")]
public partial class Float3Node : BaseNode
{
    [Output("Out")]
    public float output;

    [Input("In"), ShowAsDrawer]
    public float input;

    public override string name => "Float";

    protected override void Process()
    {
        output = input;
    }

    protected override bool TryGetOutputValue<T>(int index, out T value, int edgeIdx)
    {
        return TryConvertValue(ref this.output, out value);
    }
}

public partial class Float3Node
{
    protected override void PrepareInputsGenerated(BaseNode prevNode = null)
    {
        if (!TryReadInputValue(0, ref input))
        {
            TryReadInputValue<Vector4, float>(0, ref input);
        }
    }
}

[NodeMenuItem("Primitives/Float4"), PartialNode]
public partial class Float4Node : BaseNode
{
    [Output("Out"), Tooltip("Float output port")]
    public float output;

    [Input("In", allowMultiple = true), ShowAsDrawer]
    public float input;
    
    [Input("In2", allowMultiple = true), ShowAsDrawer]
    public float input2;

    public override string name => "Float";

    protected override bool fieldsSortedDescending => false;

    protected override void Process()
    {
        output = Mathf.Max(input, input2);
        Debug.Log($"Float4 processed {output}");
    }

    protected override bool TryGetOutputValue<T>(int index, out T value, int edgeIdx)
    {
        return TryConvertValue(ref this.output, out value);
    }
}

// public partial class Float4Node
// {
//     protected override void PrepareInputsGenerated(BaseNode prevNode = null)
//     {
//         int index = fieldsSortedDescending ? -1 : 2;
//         
//         TryReadInputValue(fieldsSortedDescending ? ++index : --index, ref input2);
//         TryReadInputValue(fieldsSortedDescending ? ++index : --index, ref input);
//     }
//
//     protected override void InitializeFieldData()
//     {
//         _needsInspector = false;
//         int index = fieldsSortedDescending ? -1 : 3;
//         nodeFields["input2"] = new NodeFieldInformation(this, "input2", "In2", true, true, null, false, fieldsSortedDescending ? ++index : --index);
//         nodeFields["input"] = new NodeFieldInformation(this, "input", "In", true, true, null, false, fieldsSortedDescending ? ++index : --index);
//         nodeFields["output"] = new NodeFieldInformation(this, "output", "Out", false, false, "Float output port", false, fieldsSortedDescending ? ++index : --index);
//     }
// }