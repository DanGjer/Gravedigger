using System.IO;

namespace Gravedigger;

public class GravediggerCommand : IRevitExtension<AssistantArgs>
{
    public IExtensionResult Run(IRevitExtensionContext context, AssistantArgs args, CancellationToken cancellationToken)
    {
        var document = context.UIApplication.ActiveUIDocument?.Document;
        if (document == null)
        {
            return Result.Text.Failed("No active document found.");
        }

        bool targetInSight = false;
        RevitLinkInstance? targetModel = null;

        var linksInDocument = GetActiveLinks(document);

        foreach (var link in linksInDocument)
        {
            if (link.Name.Contains(args.GraveModelName))
            {
                targetInSight = true;
                targetModel = link;
                break;
            }
        }

        if (!targetInSight)
        {
            return Result.Text.Failed($"No active links found with name containing '{args.GraveModelName}'");
        }

        else
        {
            var linkedDoc = targetModel?.GetLinkDocument();
            if (linkedDoc == null)
            {
                return Result.Text.Failed("Failed to access the linked document.");
            }
            
            // Collect elements from linked model
            var elementsInLinkedModel = new FilteredElementCollector(linkedDoc)
                .WhereElementIsNotElementType()
                .ToElements();

            var linkedModelElements = new List<LinkedElementData>();

            foreach (var element in elementsInLinkedModel)
            {
                var elementData = new LinkedElementData
                {
                    UniqueId = element.UniqueId,
                    ElementId = (int)element.Id.Value
                };

                foreach (var paramName in args.ParametersToCopy)
                {
                    var param = element.LookupParameter(paramName);
                    if (param != null && param.HasValue)
                    {
                        string paramValue = string.Empty;
                        
                        switch (param.StorageType)
                        {
                            case StorageType.String:
                                paramValue = param.AsString() ?? string.Empty;
                                break;
                            case StorageType.Integer:
                                paramValue = param.AsInteger().ToString();
                                break;
                            case StorageType.Double:
                                paramValue = param.AsDouble().ToString();
                                break;
                            case StorageType.ElementId:
                                paramValue = param.AsElementId().ToString();
                                break;
                        }
                        
                        elementData.Parameters[paramName] = paramValue;
                    }
                    else
                    {
                        elementData.Parameters[paramName] = "[NOT FOUND OR NO VALUE]";
                    }
                }

                linkedModelElements.Add(elementData);
            }

            // Collect elements from host document
            var elementsInHostModel = new FilteredElementCollector(document)
                .WhereElementIsNotElementType()
                .ToElements();

            var hostModelElements = new List<DocumentElementData>();

            foreach (var element in elementsInHostModel)
            {
                var elementData = new DocumentElementData
                {
                    UniqueId = element.UniqueId,
                    ElementId = (int)element.Id.Value
                };

                foreach (var paramName in args.ParametersToCopy)
                {
                    var param = element.LookupParameter(paramName);
                    if (param != null && param.HasValue)
                    {
                        string paramValue = string.Empty;
                        
                        switch (param.StorageType)
                        {
                            case StorageType.String:
                                paramValue = param.AsString() ?? string.Empty;
                                break;
                            case StorageType.Integer:
                                paramValue = param.AsInteger().ToString();
                                break;
                            case StorageType.Double:
                                paramValue = param.AsDouble().ToString();
                                break;
                            case StorageType.ElementId:
                                paramValue = param.AsElementId().ToString();
                                break;
                        }
                        
                        elementData.Parameters[paramName] = paramValue;
                    }
                    else
                    {
                        elementData.Parameters[paramName] = "[NOT FOUND OR NO VALUE]";
                    }
                }

                hostModelElements.Add(elementData);
            }

            // Match and copy parameters from linked model to host model
            var linkedDict = linkedModelElements.ToDictionary(e => e.UniqueId);
            int matchCount = 0;
            int updatedParameterCount = 0;

            foreach (var hostElement in hostModelElements)
            {
                if (linkedDict.TryGetValue(hostElement.UniqueId, out var linkedElement))
                {
                    matchCount++;
                    bool hasUpdates = false;
                    
                    foreach (var linkedParam in linkedElement.Parameters)
                    {
                        if (hostElement.Parameters.ContainsKey(linkedParam.Key))
                        {
                            // Only update if the linked element has a valid value
                            if (linkedParam.Value != "[NOT FOUND OR NO VALUE]")
                            {
                                hostElement.Parameters[linkedParam.Key] = linkedParam.Value;
                                updatedParameterCount++;
                                hasUpdates = true;
                            }
                        }
                    }
                    
                    if (hasUpdates)
                    {
                        hostElement.WasUpdated = true;
                        linkedElement.WasUpdated = true;
                    }
                }
            }

            // Write updated parameters back to Revit elements
            int elementsWritten = 0;
            int parametersWritten = 0;

            using (var transaction = new Transaction(document, "Update Parameters from Linked Model"))
            {
                transaction.Start();

                foreach (var hostData in hostModelElements.Where(e => e.WasUpdated))
                {
                    var element = document.GetElement(hostData.UniqueId);
                    if (element != null)
                    {
                        elementsWritten++;

                        foreach (var paramEntry in hostData.Parameters)
                        {
                            if (paramEntry.Value != "[NOT FOUND OR NO VALUE]")
                            {
                                var param = element.LookupParameter(paramEntry.Key);
                                if (param != null && !param.IsReadOnly)
                                {
                                    try
                                    {
                                        switch (param.StorageType)
                                        {
                                            case StorageType.String:
                                                param.Set(paramEntry.Value);
                                                parametersWritten++;
                                                break;
                                            case StorageType.Integer:
                                                if (int.TryParse(paramEntry.Value, out int intValue))
                                                {
                                                    param.Set(intValue);
                                                    parametersWritten++;
                                                }
                                                break;
                                            case StorageType.Double:
                                                if (double.TryParse(paramEntry.Value, out double doubleValue))
                                                {
                                                    param.Set(doubleValue);
                                                    parametersWritten++;
                                                }
                                                break;
                                        }
                                    }
                                    catch
                                    {
                                        // Skip parameters that fail to set
                                    }
                                }
                            }
                        }
                    }
                }

                transaction.Commit();
            }


            // Debug: Write to file
            var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var outputPath = Path.Combine(desktopPath, "GravediggerDebug.txt");
            
            using (var writer = new StreamWriter(outputPath))
            {
                writer.WriteLine($"Total Elements in Linked Model: {linkedModelElements.Count}");
                writer.WriteLine($"Total Elements in Host Model: {hostModelElements.Count}");
                writer.WriteLine($"Matched Elements (by UniqueId): {matchCount}");
                writer.WriteLine($"Parameters Updated in Memory: {updatedParameterCount}");
                writer.WriteLine($"Elements Written to Revit: {elementsWritten}");
                writer.WriteLine($"Parameters Written to Revit: {parametersWritten}");
                writer.WriteLine($"Timestamp: {DateTime.Now}");
                writer.WriteLine(new string('-', 80));
                
                writer.WriteLine("\n=== LINKED MODEL ELEMENTS ===\n");
                foreach (var data in linkedModelElements)
                {
                    writer.WriteLine($"\nElementId: {data.ElementId}");
                    writer.WriteLine($"UniqueId: {data.UniqueId}");
                    writer.WriteLine($"WasUpdated: {data.WasUpdated}");
                    foreach (var param in data.Parameters)
                    {
                        writer.WriteLine($"  {param.Key}: {param.Value}");
                    }
                }

                writer.WriteLine("\n\n=== HOST MODEL ELEMENTS ===\n");
                foreach (var data in hostModelElements)
                {
                    writer.WriteLine($"\nElementId: {data.ElementId}");
                    writer.WriteLine($"UniqueId: {data.UniqueId}");
                    writer.WriteLine($"WasUpdated: {data.WasUpdated}");
                    foreach (var param in data.Parameters)
                    {
                        writer.WriteLine($"  {param.Key}: {param.Value}");
                    }
                }
            }

        return Result.Text.Succeeded($"Parameter copy completed. Matched: {matchCount}, Elements written: {elementsWritten}, Parameters written: {parametersWritten}");
        }
        
    
    }

    private List<RevitLinkInstance> GetActiveLinks(Document document)
    {
        var activeLinks = new FilteredElementCollector(document)
            .OfClass(typeof(RevitLinkInstance))
            .Cast<RevitLinkInstance>()
            .Where(link => link.GetLinkDocument() != null)
            .ToList();

        return activeLinks;
    }

}

public class LinkedElementData
{
    public string UniqueId { get; set; }
    public int ElementId { get; set; }
    public Dictionary<string, string> Parameters { get; set; }
    public bool WasUpdated { get; set; }

    public LinkedElementData()
    {
        UniqueId = string.Empty;
        ElementId = 0;
        Parameters = new Dictionary<string, string>();
        WasUpdated = false;
    }
}

public class DocumentElementData
{
    public string UniqueId { get; set; }
    public int ElementId { get; set; }
    public Dictionary<string, string> Parameters { get; set; }
    public bool WasUpdated { get; set; }

    public DocumentElementData()
    {
        UniqueId = string.Empty;
        ElementId = 0;
        Parameters = new Dictionary<string, string>();
        WasUpdated = false;
    }
}