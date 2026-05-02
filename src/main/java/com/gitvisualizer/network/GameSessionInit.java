package com.gitvisualizer.network;

import com.fasterxml.jackson.annotation.JsonIgnoreProperties;
import com.gitvisualizer.graph.GraphVisualizationPayload;

import java.util.List;

@JsonIgnoreProperties(ignoreUnknown = true)
public record GameSessionInit(
        GraphVisualizationPayload graph,
        List<Integer> conflictIndices,
        float remainingTime,
        int resolvedCount,
        boolean gameEnded,
        Boolean win
) {
}
