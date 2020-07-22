using Microsoft.AspNetCore.Html;
using System.ComponentModel.DataAnnotations;

namespace GithubReleaseNotesGenerator.Models
{
    public class ReleaseNotesViewModel
    {
        [Required]
        [Display(Name = "Person Access Token")]
        public string PatToken { get; set; }

        [Required]
        [Display(Name = "Repository Owner")]
        public string RepositoryOwner { get; set; }

        [Required]
        [Display(Name = "Repository Name")]
        public string RepositoryName { get; set; }

        [Required]
        public string From { get; set; }

        [Required]
        public string To { get; set; }

        [Display(Name = "Breaking Identifier Label")]
        public string BreakingLabel { get; set; }

        public string RawNotes { get; set; }

        public HtmlString FormattedNotes { get; set; }
    }
}
