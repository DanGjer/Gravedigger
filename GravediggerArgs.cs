using Microsoft.VisualBasic;

namespace Gravedigger;

public class AssistantArgs
{
    [Description("Grave model name"), ControlData(ToolTip = "")]
    public string GraveModelName { get; set; } = "";
    
    [Description("List of parameters to copy"), ControlData(ToolTip = "")]
    public List<string> ParametersToCopy { get; set; } = new List<string>();
}