# Gravedigger

## Description

Gravedigger is a Revit extension designed to recover lost or accidentally deleted parameter data from backup models. When element parameters have been cleared, overwritten, or removed—and standard undo methods are insufficient—Gravedigger matches elements between your current model and a linked backup version by their unique identifiers and restores the specified parameter values. This tool is essential for data recovery scenarios where transaction history doesn't extend back far enough or when changes have been saved and closed.

## Configuration

### Parameters to Copy

- **Parameters to Copy**: A list of parameter names that you want to recover from the linked backup model. Enter the exact parameter names as they appear in your Revit project (e.g., "Comments", "Mark", "drofus_occurrence_id"). These can be built-in Revit parameters, shared parameters, or project parameters. The extension will search for these parameters on every element and copy their values from the backup model to your current model where matches are found.
  - **Format**: Enter parameter names one per line or as a comma-separated list
  - **Case Sensitivity**: Parameter names must match exactly as they appear in Revit
  - **Tip**: Focus on the specific parameters you need to recover rather than listing all parameters, as this improves performance

### Linked Model

- **Linked Model**: Select the Revit link that contains the backup version of your model with the parameter data you want to recover. This dropdown automatically populates with all currently loaded Revit links in your active document. The backup model should be an earlier version of your project from before the data loss occurred.
  - **Requirement**: The linked model must be actively loaded (not unloaded) in your current project
  - **Best Practice**: Use a backup from just before the data loss to ensure element matching accuracy
  - **Note**: The extension matches elements by Unique ID, so the backup must contain the same elements (not recreated elements)

## Functionality

### Description

Gravedigger performs a systematic data recovery process through the following steps:

1. **Validation**: Verifies that an active Revit document is open and the specified linked model exists and is accessible
2. **Data Collection from Backup**: Scans all elements in the linked backup model and extracts values for the specified parameters, storing each element's data with its Unique ID
3. **Data Collection from Current Model**: Scans all elements in your active model and catalogs their current parameter values
4. **Element Matching**: Matches elements between the backup and current model using Revit's Unique ID system, which persists across model versions
5. **Parameter Comparison**: Identifies which elements have matching Unique IDs and determines which parameters need to be restored
6. **Data Restoration**: Opens a transaction and writes the recovered parameter values back to the corresponding elements in your active model
7. **Reporting**: Provides a summary showing how many elements were matched and how many parameters were successfully restored

The extension handles different parameter types automatically (text, integers, numbers, element IDs) and skips read-only parameters or parameters that cannot be set. If a parameter doesn't exist on an element or has no value in the backup, it will be noted but won't cause the operation to fail.

### How to Use

1. **Prepare Your Backup Model**
   - Locate a backup file of your project from before the data loss occurred
   - Open your current Revit project (the one with missing/incorrect data)
   - Link the backup model into your current project using Revit's "Link Revit" command
   - Ensure the linked model is loaded (not unloaded or temporarily hidden)

2. **Configure Gravedigger**
   - Open Assistant and add the Gravedigger extension to your sequence
   - In the **Linked Model** dropdown, select the name of your backup link
   - In the **Parameters to Copy** field, enter the names of parameters you need to recover
     - Example: If "Comments" and "Mark" were cleared, enter both names
     - Make sure parameter names are spelled exactly as they appear in Revit

3. **Run the Extension**
   - Execute the Assistant sequence containing Gravedigger
   - The extension will process all elements in both models—this may take a few minutes for large projects
   - Monitor the progress in Assistant's output window

4. **Verify Results**
   - Review the completion message showing:
     - Number of elements matched between models
     - Number of elements updated
     - Number of individual parameters written
   - Spot-check several elements to confirm parameter values were restored correctly
   - If some elements weren't updated, verify they exist in both the backup and current model with the same Unique ID

### Visual Aids

[Note: Screenshots showing the following would be helpful:
- The Assistant interface with Gravedigger's configuration panel
- The "Linked Model" dropdown populated with available links
- The "Parameters to Copy" field with example parameter names entered
- A before/after comparison of element properties showing restored parameter values
- The success message with match statistics]

## Troubleshooting

### Issue 1: "Linked model '[name]' not found"
- **Causes**: 
  - The linked model was unloaded after configuration
  - The model name was changed or the link was removed
  - The wrong linked model name was selected or entered
- **Solution**: 
  - Open Manage Links in Revit and verify the link status
  - Reload any unloaded links
  - If the link was removed, re-link the backup model
  - Return to Gravedigger configuration and select the correct link from the dropdown
- **Resources**: Revit Help - "About Linked Revit Models"

### Issue 2: "No parameters specified to copy"
- **Causes**: 
  - The Parameters to Copy field was left empty
  - Parameter names were entered incorrectly and cleared during validation
- **Solution**: 
  - Verify the exact spelling of parameter names in your Revit model
  - Open an element's properties to confirm the parameter name exactly as shown
  - Enter at least one valid parameter name in the configuration
  - Ensure there are no extra spaces or typos in parameter names
- **Resources**: Check your project's shared parameters file or project parameters list

