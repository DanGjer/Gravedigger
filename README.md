# Gravedigger

A Revit/COWI Tools extension for recovering lost parameter data from backup models.

## Purpose

Gravedigger helps you recover parameter values that were accidentally deleted, cleared, or lost in your Revit model. If you've made changes that you can't undo through normal means (transaction history, undo, etc.), this tool lets you restore data from an earlier version of your model.

## How It Works

1. **Link the backup**: Load an earlier version of your model (before the data loss) as a linked model into your current project
2. **Specify parameters**: Tell Gravedigger which parameters you want to recover
3. **Run the tool**: Gravedigger matches elements between your current model and the linked backup using their Unique IDs
4. **Restore data**: Parameter values are copied from the linked backup to the corresponding elements in your active model

## Use Cases

- Accidentally deleted a parameter and lost all its data
- Blanked out parameter values by mistake
- Need to restore data from before a problematic change
- Transaction history doesn't go back far enough to undo the error

## How to Use

1. Link your backup model into the current project
2. Launch Gravedigger
3. Select the linked model by name
4. Specify which parameters to copy (e.g., "drofus_occurrence_id")
5. Click Run

The tool will match elements by Unique ID and copy the specified parameter values from the backup to your active model.