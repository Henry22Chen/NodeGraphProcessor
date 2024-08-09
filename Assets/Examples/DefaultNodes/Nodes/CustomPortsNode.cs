using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GraphProcessor;
using System.Linq;

[System.Serializable, NodeMenuItem("Custom/MultiPorts")]
public class CustomPortsNode : BaseNode
{
    public override string		name => "CustomPorts";

    public override string      layoutStyle => "TestType";
	protected override bool hasCustomInputs => true;
	protected override bool hasCustomOutputs => true;

    // We keep the max port count so it doesn't cause binding issues
    [SerializeField, HideInInspector]
	int							portCount = 1;

	protected override void Process()
	{
		// do things with values
	}

    public override void OnEdgeConnected(SerializableEdge edge)
    {
        base.OnEdgeConnected(edge);
        UpdateAllPorts();
    }

    int GetEdgeCount()
    {
        int cnt = 0;
        foreach(var i in inputPorts)
        {
            cnt += i.GetEdges().Count;
        }
        return cnt;
    }

    protected override IEnumerable<PortData> GetCustomInputPorts()
	{
		portCount = Mathf.Max(portCount, GetEdgeCount() + 1);
		for (int i = 0; i < portCount; i++)
		{
			yield return BuildCustomPort(null, typeof(float), "In " + i);
		}
	}

    protected override IEnumerable<PortData> GetCustomOutputPorts()
    {
        yield return BuildCustomPort(null, typeof(float), "Out", allowMultiple: true);
    }

    protected override bool TryGetOutputValue<T>(int index, out T value, int edgeIndex)
    {
		value = default;
		return TryReadInputValue(edgeIndex, ref value);
    }
}
