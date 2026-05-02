package com.gitvisualizer.graph;

import com.fasterxml.jackson.annotation.JsonIgnoreProperties;

@JsonIgnoreProperties(ignoreUnknown = true)
public record GraphNodeData(
        double x,
        double y,
        double z,
        String sha,
        String message,
        String authorName,
        String date,
        int indexInBranch
) {
}
