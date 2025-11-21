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

        var targetModel = new FilteredElementCollector(document)
            .OfClass(typeof(RevitLinkInstance))
            .Cast<RevitLinkInstance>()
            .FirstOrDefault(link => link.Name.Equals(args.LinkedModel, StringComparison.OrdinalIgnoreCase));

        if (targetModel == null)
        {
            return Result.Text.Failed($"Linked model '{args.LinkedModel}' not found.");
        }

        var targetDocument = targetModel.GetLinkDocument();

        if (targetDocument == null)
        {
            return Result.Text.Failed("Failed to access the linked document.");
        }

        if (args.ParametersToCopy == null || !args.ParametersToCopy.Any())
        {
            return Result.Text.Failed("No parameters specified to copy.");
        }

        // Collect elements from linked model
        var elementsInLinkedModel = new FilteredElementCollector(targetDocument)
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

        return Result.Text.Succeeded($"Parameter copy completed. Matched: {matchCount}, Elements written: {elementsWritten}, Parameters written: {parametersWritten}");
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