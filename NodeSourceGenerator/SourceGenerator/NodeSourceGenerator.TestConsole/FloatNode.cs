using GraphProcessor;
using UnityEngine;

// write by yourself
[PartialNode]
public partial class FloatNode : BaseNode
{
    [Output("Out"), Tooltip("Float output port"), ShowInInspector]
    public float output;

    [Input("In", allowMultiple = true), ShowAsDrawer]
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

// generated
// public partial class FloatNode
// {
//     protected override void PrepareInputsGenerated(BaseNode prevNode = null)
//     {
//         if (prevNode == null)
//         {
//             TryReadInputValue(0, ref input);
//         }
//         else
//         {
//             TryReadInputValue(0, ref input, prevNode);
//         }
//     }
//
//     protected override void InitializeFieldData()
//     {
//         _needsInspector = true; // ShowInInspectorAttribute != null
//         nodeFields["input"] = new NodeFieldInformation(this, "input", "In", true, true, null, false);
//         nodeFields["output"] =
//             new NodeFieldInformation(this, "output", "Out", false, false, "Float output port", false);
//     }
// }