### Issue 3: Fewer parameters restored than expected
- **Causes**: 
  - Elements were recreated rather than modified (different Unique IDs)
  - Parameters are read-only or controlled by formulas/constraints
  - The parameter didn't exist on certain element categories
  - The backup model itself had missing parameter values
- **Solution**: 
  - Verify that elements in the current model are the same instances as in the backup (not deleted and recreated)
  - Check if parameters are editable by trying to manually change them
  - Review the backup model to confirm it contains the data you're trying to recover
  - For recreated elements, consider using other identification methods (manual matching by properties)
- **Resources**: Check Revit's Unique ID persistence documentation

### Issue 4: "Failed to access the linked document"
- **Causes**: 
  - The linked model file is missing or the path is broken
  - The linked model was not opened/loaded with the host model
  - File permissions prevent accessing the linked file
- **Solution**: 
  - Use Manage Links to reload or relink the backup file
  - Ensure the backup file exists at its specified location
  - Check that you have read permissions for the linked file
  - If the file is on a network, verify network connectivity
- **Resources**: Revit Help - "Manage Links Dialog"

### Issue 5: Performance is slow with large models
- **Causes**: 
  - Processing thousands of elements with multiple parameters takes time
  - Complex model geometry or many linked files slow down the operation
- **Solution**: 
  - Be selective with Parameters to Copy—only include what you need
  - Consider isolating element categories if only specific types need recovery
  - Close unnecessary views and applications to free up memory
  - Run during off-hours for very large projects
  - Process in batches if possible (though this extension processes all elements at once)
- **Resources**: Standard Revit performance optimization guidelines

## FAQ

- **Q: When should I use Gravedigger instead of standard undo?**
  - **A:** Use Gravedigger when standard undo (Ctrl+Z) won't work because: (1) The changes were made in a previous session and the model has been closed and reopened, (2) Too many actions have occurred since the data loss and undo history doesn't go back far enough, (3) The changes were made by multiple people and synced through a central model, or (4) You need to selectively recover specific parameters without undoing other work.

- **Q: What happens if an element exists in my current model but not in the backup?**
  - **A:** Gravedigger only updates elements that exist in both models with matching Unique IDs. New elements created after the backup was made won't be affected—they'll simply remain unchanged. The tool reports how many elements were matched, so you'll know if elements were skipped.

- **Q: Can I recover parameters from multiple backup versions?**
  - **A:** Each run of Gravedigger works with one linked model at a time. If you need to recover different parameters from different backup versions, link multiple backups and run Gravedigger separately for each, specifying different linked models and parameters each time.

- **Q: Will this work if elements were deleted and recreated?**
  - **A:** No. Gravedigger matches elements using Revit's Unique ID system. When an element is deleted and recreated, it receives a new Unique ID, so the tool cannot match it to the backup. This works only for existing elements that had their parameters modified, not for recreated geometry.

- **Q: What types of parameters can be recovered?**
  - **A:** Gravedigger can recover text (string), integer, number (double), and Element ID parameters. It works with built-in Revit parameters, shared parameters, and project parameters. Read-only parameters and calculated parameters cannot be modified and will be skipped automatically.

- **Q: Does this modify the linked backup model?**
  - **A:** No. Gravedigger only reads data from the linked backup model and writes to your current active model. The backup link remains completely unchanged.

- **Q: How do I know which parameters to include in the Parameters to Copy list?**
  - **A:** Select an affected element in your model, open its Properties panel, and identify which parameters are missing or have incorrect values. Only include the specific parameters that need recovery—this improves performance and makes the results easier to verify.

## Resources

- [Assistant Platform Documentation](https://assistant-docs.link) - Complete guide to using Assistant extensions
- [Revit API - Element Unique ID](https://www.revitapidocs.com) - Technical details about Unique ID matching
- [Managing Revit Links](https://help.autodesk.com/view/RVT/ENU/?guid=GUID-linking-revit-models) - Official Autodesk guidance on linking models
- Related Extensions:
  - Parameter Transfer Tools - For copying parameters between different projects
  - Data Validation Extensions - For identifying missing parameter data before it's too late

## Support

For assistance or to report issues:

- **Extension Developer**: DBGJ
- **Issue Tracking**: Contact your BIM Manager or Assistant administrator
- **Community Resources**: Check your organization's internal documentation for Assistant extension support procedures
- **Technical Support**: For bugs or unexpected behavior, provide:
  - Revit version being used
  - Number of elements in both current and linked models
  - Names of parameters being copied
  - Complete error message if any

## Version History

- **Version 0.0.2 - November 2025**
  - Current release
  - Core functionality: Parameter recovery from linked backup models
  - Supports all standard parameter types (String, Integer, Double, ElementId)
  - Automatic element matching via Unique ID
  - Comprehensive error handling and reporting
  - Smart parameter validation (skips read-only and invalid parameters)

- **Version 0.0.1**
  - Initial development release

---

*This documentation was generated based on the extension's code structure and is designed for engineers, architects, and BIM coordinators using the Assistant automation platform.*
