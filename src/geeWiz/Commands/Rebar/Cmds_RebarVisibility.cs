using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB.Structure;

namespace geeWiz.Cmds_RebarVisibility
{
    public class RebarSelectHelper
    {
        public static List<Element> GetSelectedOrAll(UIDocument uidoc1)
        {
            Document doc = uidoc1.Document;
            var view = doc.ActiveView;
            ICollection<ElementId> selectedRebarIds = uidoc1.Selection.GetElementIds();
            List<Element> selectedRebars = selectedRebarIds.Select(q => doc.GetElement(q)).ToList();

            if (selectedRebars.OfType<Rebar>().Any() || selectedRebars.OfType<RebarInSystem>().Any())
            {
                return selectedRebars.OfType<Rebar>().Cast<Element>().ToList();
            }
            else
            {
                FilteredElementCollector fec1 = new FilteredElementCollector(doc, view.Id);
                return fec1.OfCategory(BuiltInCategory.OST_Rebar).ToList();
            }
        }
    }
    
    //Set the attributes
    [TransactionAttribute(TransactionMode.Manual)]
    [RegenerationAttribute(RegenerationOption.Manual)]

    public class Cmd_SetUnobscuredInView : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Document doc = commandData.Application.ActiveUIDocument.Document;
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            var view = doc.ActiveView;
            List<Element> rebars = RebarSelectHelper.GetSelectedOrAll(uidoc);
            using (Transaction t1 = new Transaction(doc, "Set Unobscured in View"))
            {
                t1.Start();
                foreach (Element rebar in rebars)
                {
                    if (rebar is RebarInSystem)
                    {
                        RebarInSystem r = (RebarInSystem)rebar;
                 
                        if (!r.IsUnobscuredInView(view))
                        {
                            r.SetUnobscuredInView(view, true);
                        }
  
                    }
                    if (rebar is Rebar)
                    {
                        Rebar r = (Rebar)rebar;

                        if (!r.IsUnobscuredInView(view))
                        {
                            r.SetUnobscuredInView(view, true);
                        }

                    }
                }
                t1.Commit();
            }
            return Result.Succeeded;
        }
    } //class

    //Set the attributes
    [TransactionAttribute(TransactionMode.Manual)]
    [RegenerationAttribute(RegenerationOption.Manual)]

    public class Cmd_SetObscuredInView : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Document doc = commandData.Application.ActiveUIDocument.Document;
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            var view = doc.ActiveView;
            List<Element> rebars = RebarSelectHelper.GetSelectedOrAll(uidoc);
            using (Transaction t2 = new Transaction(doc, "Set Obscured in View"))
            {
                t2.Start();
                foreach (Element rebar in rebars)
                {
                    if (rebar is RebarInSystem)
                    {
                        RebarInSystem r = (RebarInSystem)rebar;

                        if (r.IsUnobscuredInView(view))
                        {
                            r.SetUnobscuredInView(view, false);
                        }

                    }
                    else if (rebar is Rebar)
                    {
                        Rebar r = (Rebar)rebar;

                        if (r.IsUnobscuredInView(view))
                        {
                            r.SetUnobscuredInView(view, false);
                        }

                    }
                }

                t2.Commit();
            }
            return Result.Succeeded;
        }
    } //class


} //namespace
