package com.gitvisualizer.service;

import com.fasterxml.jackson.databind.DeserializationFeature;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.fasterxml.jackson.databind.PropertyNamingStrategies;
import com.gitvisualizer.graph.GraphVisualizationBuilder;
import com.gitvisualizer.model.Branch;
import com.gitvisualizer.model.Commit;
import com.gitvisualizer.model.Repository;

import java.io.IOException;
import java.net.URI;
import java.net.URLEncoder;
import java.nio.charset.StandardCharsets;
import java.time.Duration;
import java.util.LinkedHashMap;
import java.util.Map;
import java.util.Objects;

import java.net.http.HttpClient;
import java.net.http.HttpRequest;
import java.net.http.HttpResponse;

/** Fetch repository metadata, branches, and recent commits via the GitHub REST API. */
public class GitHubService {

    private static final String DEFAULT_API = "https://api.github.com";
    private static final String ACCEPT = "application/vnd.github+json";
    private static final String API_VERSION = "2022-11-28";

    private final HttpClient httpClient;
    private final ObjectMapper mapper;
    private final String apiBaseUrl;

    public GitHubService() {
        this(DEFAULT_API, HttpClient.newBuilder().connectTimeout(Duration.ofSeconds(30)).build());
    }

    public GitHubService(String apiBaseUrl, HttpClient httpClient) {
        this.apiBaseUrl = Objects.requireNonNullElse(apiBaseUrl, DEFAULT_API).replaceAll("/$", "");
        this.httpClient = httpClient != null ? httpClient : HttpClient.newBuilder().connectTimeout(Duration.ofSeconds(30)).build();
        this.mapper = new ObjectMapper()
                .setPropertyNamingStrategy(PropertyNamingStrategies.SNAKE_CASE)
                .configure(DeserializationFeature.FAIL_ON_UNKNOWN_PROPERTIES, false);
    }

    public record RepoDataResult(
            Repository repository,
            Branch[] branches,
            Map<String, Commit[]> commitsByBranch
    ) {
    }

    public RepoDataResult fetchRepoData(String owner, String repoName, String personalAccessToken)
            throws IOException, InterruptedException {
        if (owner == null || owner.isBlank() || repoName == null || repoName.isBlank()) {
            return null;
        }
        Repository repo = fetchRepository(owner, repoName, personalAccessToken);
        if (repo == null) {
            return null;
        }
        Branch[] branches = fetchBranches(owner, repoName, personalAccessToken);
        if (branches == null) {
            return null;
        }
        Map<String, Commit[]> commitsByBranch = new LinkedHashMap<>();
        for (Branch branch : branches) {
            if (branch == null || branch.name() == null) {
                continue;
            }
            Commit[] commits = fetchCommits(owner, repoName, branch.name(), personalAccessToken);
            if (commits != null) {
                commitsByBranch.put(branch.name(), commits);
            }
        }
        return new RepoDataResult(repo, branches, commitsByBranch);
    }

    public Repository fetchRepository(String owner, String repoName, String token)
            throws IOException, InterruptedException {
        String json = get("/repos/" + owner + "/" + repoName, token);
        if (json == null) {
            return null;
        }
        return mapper.readValue(json, Repository.class);
    }

    public Branch[] fetchBranches(String owner, String repoName, String token)
            throws IOException, InterruptedException {
        String url = "/repos/" + owner + "/" + repoName + "/branches?per_page=" + GraphVisualizationBuilder.MAX_BRANCHES_PER_PAGE;
        String json = get(url, token);
        if (json == null) {
            return null;
        }
        Branch[] arr = mapper.readValue(json, Branch[].class);
        return arr != null ? arr : new Branch[0];
    }

    public Commit[] fetchCommits(String owner, String repoName, String branchShaOrName, String token)
            throws IOException, InterruptedException {
        String encoded = URLEncoder.encode(branchShaOrName, StandardCharsets.UTF_8);
        String url = "/repos/" + owner + "/" + repoName + "/commits?sha=" + encoded
                + "&per_page=" + GraphVisualizationBuilder.COMMITS_PER_BRANCH;
        String json = get(url, token);
        if (json == null) {
            return null;
        }
        Commit[] arr = mapper.readValue(json, Commit[].class);
        return arr != null ? arr : new Commit[0];
    }

    private String get(String path, String token) throws IOException, InterruptedException {
        URI uri = URI.create(apiBaseUrl + path);
        HttpRequest.Builder b = HttpRequest.newBuilder(uri)
                .timeout(Duration.ofSeconds(60))
                .header("Accept", ACCEPT)
                .header("X-GitHub-Api-Version", API_VERSION)
                .GET();
        if (token != null && !token.isEmpty()) {
            b.header("Authorization", "Bearer " + token);
        }
        HttpResponse<String> response = httpClient.send(b.build(), HttpResponse.BodyHandlers.ofString());
        int code = response.statusCode();
        if (code >= 200 && code < 300) {
            return response.body();
        }
        return null;
    }

    public ObjectMapper objectMapper() {
        return mapper.copy();
    }
}
