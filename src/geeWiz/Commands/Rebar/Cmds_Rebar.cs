// Revit API
using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using geeWiz.RebarUtils;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using System.Collections.Generic;
using System.Linq;

// geeWiz
using gFil = geeWiz.Utilities.File_Utils;

namespace geeWiz.Cmds_Rebar
{
    #region Cmd_Renumber

    /// <summary>
    /// Changes Rebar Number by swapping two numbers safely or renaming if target not taken
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    [RegenerationAttribute(RegenerationOption.Manual)]
    public class Cmd_Renumber : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, Autodesk.Revit.DB.ElementSet elements)
        {
            var app = commandData.Application;
            Autodesk.Revit.DB.Document doc = commandData.Application.ActiveUIDocument.Document;
            UIDocument uidoc = commandData.Application.ActiveUIDocument;

            NumberingSchema schema = NumberingSchema.GetNumberingSchema(doc, NumberingSchemaTypes.StructuralNumberingSchemas.Rebar);
            var partitions = schema.GetNumberingSequences();
            if (!partitions.Any())
            {
                message = "No partitions in the document";
                return Result.Failed;
            }

            Dictionary<string, string[]> rebarNumbers = new Dictionary<string, string[]>();
            foreach (string p in partitions)
            {
                rebarNumbers[p] = GetRebarNumbers(doc, p);
            }
            string[] sortedPartitions = partitions.OrderBy(q => q).ToArray();

            using (FormRenumber form1 = new FormRenumber(doc, sortedPartitions, rebarNumbers))
            {
                // ✅ Prepopulate if a single Rebar is selected
                var selectedIds = uidoc.Selection.GetElementIds();
                if (selectedIds.Count == 1)
                {
                    Element selectedElement = doc.GetElement(selectedIds.First());
                    if (selectedElement is Rebar selectedRebar)
                    {
                        string selectedPartition = selectedRebar.LookupParameter("Partition")?.AsString();
                        string rebarNumberStr = selectedRebar.LookupParameter("Rebar Number")?.AsString();

                        if (!string.IsNullOrEmpty(selectedPartition) &&
                            !string.IsNullOrEmpty(rebarNumberStr) &&
                            int.TryParse(rebarNumberStr, out int rebarNumber) &&
                            partitions.Contains(selectedPartition))
                        {
                            form1.partition = selectedPartition;
                            form1.fromNumber = rebarNumber;
                        }
                    }
                }

                form1.ShowDialog();
                if (form1.DialogResult == System.Windows.Forms.DialogResult.Cancel) return Result.Cancelled;

                string partition = form1.partition;
                int fromNumber = form1.fromNumber;
                int toNumber = form1.toNumber;

                if (fromNumber == toNumber)
                {
                    message = "From and To numbers are the same.";
                    return Result.Cancelled;
                }

                string[] currentNumbers = rebarNumbers[partition];

                // If toNumber does NOT exist, just rename fromNumber -> toNumber directly
                if (!currentNumbers.Contains(toNumber.ToString()))
                {
                    IList<Autodesk.Revit.DB.ElementId> changedIds = new List<Autodesk.Revit.DB.ElementId>();
                    using (Transaction t = new Transaction(doc, "Rename rebar number"))
                    {
                        t.Start();
                        var ids = schema.ChangeNumber(partition, fromNumber, toNumber);
                        changedIds = changedIds.Concat(ids).ToList();
                        t.Commit();
                    }

                    message = $"Changed rebar number {fromNumber} to {toNumber}. Affected IDs: " +
                              string.Join(", ", changedIds.Select(id => id.ToString()));
                    uidoc.Selection.SetElementIds(changedIds.ToList());
                }
                else
                {
                    // Swap needed — safe 3-step process using temp number
                    int tempNumber = GetUnusedRebarNumber(currentNumbers);
                    IList<Autodesk.Revit.DB.ElementId> changedIds = new List<Autodesk.Revit.DB.ElementId>();

                    using (Transaction t1 = new Transaction(doc, "Step 1: Temp renumber"))
                    {
                        t1.Start();
                        var ids1 = schema.ChangeNumber(partition, fromNumber, tempNumber);
                        changedIds = changedIds.Concat(ids1).ToList();
                        t1.Commit();
                    }

                    using (Transaction t2 = new Transaction(doc, "Step 2: Swap renumber"))
                    {
                        t2.Start();
                        var ids2 = schema.ChangeNumber(partition, toNumber, fromNumber);
                        changedIds = changedIds.Concat(ids2).ToList();
                        t2.Commit();
                    }

                    using (Transaction t3 = new Transaction(doc, "Step 3: Final renumber"))
                    {
                        t3.Start();
                        var ids3 = schema.ChangeNumber(partition, tempNumber, toNumber);
                        changedIds = changedIds.Concat(ids3).ToList();
                        t3.Commit();
                    }

                    message = $"Swapped rebar numbers {fromNumber} and {toNumber}. Affected IDs: " +
                              string.Join(", ", changedIds.Select(id => id.ToString()));
                    uidoc.Selection.SetElementIds(changedIds.ToList());
                }
            }

            return Result.Succeeded;
        }

        public static string[] GetRebarNumbers(Document doc, string partition)
        {
            NumberingSchema schema = NumberingSchema.GetNumberingSchema(doc, NumberingSchemaTypes.StructuralNumberingSchemas.Rebar);
            IList<IntegerRange> ranges = schema.GetNumbers(partition);
            List<int[]> list = new List<int[]>();
            foreach (var range in ranges)
            {
                list.Add(Enumerable.Range(range.Low, range.High - range.Low + 1).ToArray());
            }
            int[] numbers = list.SelectMany(i => i).ToArray();
            return numbers.Select(i => i.ToString()).ToArray();
        }

        private static int GetUnusedRebarNumber(string[] existingNumbers)
        {
            var existing = new HashSet<int>(existingNumbers.Select(int.Parse));
            int temp = 99999;
            while (existing.Contains(temp))
            {
                temp--;
            }
            return temp;
        }
    }

    #endregion

    #region Cmd_FreezeRebar

    /// <summary>
    /// Changes Rebar Number by swapping two numbers safely or renaming if target not taken
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class Cmd_FreezeRebar : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            // Filter for all rebar elements
            FilteredElementCollector collector = new FilteredElementCollector(doc)
                .OfClass(typeof(Rebar));

            int copiedCount = 0;

            using (Transaction tx = new Transaction(doc, "Copy Rebar Number to Schedule Mark"))
            {
                tx.Start();

                foreach (Rebar rebar in collector)
                {
                    Parameter rebarNumberParam = rebar.LookupParameter("Rebar Number");
                    Parameter scheduleMarkParam = rebar.LookupParameter("Schedule Mark");

                    if (rebarNumberParam != null)
                    {
                        string rebarNumber = rebarNumberParam.AsString();
                        if (!string.IsNullOrEmpty(rebarNumber))
                        {
                            scheduleMarkParam.Set(rebarNumber);
                            copiedCount++;
                        }
                    }
                }

                tx.Commit();
            }

            TaskDialog.Show("Done", $"Copied 'Rebar Number' to 'Schedule Mark' for {copiedCount} elements.");
            return Result.Succeeded;
        }
    }
    #endregion
}


