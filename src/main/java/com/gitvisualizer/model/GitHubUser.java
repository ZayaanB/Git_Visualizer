package com.gitvisualizer.model;

import com.fasterxml.jackson.annotation.JsonIgnoreProperties;

@JsonIgnoreProperties(ignoreUnknown = true)
public record GitHubUser(
        String login,
        Long id,
        String name,
        String email,
        String avatarUrl,
        String htmlUrl
) {
}
