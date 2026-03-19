using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using GitVisualizer.Models;

namespace GitVisualizer.Services
{
    /// <summary>
    /// Securely fetches repository data from the GitHub REST API.
    /// Uses UnityWebRequest for all network calls.
    /// </summary>
    public class GitHubService
    {
        private const string ApiBaseUrl = "https://api.github.com";
        private const string AcceptHeader = "application/vnd.github+json";
        private const string ApiVersionHeader = "2022-11-28";
        private const int CommitsPerBranch = 50;
        private const int MaxBranchesPerPage = 100;

        /// <summary>
        /// Result of fetching repository data (repo, branches, and commits per branch).
        /// </summary>
        public class RepoDataResult
        {
            public Repository Repository;
            public Branch[] Branches;
            public Dictionary<string, Commit[]> CommitsByBranch;

            public RepoDataResult()
            {
                CommitsByBranch = new Dictionary<string, Commit[]>();
            }
        }

        /// <summary>
        /// Asynchronously fetches repository data: repo info, all branches, and last 50 commits per branch.
        /// </summary>
        /// <param name="owner">Repository owner (e.g., "ZayaanB").</param>
        /// <param name="repoName">Repository name (e.g., "Git_Visualizer").</param>
        /// <param name="personalAccessToken">GitHub PAT for auth (use null for public repos if unauthenticated).</param>
        /// <returns>RepoDataResult with Repository, Branches, and CommitsByBranch; null on failure.</returns>
        public async Task<RepoDataResult> FetchRepoData(string owner, string repoName, string personalAccessToken)
        {
            if (string.IsNullOrWhiteSpace(owner) || string.IsNullOrWhiteSpace(repoName))
            {
                Debug.LogError("[GitHubService] Owner and repo name are required.");
                return null;
            }

            var result = new RepoDataResult();

            try
            {
                // 1. Fetch repository
                result.Repository = await FetchRepositoryAsync(owner, repoName, personalAccessToken);
                if (result.Repository == null)
                    return null;

                // 2. Fetch all branches
                result.Branches = await FetchBranchesAsync(owner, repoName, personalAccessToken);
                if (result.Branches == null)
                    return null;

                // 3. Fetch last 50 commits for each branch
                foreach (var branch in result.Branches)
                {
                    var commits = await FetchCommitsAsync(owner, repoName, branch.name, personalAccessToken);
                    if (commits != null)
                        result.CommitsByBranch[branch.name] = commits;
                }

                return result;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GitHubService] Unexpected error: {ex.Message}\n{ex.StackTrace}");
                return null;
            }
        }

        private async Task<Repository> FetchRepositoryAsync(string owner, string repoName, string token)
        {
            var url = $"{ApiBaseUrl}/repos/{owner}/{repoName}";
            var json = await SendGetRequestAsync(url, token);
            if (json == null)
                return null;

            try
            {
                return JsonConvert.DeserializeObject<Repository>(json);
            }
            catch (JsonException ex)
            {
                Debug.LogError($"[GitHubService] Failed to parse repository JSON: {ex.Message}");
                return null;
            }
        }

        private async Task<Branch[]> FetchBranchesAsync(string owner, string repoName, string token)
        {
            var url = $"{ApiBaseUrl}/repos/{owner}/{repoName}/branches?per_page={MaxBranchesPerPage}";
            var json = await SendGetRequestAsync(url, token);
            if (json == null)
                return null;

            try
            {
                return JsonConvert.DeserializeObject<Branch[]>(json) ?? Array.Empty<Branch>();
            }
            catch (JsonException ex)
            {
                Debug.LogError($"[GitHubService] Failed to parse branches JSON: {ex.Message}");
                return null;
            }
        }

        private async Task<Commit[]> FetchCommitsAsync(string owner, string repoName, string branchShaOrName, string token)
        {
            var encoded = Uri.EscapeDataString(branchShaOrName);
            var url = $"{ApiBaseUrl}/repos/{owner}/{repoName}/commits?sha={encoded}&per_page={CommitsPerBranch}";
            var json = await SendGetRequestAsync(url, token);
            if (json == null)
                return null;

            try
            {
                return JsonConvert.DeserializeObject<Commit[]>(json) ?? Array.Empty<Commit>();
            }
            catch (JsonException ex)
            {
                Debug.LogError($"[GitHubService] Failed to parse commits JSON for branch '{branchShaOrName}': {ex.Message}");
                return null;
            }
        }

        private async Task<string> SendGetRequestAsync(string url, string token)
        {
            using (var request = UnityWebRequest.Get(url))
            {
                request.SetRequestHeader("Accept", AcceptHeader);
                request.SetRequestHeader("X-GitHub-Api-Version", ApiVersionHeader);
                if (!string.IsNullOrEmpty(token))
                    request.SetRequestHeader("Authorization", $"Bearer {token}");

                var operation = request.SendWebRequest();

                while (!operation.isDone)
                    await Task.Yield();

                if (request.result == UnityWebRequest.Result.Success)
                    return request.downloadHandler.text;

                HandleRequestError(request, url);
                return null;
            }
        }

        private void HandleRequestError(UnityWebRequest request, string url)
        {
            var code = request.responseCode;
            var message = request.error ?? request.downloadHandler?.text ?? "Unknown error";

            if (code == 403)
            {
                Debug.LogError(
                    "[GitHubService] Rate limit exceeded (403). " +
                    "Check headers for reset time, or use a Personal Access Token for higher limits.");
            }
            else if (code == 404)
            {
                Debug.LogError($"[GitHubService] Resource not found (404). URL: {url}. Message: {message}");
            }
            else
            {
                Debug.LogError($"[GitHubService] Request failed. Code: {code}, URL: {url}, Error: {message}");
            }
        }
    }
}
