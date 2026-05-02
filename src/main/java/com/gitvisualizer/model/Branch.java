package com.gitvisualizer.model;

import com.fasterxml.jackson.annotation.JsonIgnoreProperties;
import com.fasterxml.jackson.annotation.JsonProperty;

@JsonIgnoreProperties(ignoreUnknown = true)
public record Branch(
        String name,
        @JsonProperty("protected") boolean isProtected,
        BranchCommit commit
) {
}
