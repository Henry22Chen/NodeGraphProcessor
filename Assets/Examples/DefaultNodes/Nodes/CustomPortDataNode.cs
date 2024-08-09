using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GraphProcessor;
using System.Linq;
using UnityEngine.Windows;

[System.Serializable, NodeMenuItem("Custom/PortData")]
public class CustomPortData : BaseNode
{
	[Output]
	public float				output;

	public override string		name => "Port Data";

    protected override void Process()
    {
        output = 0;
        var cnt = inputPorts[0].GetEdges().Count;

        for (int i = 0; i < cnt; i++)
        {
            float floatVal = 0;
            if (!TryReadInputValue(0, ref floatVal, i))
            {
                TryReadInputValue<int, float>(i, ref floatVal);
            }
            output += floatVal;
        }
    }

    protected override bool hasCustomOutputs => true;
	protected override bool hasCustomInputs => true;

    protected override bool TryGetOutputValue<T>(int index, out T value, int edgeIndex)
    {
        switch (index)
        {
            case 0:
                return TryConvertValue(ref output, out value);
            case 1:
                int intVal = (int)output;
                return TryConvertValue(ref intVal, out value);
        }
        value = default;
        return false;
    }

    protected override IEnumerable<PortData> GetCustomInputPorts()
    {
        yield return BuildCustomPort(null, typeof(object), "In Values", allowMultiple: true);
    }
    protected override IEnumerable<PortData> GetCustomOutputPorts()
    {
        yield return BuildCustomPort(null, typeof(float), "0", false, true);
        yield return BuildCustomPort(null, typeof(int), "1", false, true);
        yield return BuildCustomPort(null, typeof(GameObject), "2", false, true);
        yield return BuildCustomPort(null, typeof(Texture2D), "3", false, true);
    }
}
