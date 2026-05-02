package com.gitvisualizer.ui;

import com.gitvisualizer.graph.BranchVisualizationData;
import com.gitvisualizer.graph.GraphNodeData;
import com.gitvisualizer.graph.GraphVisualizationPayload;
import javafx.geometry.Point3D;
import javafx.scene.Group;
import javafx.scene.Node;
import javafx.scene.paint.Color;
import javafx.scene.paint.PhongMaterial;
import javafx.scene.shape.Cylinder;
import javafx.scene.shape.Sphere;
import javafx.scene.transform.Rotate;
import javafx.scene.transform.Transform;
import javafx.scene.transform.Translate;

import java.util.ArrayList;
import java.util.List;

/** Spheres per commit and cylinders along each branch for JavaFX 3D. */
public final class Graph3DFactory {

    public static final double NODE_RADIUS = 0.15;
    public static final double EDGE_RADIUS = 0.04;
    private static final int MAX_LINE_VERTICES = 128;

    private Graph3DFactory() {
    }

    public record IndexedSphere(Sphere sphere, int globalIndex, GraphNodeData data, Color branchColor) {
    }

    public record GraphMeshes(Group root, List<IndexedSphere> spheres) {
    }

    public static GraphMeshes build(GraphVisualizationPayload payload) {
        Group root = new Group();
        List<IndexedSphere> spheres = new ArrayList<>();
        int globalIndex = 0;
        if (payload == null || payload.branches() == null) {
            return new GraphMeshes(root, spheres);
        }
        for (BranchVisualizationData branch : payload.branches()) {
            if (branch == null || branch.nodes() == null || branch.nodes().isEmpty()) {
                continue;
            }
            Color branchColor = toFxColor(branch.colorRgb());
            List<Point3D> linePts = sampleBranchLine(branch.nodes());
            for (int i = 0; i < linePts.size() - 1; i++) {
                root.getChildren().add(edgeCylinder(linePts.get(i), linePts.get(i + 1), EDGE_RADIUS, branchColor));
            }
            for (GraphNodeData node : branch.nodes()) {
                Sphere s = new Sphere(NODE_RADIUS);
                s.setTranslateX(node.x());
                s.setTranslateY(node.y());
                s.setTranslateZ(node.z());
                PhongMaterial mat = new PhongMaterial(branchColor);
                s.setMaterial(mat);
                spheres.add(new IndexedSphere(s, globalIndex, node, branchColor));
                root.getChildren().add(s);
                globalIndex++;
            }
        }
        return new GraphMeshes(root, spheres);
    }

    private static Color toFxColor(double[] rgb) {
        if (rgb == null || rgb.length < 3) {
            return Color.GRAY;
        }
        return new Color(rgb[0], rgb[1], rgb[2], 1.0);
    }

    static List<Point3D> sampleBranchLine(List<GraphNodeData> nodes) {
        int n = nodes.size();
        if (n < 2) {
            return List.of();
        }
        int vertexCount = Math.min(n, MAX_LINE_VERTICES);
        List<Point3D> positions = new ArrayList<>(vertexCount);
        if (vertexCount == n) {
            for (GraphNodeData nd : nodes) {
                positions.add(new Point3D(nd.x(), nd.y(), nd.z()));
            }
        } else {
            for (int i = 0; i < vertexCount; i++) {
                double t = vertexCount == 1 ? 0 : (double) i / (vertexCount - 1);
                int idx = (int) Math.round(t * (n - 1));
                GraphNodeData nd = nodes.get(idx);
                positions.add(new Point3D(nd.x(), nd.y(), nd.z()));
            }
        }
        return positions;
    }

    static Node edgeCylinder(Point3D a, Point3D b, double radius, Color color) {
        Point3D diff = b.subtract(a);
        double len = diff.magnitude();
        if (len < 1e-6) {
            return new Group();
        }
        Point3D dir = diff.normalize();
        Point3D mid = a.midpoint(b);
        Cylinder c = new Cylinder(radius, len);
        Point3D yAxis = new Point3D(0, 1, 0);
        Point3D axis = yAxis.crossProduct(dir);
        double axisMag = axis.magnitude();
        Group g = new Group(c);
        PhongMaterial mat = new PhongMaterial(color);
        c.setMaterial(mat);
        List<Transform> transforms = new ArrayList<>();
        if (axisMag < 1e-6) {
            if (dir.getY() < 0) {
                transforms.add(new Rotate(180, Rotate.X_AXIS));
            }
        } else {
            axis = axis.normalize();
            double angleRad = Math.acos(clamp(yAxis.dotProduct(dir), -1.0, 1.0));
            transforms.add(new Rotate(Math.toDegrees(angleRad), axis));
        }
        transforms.add(new Translate(mid.getX(), mid.getY(), mid.getZ()));
        g.getTransforms().setAll(transforms);
        return g;
    }

    private static double clamp(double v, double lo, double hi) {
        return Math.max(lo, Math.min(hi, v));
    }
}
