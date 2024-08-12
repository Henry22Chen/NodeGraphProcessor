using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;

namespace GraphProcessor
{
    /// <summary>
    /// Stack node view implementation, can be used to stack multiple node inside a context like VFX graph does.
    /// </summary>
    public class BaseStackNodeView : StackNode
    {
        public delegate void ReorderNodeAction(BaseNodeView nodeView, int oldIndex, int newIndex);
    
        /// <summary>
        /// StackNode data from the graph
        /// </summary>
        protected internal BaseStackNode    stackNode;
        protected BaseGraphView             owner;
        readonly string                     styleSheet = "GraphProcessorStyles/BaseStackNodeView";

        /// <summary>Triggered when a node is re-ordered in the stack.</summary>
        public event ReorderNodeAction      onNodeReordered;

        internal BaseNodeView CurrentDraggingOut { get; set; }

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
            headerContainer.Add(new Label(stackNode.title));

            SetPosition(new Rect(stackNode.position, Vector2.one));

            InitializeInnerNodes();
        }

        void InitializeInnerNodes()
        {
            int i = 0;
            // Sanitize the GUID list in case some nodes were removed
            stackNode.nodeGUIDs.RemoveAll(nodeGUID =>
            {
                if (owner.graph.nodesPerGUID.ContainsKey(nodeGUID))
                {
                    var node = owner.graph.nodesPerGUID[nodeGUID];
                    var view = owner.nodeViewsPerNode[node];
                    view.AddToClassList("stack-child__" + i);
                    i++;
                    AddElement(view);
                    return false;
                }
                else
                {
                    return true; // remove the entry as the GUID doesn't exist anymore
                }
            });
        }

        /// <inheritdoc />
        public override void SetPosition(Rect newPos)
        {
            base.SetPosition(newPos);

            stackNode.position = newPos.position;
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
                var index = Mathf.Clamp(proposedIndex, 0, Mathf.Max(stackNode.nodeGUIDs.Count - 1, 0));

                int oldIndex = stackNode.nodeGUIDs.FindIndex(g => g == nodeView.nodeTarget.GUID);
                if (oldIndex != -1)
                {
                    stackNode.nodeGUIDs.Remove(nodeView.nodeTarget.GUID);
                    if (oldIndex != index)
                        onNodeReordered?.Invoke(nodeView, oldIndex, index);
                }

                stackNode.nodeGUIDs.Insert(index, nodeView.nodeTarget.GUID);
            }

            return accept;
        }

        public void AddNode(GraphElement element, ref int proposedIndex)
        {
            if (element is BaseNodeView nodeView)
            {
                var index = Mathf.Clamp(proposedIndex, 0, Mathf.Max(stackNode.nodeGUIDs.Count - 1, 0));
                stackNode.nodeGUIDs.Insert(index, nodeView.nodeTarget.GUID);
                InsertElement(index, element);
            }
        }

        public void RemoveNode(BaseNode node)
        {
            stackNode.nodeGUIDs.Remove(node.GUID);
        }

        public void RestoreNode(BaseNodeView nodeView)
        {
            int index = Mathf.Min(stackNode.nodeGUIDs.IndexOf(nodeView.nodeTarget.GUID), childCount);
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
    }
}