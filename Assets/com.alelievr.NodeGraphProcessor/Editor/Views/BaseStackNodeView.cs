using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;
using static UnityEditor.Experimental.GraphView.Port;
using System.ComponentModel.Composition.Primitives;

namespace GraphProcessor
{
    /// <summary>
    /// Stack node view implementation, can be used to stack multiple node inside a context like VFX graph does.
    /// </summary>
    public class BaseStackNodeView : StackNode, IConnectable
    {
        public delegate void ReorderNodeAction(BaseNodeView nodeView, int oldIndex, int newIndex);
    
        /// <summary>
        /// StackNode data from the graph
        /// </summary>
        protected internal BaseStackNode    stackNode;
        public BaseGraphView owner { get; private set; }
        readonly string                     styleSheet = "GraphProcessorStyles/BaseStackNodeView";

        /// <summary>Triggered when a node is re-ordered in the stack.</summary>
        public event ReorderNodeAction      onNodeReordered;

        internal BaseNodeView CurrentDraggingOut { get; set; }
        public List<PortView> inputPortViews = new List<PortView>();
        public List<PortView> outputPortViews = new List<PortView>();

        protected Dictionary<string, List<PortView>> portsPerFieldName = new Dictionary<string, List<PortView>>();
        protected Dictionary<NodePort, PortView> portsPerNodePort = new Dictionary<NodePort, PortView>();
        protected Dictionary<int, PortView> portsPerIdentifier = new Dictionary<int, PortView>();

        public BaseStackNodeView(BaseStackNode stackNode)
        {
            this.stackNode = stackNode;
            styleSheets.Add(Resources.Load<StyleSheet>(styleSheet));
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);
        }
        /// <inheritdoc />
        protected override void OnSeparatorContextualMenuEvent(ContextualMenuPopulateEvent evt, int separatorIndex)
        {
            base.OnSeparatorContextualMenuEvent(evt, separatorIndex);
            // TODO: write the context menu for stack node
            InsertCreateNodeAction(evt, separatorIndex, 0);
        }

        void InsertCreateNodeAction(ContextualMenuPopulateEvent evt, int separatorIndex, int itemIndex)
        {
            //we need to arbitrarily add the editor position values because node creation context
            //exptects a non local coordinate
            var mousePosition = evt.mousePosition + owner.Window.position.position;
            evt.menu.InsertAction(itemIndex, "Add Node", (e) =>
            {
                var context = new NodeCreationContext
                {
                    screenMousePosition = mousePosition,
                    target = this,
                    index = separatorIndex,
                };
                owner.nodeCreationRequest(context);
            });
        }

        /// <summary>
        /// Called after the StackNode have been added to the graph view
        /// </summary>
        public virtual void Initialize(BaseGraphView graphView)
        {
            owner = graphView;
            headerContainer.Add(new Label(stackNode.name));
            SetPosition(stackNode.position);
            InitializePorts();
            InitializeInnerNodes();
        }

        void InitializeInnerNodes()
        {
            int i = 0;
            foreach(var node in stackNode.InnerNodes.ToArray())
            {
                var view = owner.nodeViewsPerNode[node] as GraphElement;
                view.AddToClassList("stack-child__" + i);
                i++;
                AddElement(view);
            }
        }


        void InitializePorts()
        {
            var listener = owner.connectorListener;

            foreach (var inputPort in stackNode.inputPorts)
            {
                AddPort(inputPort, Direction.Input, listener, inputPort.portData);
            }

            foreach (var outputPort in stackNode.outputPorts)
            {
                AddPort(outputPort, Direction.Output, listener, outputPort.portData);
            }
        }


        public PortView AddPort(NodePort port, Direction direction, BaseEdgeConnectorListener listener, PortData portData)
        {
            portData.vertical = true;
            PortView p = CreatePortView(direction, port, portData, listener);
            p.AddToClassList("StackNodeVertical");
            if (p.direction == Direction.Input)
            {
                inputPortViews.Add(p);

                inputContainer.Add(p);
            }
            else
            {
                outputContainer.Add(p);
            }

            p.Initialize(this, portData?.displayName);
            portsPerNodePort[port] = p;
            if (portData.identifier != -1)
                portsPerIdentifier[portData.identifier] = p;

            if (!string.IsNullOrEmpty(p.fieldName))
            {
                List<PortView> ports;
                portsPerFieldName.TryGetValue(p.fieldName, out ports);
                if (ports == null)
                {
                    ports = new List<PortView>();
                    portsPerFieldName[p.fieldName] = ports;
                }
                ports.Add(p);
            }

            return p;
        }

