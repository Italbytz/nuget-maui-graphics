using System;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Layout.Layered;
using Microsoft.Msagl.Miscellaneous;
using Font = Microsoft.Maui.Graphics.Font;
using Label = Microsoft.Msagl.Core.Layout.Label;
using Node = Microsoft.Msagl.Core.Layout.Node;
using Edge = Microsoft.Msagl.Core.Layout.Edge;
using Italbytz.Ports.Graph;

namespace Italbytz.Maui.Graphics
{
    public class GraphDrawable : IDrawable
    {
        private GeometryGraph graph;
        private ICanvas? canvas;
        private RectF dirtyRect;
        private double scale = 1.0;

        private Func<Edge, double> weight;
        private Func<ITaggedEdge<string, double>, bool> mark;

        public GraphDrawable(IUndirectedGraph<string, ITaggedEdge<string, double>> graph, Func<ITaggedEdge<string, double>, bool> mark)
        {

            this.graph = graph.ToGeometryGraph();
            this.weight = (edge) => ((ITaggedEdge<string, double>)edge.UserData).Tag;
            this.mark = mark;
        }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            this.canvas = canvas;
            this.dirtyRect = dirtyRect;
            LayoutGraph();
            DrawGraph();
        }

        private void LayoutGraph()
        {
            var aspectRatio = dirtyRect.Width / dirtyRect.Height;
            var minimalHeight = 0.0;
            var minimalWidth = 0.0;

            var settings = new SugiyamaLayoutSettings()
            {
                AspectRatio = aspectRatio,
                MinimalHeight = minimalHeight,
                MinimalWidth = minimalWidth
            };

            LayoutHelpers.CalculateLayout(graph, settings, null);
            graph.UpdateBoundingBox();
            var scale = Math.Min(dirtyRect.Height / graph.BoundingBox.Height, dirtyRect.Width / graph.BoundingBox.Width);
            if (scale < 1.0)
            {
                this.scale = scale;
                graph.Transform(PlaneTransformation.ScaleAroundCenterTransformation(scale, new Microsoft.Msagl.Core.Geometry.Point(graph.BoundingBox.Center.X, graph.BoundingBox.Center.Y)));
                graph.UpdateBoundingBox();
            }

        }

        private void DrawGraph()
        {
            canvas.FontColor = Colors.Gray;
            canvas.FontSize = (float)(16 * scale);
            canvas.Font = Font.Default;

            // Move model to positive axis.
            graph.Translate(new Microsoft.Msagl.Core.Geometry.Point(-graph.Left, -graph.Bottom));

            // Center graph
            graph.Translate(new Microsoft.Msagl.Core.Geometry.Point((dirtyRect.Width - graph.BoundingBox.Width) / 2, (dirtyRect.Height - graph.BoundingBox.Height) / 2));

            foreach (var node in graph.Nodes)
            {
                DrawNode(node);
            }

            foreach (var edge in graph.Edges)
            {
                DrawEdge(edge, weight(edge), mark((ITaggedEdge<string, double>)edge.UserData));
            }
        }

        private void DrawNode(Node node)
        {
            canvas.StrokeColor = Colors.Gray;
            canvas.StrokeSize = 1;
            canvas.DrawEllipse((float)node.BoundingBox.LeftBottom.X, (float)node.BoundingBox.LeftBottom.Y, (float)node.Width, (float)node.Height);
            if (node.UserData is String)
            {
                canvas.DrawString((string)node.UserData, (float)node.BoundingBox.LeftBottom.X, (float)node.BoundingBox.LeftBottom.Y, (float)node.BoundingBox.Width, (float)node.BoundingBox.Height, HorizontalAlignment.Center, VerticalAlignment.Center);
            }
        }

        private void DrawLabel(Label label, String text)
        {
            canvas.DrawString(text, (float)(label.BoundingBox.LeftBottom.X + 10.0), (float)label.BoundingBox.LeftBottom.Y, (float)label.BoundingBox.Width, (float)label.BoundingBox.Height, HorizontalAlignment.Left, VerticalAlignment.Center);
        }

        private void DrawEdge(Edge edge, double weight, bool mark)
        {
            canvas.StrokeColor = mark ? Colors.Blue : Colors.Grey;
            canvas.StrokeSize = 2;

            // When curve is a line segment.
            if (edge.Curve is LineSegment)
            {
                var line = edge.Curve as LineSegment;
                canvas.DrawLine((float)line.Start.X, (float)line.Start.Y, (float)line.End.X, (float)line.End.Y);
            }
            // When curve is a complex segment.            
            else if (edge.Curve is Curve)
            {
                PathF path = new PathF();
                path.MoveTo((float)edge.Curve.Start.X, (float)edge.Curve.Start.Y);
                foreach (var segment in (edge.Curve as Curve).Segments)
                {
                    if (edge.Curve is LineSegment)
                    {
                        var line = edge.Curve as LineSegment;
                        path.LineTo((float)line.End.X, (float)line.End.Y);
                    }
                    else if (segment is CubicBezierSegment)
                    {
                        var bezier = segment as CubicBezierSegment;
                        path.CurveTo((float)bezier.B(1).X, (float)bezier.B(1).Y, (float)bezier.B(2).X, (float)bezier.B(2).Y, (float)bezier.B(3).X, (float)bezier.B(3).Y);
                    }
                    else if (segment is Ellipse)
                    {
                        var ellipse = segment as Ellipse;
                        // TODO: Use path.AddArc instead?

                        for (var i = ellipse.ParStart;
                                    i < ellipse.ParEnd;
                                    i += (ellipse.ParEnd - ellipse.ParStart) / 5.0)
                        {
                            var p = ellipse.Center
                                + (Math.Cos(i) * ellipse.AxisA)
                                + (Math.Sin(i) * ellipse.AxisB);
                            path.LineTo((float)p.X, (float)p.Y);
                        }
                    }
                }
                path.LineTo((float)edge.Curve.End.X, (float)edge.Curve.End.Y);
                canvas.DrawPath(path);
            }
            DrawLabel(edge.Label, weight.ToString());
        }
    }
}

