using GithubReleaseNotesGenerator.Infrastructure;
using GithubReleaseNotesGenerator.Models;
using Markdig;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Octokit;
using System.Threading.Tasks;

namespace GithubReleaseNotesGenerator.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<ReleaseNotes> logger;

        public HomeController(ILogger<ReleaseNotes> logger)
        {
            this.logger = logger;
        }

        public IActionResult Index()
        {
            return View("Index", new ReleaseNotesViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(ReleaseNotesViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("Index", model);
            }

            string githubToken = model.PatToken;
            string owner = model.RepositoryOwner;
            string repo = model.RepositoryName;
            string from = model.From;
            string to = model.To;
            string breaking = model.BreakingLabel ?? "breaking";

            var githubCreds = new Credentials(githubToken);

            var githubClient = new GitHubClient(new ProductHeaderValue("GithubReleaseNotesGenerator"))
            {
                Credentials = githubCreds
            };

            var releaseNotes = new ReleaseNotes(githubClient, logger, breaking);
            model.RawNotes = await releaseNotes.GetReleaseNotesAsync(owner, repo, from, to, 10).ConfigureAwait(false);
            model.FormattedNotes = new HtmlString(Markdown.ToHtml(model.RawNotes));

            return View("Index", model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
