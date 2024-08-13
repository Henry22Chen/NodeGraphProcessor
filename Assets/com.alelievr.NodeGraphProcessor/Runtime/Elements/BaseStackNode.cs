using UnityEngine;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;

namespace GraphProcessor
{
    /// <summary>
    /// Data container for the StackNode views
    /// </summary>
    [System.Serializable]
    public class BaseStackNode : BaseNode
    {
        /// <summary>
        /// Is the stack accept drag and dropped nodes
        /// </summary>
        public bool acceptDrop;

        /// <summary>
        /// Is the stack accepting node created by pressing space over the stack node
        /// </summary>
        public bool acceptNewNode;

        /// <summary>
        /// List of node GUID that are in the stack
        /// </summary>
        /// <typeparam name="string"></typeparam>
        /// <returns></returns>
        [SerializeField]
        List< string >   nodeGUIDs = new List< string >();

        List<BaseNode> innerNodes = new List<BaseNode>();

        public virtual bool AcceptAllNodes => true;

        public override string name => "Stack";

        public List<BaseNode> InnerNodes => innerNodes;

        public override void Initialize(BaseGraph graph)
        {
            base.Initialize(graph);
            innerNodes.Clear();
            for (int i = 0; i < nodeGUIDs.Count; i++)
            {
                var nodeGUID = nodeGUIDs[i];
                if (graph.nodesPerGUID.ContainsKey(nodeGUID))
                {
                    var node = graph.nodesPerGUID[nodeGUID];
                    innerNodes.Add(node);
                }
                else
                {
                    nodeGUIDs.RemoveAt(i); // remove the entry as the GUID doesn't exist anymore
                }
            }
        }
        protected override void Process()
        {
            base.Process();
        }

        internal void AddInnerNode(int index, BaseNode node)
        {
            nodeGUIDs.Insert(index, node.GUID);
            innerNodes.Insert(index, node);
        }

        internal int GetInnerNodeIndex(BaseNode node)
        {
            return innerNodes.IndexOf(node);
        }

        internal int TryRemoveInnerNode(BaseNode node)
        {
            int oldIdx = nodeGUIDs.IndexOf(node.GUID);
            nodeGUIDs.Remove(node.GUID);
            innerNodes.Remove(node);
            return oldIdx;
        }
    }
}