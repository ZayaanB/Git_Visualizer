package com.gitvisualizer.model;

import com.fasterxml.jackson.annotation.JsonIgnoreProperties;

@JsonIgnoreProperties(ignoreUnknown = true)
public record CommitInfo(
        GitAuthor author,
        GitAuthor committer,
        String message,
        CommitTree tree,
        String url,
        Integer commentCount
) {
}
