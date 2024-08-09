using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GraphProcessor;
using System.Linq;

[System.Serializable, NodeMenuItem("Custom/TypeSwitchNode")]
public class TypeSwitchNode : BaseNode
{
	[Input]
    public string               input;

	[SerializeField]
	public bool					toggleType;

	public override string		name => "TypeSwitchNode";

	protected override bool hasCustomInputs => true;

    protected override IEnumerable<PortData> GetCustomInputPorts()
    {
		yield return BuildCustomPort(nameof(input), (toggleType) ? typeof(float) : typeof(string), "In");
    }
	
	protected override void Process()
	{
		if (toggleType)
		{
			float val = 0;
			if (TryReadInputValue(0, ref val))
				input = val.ToString();
		}
		else
			TryReadInputValue(0, ref input);
		Debug.Log("Input: " + input);
	}
}
