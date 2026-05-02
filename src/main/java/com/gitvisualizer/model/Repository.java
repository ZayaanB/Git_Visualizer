package com.gitvisualizer.model;

import com.fasterxml.jackson.annotation.JsonIgnoreProperties;
import com.fasterxml.jackson.annotation.JsonProperty;

@JsonIgnoreProperties(ignoreUnknown = true)
public record Repository(
        Long id,
        String nodeId,
        String name,
        String fullName,
        @JsonProperty("private") boolean isPrivate,
        RepositoryOwner owner,
        String htmlUrl,
        String description,
        boolean fork,
        String url,
        String cloneUrl,
        String defaultBranch,
        Integer stargazersCount,
        Integer forksCount,
        String language,
        String createdAt,
        String updatedAt,
        String pushedAt
) {
}
