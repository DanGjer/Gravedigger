using Microsoft.VisualBasic;

namespace Gravedigger;

public class AssistantArgs
{
 
    [Description("List of parameters to grab from the linked model"), ControlData(ToolTip = "")]
    public List<string> ParametersToCopy { get; set; } = new List<string>();

    [CustomRevitAutoFill(typeof(ParameterAutoFillCollector))]
    public string LinkedModel { get; set; } = string.Empty;

public class ParameterAutoFillCollector : IRevitAutoFillCollector<AssistantArgs>
{
    public Dictionary<string, string> Get(UIApplication uiApplication, AssistantArgs args)
    {
        var result = new Dictionary<string, string>();

        try
        {
            // Access to active revit model
            var document = uiApplication.ActiveUIDocument.Document;
            var activeLinks = new FilteredElementCollector(document)
            .OfClass(typeof(RevitLinkInstance))
            .Cast<RevitLinkInstance>()
            .Where(link => link.GetLinkDocument() != null)
            .ToList();

            foreach (var link in activeLinks)
            {
                result.Add(link.Name, link.Name);
            }
        }
        catch (Exception e)
        {
            result.Add(string.Empty, $"Failed to get autofill: {e.Message}");
        }
        if (result == null || !result.Any())
        {
            throw new InvalidOperationException("No linked models found.");
        }

        return result;
    }
}
}