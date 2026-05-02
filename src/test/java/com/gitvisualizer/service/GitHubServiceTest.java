package com.gitvisualizer.service;

import com.gitvisualizer.model.Branch;
import okhttp3.mockwebserver.MockResponse;
import okhttp3.mockwebserver.MockWebServer;
import org.junit.jupiter.api.AfterEach;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;

import java.net.http.HttpClient;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertNotNull;

class GitHubServiceTest {

    private MockWebServer server;

    @BeforeEach
    void setUp() throws Exception {
        server = new MockWebServer();
        server.start();
    }

    @AfterEach
    void tearDown() throws Exception {
        server.shutdown();
    }

    @Test
    void fetchRepositoryBranchesAndCommits() throws Exception {
        String base = server.url("/").toString().replaceAll("/$", "");
        server.enqueue(new MockResponse().setBody("{\"id\":1,\"name\":\"r\",\"full_name\":\"o/r\",\"private\":false}"));
        server.enqueue(new MockResponse().setBody("[{\"name\":\"main\",\"protected\":false,\"commit\":{\"sha\":\"abc\"}}]"));
        server.enqueue(new MockResponse().setBody("[{\"sha\":\"c1\",\"commit\":{\"message\":\"m\",\"author\":{\"name\":\"a\",\"date\":\"2020-01-01T00:00:00Z\"},\"committer\":{\"name\":\"a\",\"date\":\"2020-01-01T00:00:00Z\"}}}]"));

        GitHubService gh = new GitHubService(base, HttpClient.newHttpClient());
        GitHubService.RepoDataResult data = gh.fetchRepoData("o", "r", "");
        assertNotNull(data);
        assertEquals("r", data.repository().name());
        assertEquals(1, data.branches().length);
        Branch main = data.branches()[0];
        assertEquals("main", main.name());
        assertNotNull(data.commitsByBranch().get("main"));
        assertEquals(1, data.commitsByBranch().get("main").length);
    }

    @Test
    void fetchFailsReturnsNullWhenRepoMissing() throws Exception {
        server.enqueue(new MockResponse().setResponseCode(404));
        GitHubService gh = new GitHubService(server.url("/").toString().replaceAll("/$", ""), HttpClient.newHttpClient());
        assertEquals(null, gh.fetchRepoData("o", "missing", ""));
    }
}
