using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GraphProcessor;
using System.Linq;
using System;

[System.Serializable, NodeMenuItem("Utils/Relay")]
public class RelayNode : BaseNode
{
	const int packIdentifier = 122112;

	[HideInInspector]
	public struct PackedRelayData
	{
		public List<object>	values;
		public List<string>	names;
		public List<Type>	types;
	}

	[Input(name = "In")]
    public PackedRelayData	input;

	[Output(name = "Out")]
	public PackedRelayData	output;

	public bool		unpackOutput = false;
	public bool		packInput = false;
	public int		inputEdgeCount = 0;
	[System.NonSerialized]
	int				outputIndex = 0;

	SerializableType inputType = new SerializableType(typeof(object));

	const int		k_MaxPortSize = 14;

	protected override bool hasCustomInputs => true;
	protected override bool hasCustomOutputs => true;

    protected override void Process()
	{
		outputIndex = 0;
		output = input;
	}

	public override string layoutStyle => "GraphProcessorStyles/RelayNode";

	protected override IEnumerable<PortData> GetCustomInputPorts()
	{
		if (inputPorts.Count != 0)
		{
			var edges = inputPorts[0].GetEdges();
			// When the node is initialized, the input ports is empty because it's this function that generate the ports
			int sizeInPixel = 0;
			// Add the size of all input edges:
			sizeInPixel = edges.Sum(e => Mathf.Max(0, e.outputPort.portData.sizeInPixel - 8));

			if (edges.Count == 1 && !packInput)
				inputType.type = edges[0].outputPort.portData.displayType;
			else
				inputType.type = typeof(object);
			yield return BuildCustomPort(null, inputType.type, "", false, sizeInPixel: Mathf.Min(k_MaxPortSize, sizeInPixel + 8));
		}
		else
		{
            inputType.type = typeof(object);
			yield return BuildCustomPort(null, inputType.type, "", allowMultiple: true);
        }
	}

    protected override IEnumerable<PortData> GetCustomOutputPorts()
    {
        if (inputPorts.Count == 0)
        {
            yield return BuildCustomPort(null, inputType.type, "", allowMultiple: true);
            yield break;
        }

        var inputPortEdges = inputPorts[0].GetEdges();
        var underlyingPortData = GetUnderlyingPortDataList();
        if (unpackOutput && inputPortEdges.Count == 1)
        {
            yield return BuildCustomPort(null, inputType.type, "Pack", allowMultiple: true, sizeInPixel: Mathf.Min(k_MaxPortSize, Mathf.Max(underlyingPortData.Count, 1) + 7));

            // We still keep the packed data as output when unpacking just in case we want to continue the relay after unpacking
            for (int i = 0; i < underlyingPortData.Count; i++)
            {
                yield return BuildCustomPort(null, underlyingPortData?[i].type ?? typeof(object), underlyingPortData?[i].name ?? "", allowMultiple: true);
            }
        }
        else
        {
            yield return BuildCustomPort(null, inputType.type, "", allowMultiple: true, sizeInPixel: Mathf.Min(k_MaxPortSize, Mathf.Max(underlyingPortData.Count, 1) + 7));
        }
    }

    protected override bool TryGetOutputValue<T>(int index, out T value, int edgeIdx)
    {
		value = default(T);
        return TryReadInputValue(edgeIdx, ref value);
    }

	static List<(Type, string)> s_empty = new List<(Type, string)>();
	public List<(Type type, string name)> GetUnderlyingPortDataList()
	{
		// get input edges:
		if (inputPorts.Count == 0)
			return s_empty;

		var inputEdges = GetNonRelayEdges();

		if (inputEdges != null)
			return inputEdges.Select(e => (e.outputPort.portData.displayType ?? e.outputPort.fieldInfo.FieldType, e.outputPort.portData.displayName)).ToList();

		return s_empty;
	}

	public List<SerializableEdge> GetNonRelayEdges()
	{
		var inputEdges = inputPorts?[0]?.GetEdges();

		// Iterate until we don't have a relay node in input
		while (inputEdges.Count == 1 && inputEdges.First().outputNode.GetType() == typeof(RelayNode))
			inputEdges = inputEdges.First().outputNode.inputPorts[0]?.GetEdges();

		return inputEdges;
	}
}