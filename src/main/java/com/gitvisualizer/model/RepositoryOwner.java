package com.gitvisualizer.model;

import com.fasterxml.jackson.annotation.JsonIgnoreProperties;

@JsonIgnoreProperties(ignoreUnknown = true)
public record RepositoryOwner(String login, Long id, String avatarUrl, String htmlUrl, String type) {
}