        protected virtual PortView CreatePortView(Direction direction, NodePort port, PortData portData, BaseEdgeConnectorListener listener)
            => PortView.CreatePortView(direction, port, portData, listener);

        /// <inheritdoc />
        public override void SetPosition(Rect newPos)
        {
            base.SetPosition(newPos);

            stackNode.position = newPos;
        }

        /// <inheritdoc />
        protected override bool AcceptsElement(GraphElement element, ref int proposedIndex, int maxIndex)
        {
            BaseNodeView nodeView = element as BaseNodeView;
            if (nodeView != null)
            {
                var arr = NodeProvider.GetNodeCompatibleStack(nodeView.nodeTarget.GetType(), owner.graph);
                if (arr != null && !arr.Contains(stackNode.GetType()))
                    return false;
            }

            bool accept = base.AcceptsElement(element, ref proposedIndex, maxIndex);

            if (accept && nodeView != null)
            {
                var index = Mathf.Clamp(proposedIndex, 0, Mathf.Max(stackNode.InnerNodes.Count - 1, 0));

                int oldIndex = stackNode.GetInnerNodeIndex(nodeView.nodeTarget);
                if (oldIndex != -1)
                {
                    if (oldIndex != index)
                    {
                        stackNode.TryRemoveInnerNode(nodeView.nodeTarget);
                        onNodeReordered?.Invoke(nodeView, oldIndex, index);
                    }
                    else
                        return accept;
                }

                stackNode.AddInnerNode(index, nodeView.nodeTarget);
            }

            return accept;
        }

        public void AddNode(GraphElement element, ref int proposedIndex)
        {
            if (element is BaseNodeView nodeView)
            {
                var index = Mathf.Clamp(proposedIndex, 0, Mathf.Max(stackNode.InnerNodes.Count - 1, 0));
                
                InsertElement(index, element);
            }
        }

        public void RemoveNode(BaseNode node)
        {
            stackNode.TryRemoveInnerNode(node);
        }

        public void RestoreNode(BaseNodeView nodeView)
        {
            int index = Mathf.Min(stackNode.GetInnerNodeIndex(nodeView.nodeTarget), childCount);
            if(index >= 0)
            {
                InsertElement(index, nodeView);
            }
        }

        public override void OnStartDragging(GraphElement ge)
        {
            CurrentDraggingOut = ge as BaseNodeView;

            base.OnStartDragging(ge);
        }

        public override bool DragLeave(DragLeaveEvent evt, IEnumerable<ISelectable> selection, IDropTarget leftTarget, ISelection dragSource)
        {
            foreach (var elem in selection)
            {
                if(elem is BaseNodeView nodeView)
                {
                    CurrentDraggingOut = nodeView;
                }
            }
            return base.DragLeave(evt, selection, leftTarget, dragSource);
        }

        public override bool DragPerform(DragPerformEvent evt, IEnumerable<ISelectable> selection, IDropTarget dropTarget, ISelection dragSource)
        {
            CurrentDraggingOut = null;
            return base.DragPerform(evt, selection, dropTarget, dragSource);
        }

        protected override void HandleEventBubbleUp(EventBase evt)
        {
            base.HandleEventBubbleUp(evt);
        }

        public virtual void OnPortConnected(PortView port)
        {
        }

        public virtual void OnPortDisconnected(PortView port)
        {
        }

        public NodePort GetPort(string fieldName, int identifier)
        {
            return stackNode.GetPort(fieldName, identifier);
        }
        public List<PortView> GetPortViewsFromFieldName(string fieldName)
        {
            List<PortView> ret;

            portsPerFieldName.TryGetValue(fieldName, out ret);

            return ret;
        }
        public PortView GetPortViewFromFieldName(string fieldName, int identifier)
        {
            if (string.IsNullOrEmpty(fieldName))
            {
                if (portsPerIdentifier.TryGetValue(identifier, out var view))
                    return view;
                return null;
            }
            else
            {
                return GetPortViewsFromFieldName(fieldName)?.FirstOrDefault(pv =>
                {
                    return (pv.portData.identifier == identifier) || (pv.portData.identifier == -1 && identifier == -1);
                });
            }
        }

        public virtual new bool RefreshPorts()
        {
            return base.RefreshPorts();
        }
    }
}