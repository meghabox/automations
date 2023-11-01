using System.ComponentModel.DataAnnotations;

namespace CloudMed.Automations.Core.Options;

public class GitOptions
{
    [Required]
    public string CollectionUri { get; set; } = @"https://dev.azure.com/revint";

    [Required]
    public string ProjectName { get; set; } = "OneRevInt";

    [Required]
    public Guid ProjectId { get; set; } = new Guid("e86f31c2-5c81-418e-9438-05298e9b2774");

    [Required]
    public List<string> RepositoryNames = new() {
        "CM.Models", "CM.Messaging", "CloudMedCore", "CloudMedStorage", "CloudMedClient", "CloudMedAnomalyEngine", //Libraries
        "Auditing Tools App", "CloudMed", "Auditing Tools App V2", "CloudMed.WorkDriver",
        "AzureFunctions",
        "CloudMed.HL7", "CloudMed.Reporting",
        "CloudMedDataAPI",
        "Invoicing", "InvoicingWebJobs",
        "CMTool", "RevIntAdminTool",
        "Service Fabric File Ingestion",
        "KBox.Contract.Services", "KBox.Contract.UI", "KBox.NRE.Services"
    };

    public List<(bool initial, string name)> PipelineProjectNames = new() {
        (true, "CloudMed.CodeStyles Release"),
        (false, "Dev CM.Models Release"),
        (false, "Dev Invoicing Release"),
        (false, "Dev AnalysisJobApi Swagger Client Gen"),
    };
}