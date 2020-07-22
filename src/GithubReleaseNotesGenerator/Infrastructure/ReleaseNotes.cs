using Microsoft.Extensions.Logging;
using Octokit;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GithubReleaseNotesGenerator.Infrastructure
{
    public class ReleaseNotes
    {
        private readonly IGitHubClient GitHubClient;
        private readonly ILogger<ReleaseNotes> logger;
        private readonly string breakingLabel;

        public ReleaseNotes(IGitHubClient client, ILogger<ReleaseNotes> logger, string breakingLabel)
        {
            this.GitHubClient = client;
            this.logger = logger;
            this.breakingLabel = breakingLabel;
        }

        public async Task<string> GetReleaseNotesAsync(string owner, string repo, string from, string to, int batchSize)
        {
            var sb = new StringBuilder();

            // Get merged PR's between specified refs
            IReadOnlyList<PullRequestEntry> mergedPulls = await this.GetMergedPullRequestsBetween2Refs(owner, repo, from, to, batchSize).ConfigureAwait(false);

            var contribs = mergedPulls
                .SelectMany(x => x.Contributors.Select(y => y.Login))
                .Distinct()
                .ToList();

            this.logger.LogInformation($"Release Stats: {mergedPulls.Count} pull requests from {contribs.Count} contributors!");

            sb.AppendLine("## Advisories and Breaking Changes");
            FormatPullRequests(mergedPulls.Where(x => x.Labels.Any(y => y.Equals(this.breakingLabel, StringComparison.OrdinalIgnoreCase))), sb);
            sb.AppendLine();

            sb.AppendLine("## Release Notes");
            FormatPullRequests(mergedPulls.Where(x => !x.Labels.Any(y => y.Equals(this.breakingLabel, StringComparison.OrdinalIgnoreCase))), sb);

            return sb.ToString();
        }

        private void FormatPullRequests(IEnumerable<PullRequestEntry> mergedPulls, StringBuilder sb)
        {
            sb.AppendLine();

            if (!mergedPulls.Any())
            {
                sb.AppendLine("- None");
            }

            // Group+Order by MileStone
            IOrderedEnumerable<IGrouping<string, PullRequestEntry>> groupByMilestone = mergedPulls
                .GroupBy(x => x.PullRequest.Milestone == null ? "zzzNone" : x.PullRequest.Milestone.Title)
                .OrderBy(x => x.Key.ToUpper());

            foreach (IGrouping<string, PullRequestEntry> milestoneGroup in groupByMilestone)
            {
                // Milestone Header
                if (groupByMilestone.Count() > 1)
                {
                    sb.AppendFormat("### Milestone: {0}\r\n\r\n", milestoneGroup.Key.Replace("zzzNone", "None"));
                }

                // Group+Order by Label Category
                foreach (IGrouping<string, PullRequestEntry> categoryGroup in milestoneGroup.GroupBy(x => x.Labels?.FirstOrDefault() ?? "other").OrderBy(x => x.Key))
                {
                    // Category Header
                    var textInfo = new CultureInfo("en-US", false).TextInfo;
                    sb.AppendFormat("**{0}**\r\n\r\n", textInfo.ToTitleCase(categoryGroup.Key));

                    // Order by PullRequest Merge date
                    foreach (PullRequestEntry pull in categoryGroup.OrderBy(x => x.PullRequest.MergedAt.Value))
                    {
                        IEnumerable<string> contributors = new[] { pull.PullRequest.User }
                            .Select(x => $"[@{x.Login}]({x.HtmlUrl})")
                            .Concat(
                                pull.Contributors
                                    .Select(x => $"[@{x.Login}]({x.HtmlUrl})")
                                    .OrderBy(x => x))
                            .Distinct();

                        sb.AppendFormat("- {0} - [#{1}]({2}) via {3}",
                            pull.PullRequest.Title,
                            pull.PullRequest.Number,
                            pull.PullRequest.HtmlUrl,
                            string.Join(", ", contributors));

                        sb.AppendLine();
                    }

                    sb.AppendLine();
                }

                sb.AppendLine();
            }
        }

        private async Task<IReadOnlyList<PullRequestEntry>> GetMergedPullRequestsBetween2Refs(string owner, string repository, string fromRef, string toRef, int batchSize)
        {
            // Get commits for the from/to range
            this.logger.LogInformation($"Get commits from {owner}/{repository} in specified range ({fromRef} - {toRef})");

            GitHubCommit fromCommit = await this.GitHubClient.Repository.Commit.Get(owner, repository, fromRef).ConfigureAwait(false);
            GitHubCommit toCommit = await this.GitHubClient.Repository.Commit.Get(owner, repository, toRef).ConfigureAwait(false);

            // Offset minimum date to ensure commit from last tag is not included.
            DateTime from = fromCommit.Commit.Committer.Date.UtcDateTime.AddMinutes(1);
            DateTime to = toCommit.Commit.Committer.Date.UtcDateTime;
            var fromOffset = new DateTimeOffset(from);
            var toOffset = new DateTimeOffset(to);

            // First load the Pull Requests for the date range of our commits
            this.logger.LogInformation($"Find Pull Requests merged in this date range {from.ToLocalTime()} - {to.ToLocalTime()}");

            var pullRequestRequest = new PullRequestRequest
            {
                State = ItemStateFilter.Closed,
            };

            // TODO: There seems to be no way to limit the first query via date. 
            // We have to pull all the PRs and filter in memory. This is slow and will only get slower.
            // I'm not convinced paging will help here.
            IReadOnlyList<PullRequest> allPullRequests =
                await this.GitHubClient
                .PullRequest
                .GetAllForRepository(owner, repository, pullRequestRequest)
                .ConfigureAwait(false);

            PullRequest[] mergedPullRequests =
                allPullRequests
                .Where(x => x.Merged && x.MergedAt >= fromOffset && x.MergedAt <= toOffset)
                .ToArray();

            this.logger.LogInformation($"Found {mergedPullRequests.Length} Pull Requests merged in this date range");
            this.logger.LogInformation("Getting details for each Pull Request");

            // Now load details about the PullRequests using parallel async tasks
            var result = new List<PullRequestEntry>();
            for (int i = 0; i < mergedPullRequests.Length; i += batchSize)
            {
                IEnumerable<PullRequest> batchPullRequests = mergedPullRequests
                    .Skip(i)
                    .Take(batchSize);

                List<Task<PullRequestEntry>> tasks = batchPullRequests.Select(async pull =>
                {
                    // Load the commits.
                    IReadOnlyList<PullRequestCommit> pullRequestCommits = await this.GitHubClient
                    .PullRequest
                    .Commits(owner, repository, pull.Number)
                    .ConfigureAwait(false);

                    // Need to load commits individually to get the author details.
                    // Using ToList runs the queries in parallel.
                    List<Task<GitHubCommit>> pullRequestFullCommitsTask = pullRequestCommits
                    .Select(async x => await this.GitHubClient.Repository.Commit.Get(owner, repository, x.Sha).ConfigureAwait(false))
                    .ToList();

                    GitHubCommit[] pullRequestFullCommits = await Task.WhenAll(pullRequestFullCommitsTask).ConfigureAwait(false);

                    // Extract the distinct users who have commits in the PR
                    IEnumerable<Author> contributorUsers = pullRequestFullCommits
                        .Where(x => x.Author != null && x.Author.Id != 0)
                        .Select(x => x.Author)
                        .DistinctBy(x => x.Login);

                    // Gather everything into a CachedPullRequest object
                    return new PullRequestEntry()
                    {
                        PullRequest = pull,
                        Commits = pullRequestCommits,
                        Contributors = contributorUsers.ToList(),
                        Labels = pull.Labels.Select(x => x.Name).ToList()
                    };
                }).ToList();

                // Collect the results
                result.AddRange(await Task.WhenAll(tasks).ConfigureAwait(false));

                this.logger.LogInformation($"Loaded {result.Count} Pull Requests");
            }

            return result;
        }
    }
}
