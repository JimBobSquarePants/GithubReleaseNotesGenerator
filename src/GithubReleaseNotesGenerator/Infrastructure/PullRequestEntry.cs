using Octokit;
using System.Collections.Generic;
using System.Linq;

namespace GithubReleaseNotesGenerator.Infrastructure
{
    /// <summary>
    /// A class containing the info about a pull request we need to generate release notes.
    /// </summary>
    internal class PullRequestEntry
    {
        /// <summary>
        /// Gets or sets the pull request.
        /// </summary>
        public PullRequest PullRequest { get; set; }

        /// <summary>
        /// Gets or sets the collection of commits on the PR.
        /// </summary>
        public IEnumerable<PullRequestCommit> Commits { get; set; }

        /// <summary>
        /// Gets or sets the collection of contibutors on the PR.
        /// </summary>
        public IEnumerable<Author> Contributors { get; set; }

        /// <summary>
        /// Gets or sets the collection of labels on the PR.
        /// </summary>
        public IEnumerable<string> Labels { get; set; } = Enumerable.Empty<string>();
    }
}
