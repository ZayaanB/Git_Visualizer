package com.gitvisualizer.model;

import com.fasterxml.jackson.annotation.JsonIgnoreProperties;

@JsonIgnoreProperties(ignoreUnknown = true)
public record Commit(
        String sha,
        String nodeId,
        String url,
        String htmlUrl,
        CommitInfo commit,
        GitHubUser author,
        GitHubUser committer,
        CommitParent[] parents
) {
}
