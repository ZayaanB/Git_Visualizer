using System;

namespace GitVisualizer.Models
{
    [Serializable]
    public class GitAuthor
    {
        public string name;
        public string email;
        public string date;
    }

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

    [Serializable]
    public class BranchCommit
    {
        public string sha;
        public string url;
    }

    [Serializable]
    public class Branch
    {
        public string name;
        public bool @protected;
        public BranchCommit commit;
    }

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

    [Serializable]
    public class CommitTree
    {
        public string sha;
        public string url;
    }

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

    [Serializable]
    public class CommitParent
    {
        public string sha;
        public string url;
        public string html_url;
    }
}
