package com.gitvisualizer.network;

import com.fasterxml.jackson.annotation.JsonIgnoreProperties;

import java.util.List;

@JsonIgnoreProperties(ignoreUnknown = true)
public record GameStateSnapshot(
        float remainingTime,
        int resolvedCount,
        List<Integer> conflictIndices,
        boolean gameEnded,
        Boolean win
) {
}
