namespace CloudMed.Automations.Core.Models;

public class WorkItemUpdateResponse
{
    public int Code { get; set; }
    public Dictionary<string, string> Headers { get; set; }
    public string Body { get; set; }
}
