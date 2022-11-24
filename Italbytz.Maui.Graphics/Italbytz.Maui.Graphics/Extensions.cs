using System;
using Italbytz.Ports.Graph;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;

namespace Italbytz.Maui.Graphics
{
    public static class Extensions
    {
        public static GeometryGraph ToGeometryGraph(this IUndirectedGraph<string, ITaggedEdge<string, double>> graph,
        double nodeSize = 30.0, double labelWidth = 60.0, double labelHeight = 30.0)
        {
            var geometryGraph = new GeometryGraph();
            var nodes = new Dictionary<string, Node>();
            foreach (var edge in graph.Edges)
            {
                Node? node = null;
                if (!nodes.ContainsKey(edge.Source))
                {
                    node = new Node(CurveFactory.CreateRectangle(nodeSize, nodeSize, new Microsoft.Msagl.Core.Geometry.Point()), edge.Source);
                    nodes.Add(edge.Source, node);
                    geometryGraph.Nodes.Add(node);
                }
                if (!nodes.ContainsKey(edge.Target))
                {
                    node = new Node(CurveFactory.CreateRectangle(nodeSize, nodeSize, new Microsoft.Msagl.Core.Geometry.Point()), edge.Target);
                    nodes.Add(edge.Target, node);
                    geometryGraph.Nodes.Add(node);
                }
                geometryGraph.Edges.Add(new Edge(nodes[edge.Source], nodes[edge.Target])
                {
                    Label = new Microsoft.Msagl.Core.Layout.Label()
                    {
                        Width = labelWidth,
                        Height = labelHeight
                    },
                    UserData = edge
                });
            }

            return geometryGraph;
        }
    }
}

