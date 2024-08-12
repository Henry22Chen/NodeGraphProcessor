using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GraphProcessor;
using System.Linq;

[System.Serializable]
public class NonGenericStackNode : BaseStackNode
{
    public NonGenericStackNode(Vector2 pos)
        : base(pos, "NG Stack", false)
    {

    }
    public override bool AcceptAllNodes => false;
}
