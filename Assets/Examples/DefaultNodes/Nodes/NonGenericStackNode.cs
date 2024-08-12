using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GraphProcessor;
using System.Linq;
using NodeGraphProcessor.Examples;

[System.Serializable]
public class NonGenericStackNode : BaseStackNode,IConditionalNode
{
    [Input]
    public ConditionalLink input;
    [Output]
    public ConditionalLink output;

    public NonGenericStackNode(Rect pos)
        : base(pos, false)
    {

    }

    public NonGenericStackNode() : base()
    {

    }

    public override string name => "NG Stack";
    public override bool AcceptAllNodes => false;

    public IEnumerable<IConditionalNode> GetExecutedNodes()
    {
        foreach(var i in GetOutputNodes())
        {
            if (i is IConditionalNode node)
                yield return node;
        }
    }
}
