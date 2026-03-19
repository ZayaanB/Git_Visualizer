using System;

/// <summary>
/// Data models for parsing GitHub REST API JSON responses.
/// All classes are serializable for use with JsonUtility or other JSON parsers.
/// </summary>
namespace GitVisualizer.Models
{
    #region Author & User

    /// <summary>
    /// Git author/committer info from commit metadata (name, email, date).
    /// </summary>
    [Serializable]
    public class GitAuthor
    {
        public string name;
        public string email;
        public string date;
    }

    /// <summary>
    /// GitHub user (Simple User) - appears in commits, repos, branches.
    /// </summary>
    [Serializable]
    public class GitHubUser
    {
        public string login;
        public long id;
        public string node_id;
        public string avatar_url;
        public string gravatar_id;
        public string url;
        public string html_url;
        public string type;
        public bool site_admin;
        public string name;
        public string email;
    }

    #endregion

    #region Repository

    /// <summary>
    /// Repository owner - minimal user reference.
    /// </summary>
    [Serializable]
    public class RepositoryOwner
    {
        public string login;
        public long id;
        public string node_id;
        public string avatar_url;
        public string url;
        public string html_url;
        public string type;
    }

    /// <summary>
    /// GitHub repository from GET /repos/{owner}/{repo}.
    /// </summary>
    [Serializable]
    public class Repository
    {
        public long id;
        public string node_id;
        public string name;
        public string full_name;
        public bool @private;
        public RepositoryOwner owner;
        public string html_url;
        public string description;
        public bool fork;
        public string url;
        public string clone_url;
        public string default_branch;
        public int stargazers_count;
        public int watchers_count;
        public int forks_count;
        public int open_issues_count;
        public string language;
        public int size;
        public string created_at;
        public string updated_at;
        public string pushed_at;
    }

    #endregion

    #region Branch

    /// <summary>
    /// Commit reference within a branch (sha + url).
    /// </summary>
    [Serializable]
    public class BranchCommit
    {
        public string sha;
        public string url;
    }

    /// <summary>
    /// Branch from GET /repos/{owner}/{repo}/branches.
    /// </summary>
    [Serializable]
    public class Branch
    {
        public string name;
        public bool @protected;
        public BranchCommit commit;
    }

    #endregion

    #region Commit

    /// <summary>
    /// Inner commit object with author, committer, and message.
    /// </summary>
    [Serializable]
    public class CommitInfo
    {
        public GitAuthor author;
        public GitAuthor committer;
        public string message;
        public CommitTree tree;
        public string url;
        public int comment_count;
    }

    /// <summary>
    /// Tree reference in a commit.
    /// </summary>
    [Serializable]
    public class CommitTree
    {
        public string sha;
        public string url;
    }

    /// <summary>
    /// Full commit from GET /repos/{owner}/{repo}/commits.
    /// </summary>
    [Serializable]
    public class Commit
    {
        public string sha;
        public string node_id;
        public string url;
        public string html_url;
        public CommitInfo commit;
        public GitHubUser author;
        public GitHubUser committer;
        public CommitParent[] parents;
    }

    /// <summary>
    /// Parent commit reference.
    /// </summary>
    [Serializable]
    public class CommitParent
    {
        public string sha;
        public string url;
        public string html_url;
    }

    #endregion
}
