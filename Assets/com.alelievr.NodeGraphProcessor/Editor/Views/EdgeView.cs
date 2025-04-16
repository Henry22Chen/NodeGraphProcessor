using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using UnityEngine;

namespace GraphProcessor
{
	public class EdgeView : Edge
	{
		public bool					isConnected = false;

		public SerializableEdge		serializedEdge { get { return userData as SerializableEdge; } }

		readonly string				edgeStyle = "GraphProcessorStyles/EdgeView";

		protected BaseGraphView		owner => ((input ?? output) as PortView).owner.owner;
 
		protected List<VisualElement> EdgeFlowPointVisualElements;
 		
		protected List<float> FlowPointProgress = new List<float>();
		
		public EdgeView() : base()
		{
			styleSheets.Add(Resources.Load<StyleSheet>(edgeStyle));
			RegisterCallback<MouseDownEvent>(OnMouseDown);
		}

        public override void OnPortChanged(bool isInput)
		{
			base.OnPortChanged(isInput);
			UpdateEdgeSize();
		}

		public void UpdateEdgeSize()
		{
			if (input == null && output == null)
				return;

			PortData inputPortData = (input as PortView)?.portData;
			PortData outputPortData = (output as PortView)?.portData;

			for (int i = 1; i < 20; i++)
				RemoveFromClassList($"edge_{i}");
			int maxPortSize = Mathf.Max(inputPortData?.sizeInPixel ?? 0, outputPortData?.sizeInPixel ?? 0);
			if (maxPortSize > 0)
				AddToClassList($"edge_{Mathf.Max(1, maxPortSize - 6)}");
		}

		/// <summary>
		/// draw flow point on this edge, should call by some Update method (SomeGraphWindow)
		/// This method comes from repo https://github.com/wqaetly/NodeGraphProcessor 
		/// </summary>
		/// <param name="flowPointGap">the distance of two point</param>
		/// <param name="flowPointMoveSpeed"></param>
		public virtual void DrawEdgeFlowPoint(float flowPointGap = 60f, float flowPointMoveSpeed = 0.009f)
		{
			float edgeLength = 0;
            for (int i = 0; i < PointsAndTangents.Length - 1; i++)
            {
                edgeLength += Vector2.Distance(PointsAndTangents[i], PointsAndTangents[i + 1]);
            }

            float eachChunkContainsPercentage = flowPointGap / edgeLength;
            int flowPointCount = (int)(1 / eachChunkContainsPercentage);

            if (flowPointCount % 2 == 0)
            {
                flowPointCount++;
            }

            if (EdgeFlowPointVisualElements != null && EdgeFlowPointVisualElements.Count > 0 &&
                //如果长度发生变化就需要重新计算
                EdgeFlowPointVisualElements.Count == flowPointCount)
            {
                for (int i = 0; i < flowPointCount; i++)
                {
                    FlowPointProgress[i] += Time.deltaTime * flowPointMoveSpeed;

                    EdgeFlowPointVisualElements[i].transform.position =
                        EdgeFlowPointCaculator.GetFlowPointPosByPercentage(
                            Mathf.Repeat(FlowPointProgress[i], 1),
                            PointsAndTangents, edgeLength) -
                        new Vector2(8 * i, 0);
                }
            }
            else
            {
                if (EdgeFlowPointVisualElements != null)
                {
                    foreach (var oldFlowPoint in EdgeFlowPointVisualElements)
                    {
                        Remove(oldFlowPoint);
                    }
                }

                EdgeFlowPointVisualElements = new List<VisualElement>();
                FlowPointProgress.Clear();

                for (int i = 0; i < flowPointCount; i++)
                {
                    float initalPercentage = eachChunkContainsPercentage * i;

                    VisualElement visualElement = new VisualElement()
                    {
                        name = "EdgeFlowPoint", transform =
                        {
                            position = EdgeFlowPointCaculator.GetFlowPointPosByPercentage(
                                           initalPercentage, PointsAndTangents, edgeLength) -
                                       new Vector2(8 * i, 0),
                        }
                    };
                    //可以自定义流点颜色，但注意将其alpha通道设置为1
                    //visualElement.style.unityBackgroundImageTintColor = serializedEdge.outputNode.color;
                    FlowPointProgress.Add(initalPercentage);
                    EdgeFlowPointVisualElements.Add(visualElement);
                    Add(visualElement);
                }
            }
		}

		public virtual void ClearFlowPoint()
		{
			if (EdgeFlowPointVisualElements == null) return;
			foreach (var edgeFlowPoint in EdgeFlowPointVisualElements)
			{
				Remove(edgeFlowPoint);
			}

			EdgeFlowPointVisualElements.Clear();
		}
		
		protected override void OnCustomStyleResolved(ICustomStyle styles)
		{
			base.OnCustomStyleResolved(styles);

			UpdateEdgeControl();
		}

		void OnMouseDown(MouseDownEvent e)
		{
			if (e.clickCount == 2)
			{
				// Empirical offset:
				var position = e.mousePosition;
                position += new Vector2(-10f, -28);
                Vector2 mousePos = owner.ChangeCoordinatesTo(owner.contentViewContainer, position);

				owner.AddRelayNode(input as PortView, output as PortView, mousePos);
			}
		}
	}
}