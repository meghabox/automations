using System.ComponentModel.DataAnnotations;

namespace CloudMed.Automations.Core.Options;

public class AzureAdOptions
{
    [Required]
    public string PersonalAccessToken { get; set; }
}
