using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GraphProcessor;
using System.Linq;
using NodeGraphProcessor.Examples;

[System.Serializable, NodeMenuItem("Primitives/Text")]
[IsCompatibleWithStack(typeof(NonGenericStackNode))]
public class StackTextNode : BaseNode
{
	[SerializeField]
	public string				text;
    [Input, ShowAsDrawer]
    public float Param;
    [Output]
    public ConditionalLink True;
    [Output]
    public ConditionalLink False;

    public override string		name => "Text";

    protected override bool hasCustomInputs => true;

    protected override IEnumerable<PortData> GetCustomInputPorts()
    {
        yield return BuildCustomPort(nameof(Param), typeof(float), "In 1");
        yield return BuildCustomPort(null, typeof(ConditionalLink), "In 2");
    }
}
