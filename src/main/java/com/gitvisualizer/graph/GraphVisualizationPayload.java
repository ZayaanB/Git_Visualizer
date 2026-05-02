package com.gitvisualizer.graph;

import com.fasterxml.jackson.annotation.JsonIgnoreProperties;

import java.util.List;

@JsonIgnoreProperties(ignoreUnknown = true)
public record GraphVisualizationPayload(List<BranchVisualizationData> branches) {
    public int branchCount() {
        return branches != null ? branches.size() : 0;
    }

    /** Global node index order: branches in list order, then commits along each branch. */
    public int totalNodeCount() {
        if (branches == null) {
            return 0;
        }
        int n = 0;
        for (BranchVisualizationData b : branches) {
            if (b.nodes() != null) {
                n += b.nodes().size();
            }
        }
        return n;
    }
}
