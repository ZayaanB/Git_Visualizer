package com.gitvisualizer.graph;

import com.fasterxml.jackson.annotation.JsonIgnoreProperties;

import java.util.List;

@JsonIgnoreProperties(ignoreUnknown = true)
public record BranchVisualizationData(
        String branchName,
        double[] colorRgb,
        List<GraphNodeData> nodes
) {
    public int nodeCount() {
        return nodes != null ? nodes.size() : 0;
    }
}
