//Copyright 2012 Ian Keough

//Licensed under the Apache License, Version 2.0 (the "License");
//you may not use this file except in compliance with the License.
//You may obtain a copy of the License at

//http://www.apache.org/licenses/LICENSE-2.0

//Unless required by applicable law or agreed to in writing, software
//distributed under the License is distributed on an "AS IS" BASIS,
//WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//See the License for the specific language governing permissions and
//limitations under the License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.DB.Events;  //MDJ - i think this is needed for DMU stuff
using Autodesk.Revit.DB.Analysis; //MDJ  - added for spatialfeildmanager access
using Dynamo.Elements;
using Dynamo.Controls;
using System.IO;

namespace Dynamo.Utilities
{
    class dynUtils
    {
        /// <summary>
        /// Creates a sketch plane by projecting one point's z coordinate down to the other's z coordinate.
        /// </summary>
        /// <param name="app"></param>
        /// <param name="doc"></param>
        /// <param name="pt1">The start point</param>
        /// <param name="pt2">The end point</param>
        /// <returns></returns>
        public static SketchPlane CreateSketchPlaneForModelCurve(UIApplication app, UIDocument doc, 
            XYZ pt1, XYZ pt2)
        {
            XYZ v1, v2, norm;

            if (pt1.X == pt2.X && pt1.Y == pt2.Y)
            {
                //this is a vertical line
                //make the other axis 
                v1 = (pt2 - pt1).Normalize();
                v2 = ((new XYZ(pt1.X, pt1.Y + 1.0, pt1.Z)) - pt1).Normalize();
                norm = v1.CrossProduct(v2);
            }
            else if (Math.Abs(pt2.Z - pt1.Z) > .00000001)
            {
                //flatten in the z direction
                v1 = (pt2 - pt1).Normalize();
                v2 = ((new XYZ(pt2.X, pt2.Y, pt1.Z)) - pt1).Normalize();
                norm = v1.CrossProduct(v2);
            }
            else if (Math.Abs(pt2.Y - pt1.Y) > .00000001)
            {
                //flatten in the y direction
                v1 = (pt2 - pt1).Normalize();
                v2 = ((new XYZ(pt2.X, pt1.Y, pt2.Z)) - pt1).Normalize();
                norm = v1.CrossProduct(v2);
            }
            else
            {
                //flatten in the x direction
                v1 = (pt2 - pt1).Normalize();
                v2 = ((new XYZ(pt1.X, pt2.Y, pt2.Z)) - pt1).Normalize();
                norm = v1.CrossProduct(v2);
            }
            Plane p = app.Application.Create.NewPlane(norm, pt1);

            SketchPlane sp = doc.Document.Create.NewSketchPlane(p);
            return sp;
        }
    }

    public class dynElementSettings
	{
	    Autodesk.Revit.UI.UIApplication revit;
	    Autodesk.Revit.UI.UIDocument doc;

	    Level defaultLevel;
	    DynamoWarningSwallower warningSwallower;
	    dynBench bench;
	    Dynamo.Controls.DragCanvas workbench;
	    dynCollection dynColl;
	    Transaction trans;
        TextWriter tw;
        Element spatialFieldManagerUpdated;
        Transaction subTrans;

        private static dynElementSettings sharedInstance;

        LinearGradientBrush errorBrush;
        LinearGradientBrush activeBrush;
        LinearGradientBrush selectedBrush;
        LinearGradientBrush deadBrush;

        //colors taken from:
        //http://cloford.com/resources/colours/500col.htm
        System.Windows.Media.Color colorGreen1 = System.Windows.Media.Color.FromRgb(193, 255, 193);
        System.Windows.Media.Color colorGreen2 = System.Windows.Media.Color.FromRgb(155, 250, 155);
        System.Windows.Media.Color colorRed1 = System.Windows.Media.Color.FromRgb(255, 64, 64);
        System.Windows.Media.Color colorRed2 = System.Windows.Media.Color.FromRgb(205, 51, 51);
        System.Windows.Media.Color colorOrange1 = System.Windows.Media.Color.FromRgb(255, 193, 37);
        System.Windows.Media.Color colorOrange2 = System.Windows.Media.Color.FromRgb(238, 180, 34);
        System.Windows.Media.Color colorGray1 = System.Windows.Media.Color.FromRgb(220, 220, 220);
        System.Windows.Media.Color colorGray2 = System.Windows.Media.Color.FromRgb(192, 192, 192);

        public Element SpatialFieldManagerUpdated
        {
            get { return spatialFieldManagerUpdated; }
            set{ spatialFieldManagerUpdated = value;}
        }

	    public Autodesk.Revit.UI.UIApplication Revit
	    {
	        get { return revit; }
            set { revit = value; }
	    }
	    public Autodesk.Revit.UI.UIDocument Doc
	    {
	        get { return doc; }
            set { doc = value; }
	    }
	    public Level DefaultLevel
	    {
	        get { return defaultLevel; }
            set { defaultLevel = value; }
	    }
	    public DynamoWarningSwallower WarningSwallower
	    {
	        get { return warningSwallower; }
            set { warningSwallower = value; }
	    }
	    public Dynamo.Controls.DragCanvas Workbench
	    {
	        get{return workbench;}
	        set{workbench = value;}
	    }
	    public dynCollection Collection
	    {
	        get { return dynColl; }
	        set { dynColl = value; }
	    }
	    public Transaction MainTransaction
	    {
	        get { return trans; }
            set { trans = value; }
	    }
        public Transaction SubTransaction
        {
            get { return subTrans; }
            set { subTrans = value; }
        }
        public LinearGradientBrush ErrorBrush
        {
            get { return errorBrush; }
            set { errorBrush = value; }
        }
        public LinearGradientBrush ActiveBrush
        {
            get { return activeBrush; }
            set { activeBrush = value; }
        }
        public LinearGradientBrush SelectedBrush
        {
            get { return selectedBrush; }
            set { selectedBrush = value; }
        }
        public LinearGradientBrush DeadBrush
        {
            get { return deadBrush; }
            set { deadBrush = value; }
        }
        public dynBench Bench
        {
            get { return bench; }
            set { bench = value; }
        }
        public TextWriter Writer
        {
            get { return tw; }
            set { tw = value; }
        }
        public static dynElementSettings SharedInstance
        {
            get 
            { 
                if(sharedInstance == null)
                {
                    sharedInstance = new dynElementSettings();
                    sharedInstance.SetupBrushes();
                }
                return sharedInstance;
            }
        }

        void SetupBrushes()
        {

            sharedInstance.errorBrush = new LinearGradientBrush();
            sharedInstance.errorBrush.StartPoint = new System.Windows.Point(0.5, 0);
            sharedInstance.errorBrush.EndPoint = new System.Windows.Point(0.5, 1);
            sharedInstance.errorBrush.GradientStops.Add(new GradientStop(colorRed1, 0.0));
            sharedInstance.errorBrush.GradientStops.Add(new GradientStop(colorRed2, .25));
            sharedInstance.errorBrush.GradientStops.Add(new GradientStop(colorRed2, 1.0));

            sharedInstance.activeBrush = new LinearGradientBrush();
            sharedInstance.activeBrush.StartPoint = new System.Windows.Point(0.5, 0);
            sharedInstance.activeBrush.EndPoint = new System.Windows.Point(0.5, 1);
            sharedInstance.activeBrush.GradientStops.Add(new GradientStop(colorOrange1, 0.0));
            sharedInstance.activeBrush.GradientStops.Add(new GradientStop(colorOrange2, .25));
            sharedInstance.activeBrush.GradientStops.Add(new GradientStop(colorOrange2, 1.0));

            sharedInstance.selectedBrush = new LinearGradientBrush();
            sharedInstance.selectedBrush.StartPoint = new System.Windows.Point(0.5, 0);
            sharedInstance.selectedBrush.EndPoint = new System.Windows.Point(0.5, 1);
            sharedInstance.selectedBrush.GradientStops.Add(new GradientStop(colorGreen1, 0.0));
            sharedInstance.selectedBrush.GradientStops.Add(new GradientStop(colorGreen2, .25));
            sharedInstance.selectedBrush.GradientStops.Add(new GradientStop(colorGreen2, 1.0));

            sharedInstance.deadBrush = new LinearGradientBrush();
            sharedInstance.deadBrush.StartPoint = new System.Windows.Point(0.5, 0);
            sharedInstance.deadBrush.EndPoint = new System.Windows.Point(0.5, 1);
            sharedInstance.deadBrush.GradientStops.Add(new GradientStop(colorGray1, 0.0));
            sharedInstance.deadBrush.GradientStops.Add(new GradientStop(colorGray2, .25));
            sharedInstance.deadBrush.GradientStops.Add(new GradientStop(colorGray2, 1.0));

        }
	}

    public class DynamoWarningSwallower : IFailuresPreprocessor
	{
	    public FailureProcessingResult PreprocessFailures(
	        FailuresAccessor a)
	    {
	        // inside event handler, get all warnings
	
	        IList<FailureMessageAccessor> failures
	            = a.GetFailureMessages();
	
	        foreach (FailureMessageAccessor f in failures)
	        {
	            // check failure definition ids
	            // against ones to dismiss:
	
	            FailureDefinitionId id
	                = f.GetFailureDefinitionId();
	
	            //      BuiltInFailures.JoinElementsFailures.CannotKeepJoined == id ||
                //    BuiltInFailures.JoinElementsFailures.CannotJoinElementsStructural == id ||
	            //    BuiltInFailures.JoinElementsFailures.CannotJoinElementsStructuralError == id ||
	            //    BuiltInFailures.JoinElementsFailures.CannotJoinElementsWarn == id
	
	            if (BuiltInFailures.InaccurateFailures.InaccurateLine == id ||
	                BuiltInFailures.OverlapFailures.DuplicateInstances == id ||
	                BuiltInFailures.InaccurateFailures.InaccurateCurveBasedFamily == id ||
	                BuiltInFailures.InaccurateFailures.InaccurateBeamOrBrace == id
	                )
	            {
	                a.DeleteWarning(f);
	            }
	            //else if(BuiltInFailures.CurveFailures.LineTooShortError == id ||
	            //    BuiltInFailures.CurveFailures.LineTooShortWarning == id
	            //    )
	            //{
	            //    a.RollBackPendingTransaction();
	            //}
	            else
	            {
	                a.RollBackPendingTransaction();
	            }
	               
	        }
	        return FailureProcessingResult.Continue;
	    }
	}

    public class SelectionHelper
	{
	    public static Curve RequestModelCurveSelection(UIDocument doc, string message, dynElementSettings settings)
	    {
	        try
	        {
	            ModelCurve c = null;
	            Curve cv = null;
	
	            Selection choices = doc.Selection;
	
	            choices.Elements.Clear();
	
	            //MessageBox.Show(message);
                dynElementSettings.SharedInstance.Bench.Log(message);
	
	            Reference curveRef = doc.Selection.PickObject(ObjectType.Element);
	
	            //c = curveRef.Element as ModelCurve;
                c = dynElementSettings.SharedInstance.Revit.ActiveUIDocument.Document.GetElement(curveRef) as ModelCurve;

	            if (c != null)
	            {
	                cv = c.GeometryCurve;
	            }
	            return cv;
	        }
	        catch (Exception ex)
	        {
	            settings.Bench.Log(ex.Message);
	            return null;
	        }
	    }
	
	    public static Face RequestFaceSelection(UIDocument doc, string message, dynElementSettings settings)
	    {
	        try
	        {
	            Face f = null;
	
	            Selection choices = doc.Selection;
	
	            choices.Elements.Clear();
	
	            //MessageBox.Show(message);
	            settings.Bench.Log(message);
	
	            //create some geometry options so that we computer references
	            Autodesk.Revit.DB.Options opts = new Options();
	            opts.ComputeReferences = true;
	            opts.DetailLevel = DetailLevels.Medium;
	            opts.IncludeNonVisibleObjects = false;
	
	            Reference faceRef = doc.Selection.PickObject(ObjectType.Face);
	
	            if (faceRef != null)
	            {
                    //the suggested new method didn't exist in API?
                    GeometryObject geob = settings.Doc.Document.GetElement(faceRef).GetGeometryObjectFromReference(faceRef);

	                f = geob as Face;
	            }
	            return f;
	        }
	        catch (Exception ex)
	        {
	            settings.Bench.Log(ex.Message);
	            return null;
	        }
	
	           
	    }

        public static Form RequestFormSelection(UIDocument doc, string message, dynElementSettings settings)
        {
            try
            {
                Form f = null;

                Selection choices = doc.Selection;

                choices.Elements.Clear();

                //MessageBox.Show(message);
                settings.Bench.Log(message);

                //create some geometry options so that we computer references
                Autodesk.Revit.DB.Options opts = new Options();
                opts.ComputeReferences = true;
                opts.DetailLevel = DetailLevels.Medium;
                opts.IncludeNonVisibleObjects = false;

                Reference formRef = doc.Selection.PickObject(ObjectType.Element);

                if (formRef != null)
                {
                    //the suggested new method didn't exist in API?
                    f = settings.Doc.Document.GetElement(formRef) as Form;
                }
                return f;
            }
            catch (Exception ex)
            {
                settings.Bench.Log(ex.Message);
                return null;
            }


        }

	    public static FamilySymbol RequestFamilySymbolByInstanceSelection(UIDocument doc, string message, 
	        dynElementSettings settings, ref FamilyInstance fi)
	    {
	        try
	        {
	            FamilySymbol fs = null;
	
	            Selection choices = doc.Selection;
	
	            choices.Elements.Clear();
	
	            //MessageBox.Show(message);
	            settings.Bench.Log(message);
	
	            Reference fsRef = doc.Selection.PickObject(ObjectType.Element);
	
	            if (fsRef != null)
	            {
	                fi = fsRef.Element as FamilyInstance;
	
	                if (fi != null)
	                {
	                    return fi.Symbol;
	                }
	                else return null;
	            }
	            else return null;
	        }
	        catch (Exception ex)
	        {
	            settings.Bench.Log(ex.Message);
	            return null;
	        }
	    }

        public static FamilyInstance RequestFamilyInstanceSelection(UIDocument doc, string message,
            dynElementSettings settings)
        {
            try
            {
                FamilyInstance fi = null;

                Selection choices = doc.Selection;

                choices.Elements.Clear();

                //MessageBox.Show(message);
                settings.Bench.Log(message);

                Reference fsRef = doc.Selection.PickObject(ObjectType.Element);

                if (fsRef != null)
                {
                    fi = doc.Document.get_Element(fsRef.ElementId) as FamilyInstance;

                    if (fi != null)
                    {
                        return fi;
                    }
                    else return null;
                }
                else return null;
            }
            catch (Exception ex)
            {
                settings.Bench.Log(ex.Message);
                return null;
            }
        }


        public static Element RequestAnalysisResultInstanceSelection(UIDocument doc, string message,
    dynElementSettings settings)
        {
            try
            {

                View view = doc.ActiveView as View;

                SpatialFieldManager sfm = SpatialFieldManager.GetSpatialFieldManager(view);
                Element AnalysisResult;

                if (sfm != null)
                {
                    sfm.GetRegisteredResults();

                    Selection choices = doc.Selection;

                    choices.Elements.Clear();

                    //MessageBox.Show(message);
                    settings.Bench.Log(message);

                    Reference fsRef = doc.Selection.PickObject(ObjectType.Element);

                    if (fsRef != null)
                    {
                        AnalysisResult = doc.Document.get_Element(fsRef.ElementId) as Element;

                        if (AnalysisResult != null)
                        {
                            return AnalysisResult;
                        }
                        else return null;
                    }
                    else return null;
                }
                else return null;
            }
            catch (Exception ex)
            {
                settings.Bench.Log(ex.Message);
                return null;
            }
        }
        
	}

    public class DynamoUpdater : IUpdater
    {
        public static bool _updateActive = false;
        static AddInId m_appId;
        static UpdaterId m_updaterId;
        SpatialFieldManager m_sfm = null;

        // constructor takes the AddInId for the add-in associated with this updater
        public DynamoUpdater(AddInId id)
        {
            m_appId = id;
            m_updaterId = new UpdaterId(m_appId, new Guid("1F1F44B4-8002-4CC1-8FDB-17ACD24A2ECE")); //[Guid("1F1F44B4-8002-4CC1-8FDB-17ACD24A2ECE")]
        }
        public void Execute(UpdaterData data)
        {
            try
            {
                if (_updateActive == false) { return; }

                Document doc = data.GetDocument();
                Autodesk.Revit.DB.View view = doc.ActiveView;
                SpatialFieldManager sfm = SpatialFieldManager.GetSpatialFieldManager(view);

                UpdaterData tempData = data;

                if (sfm != null)
                {
                    // Cache the spatial field manager if ther is one
                    if (m_sfm == null)
                    {
                        //FilteredElementCollector collector = new FilteredElementCollector(doc);
                        //collector.OfClass(typeof(SpatialFieldManager));
                        //var sfm = from element in collector
                        //          select element;
                        //if (sfm.Count<Element>() > 0) // if we actually found an SFM
                        //{
                        //m_sfm = sfm.Cast<SpatialFieldManager>().ElementAt<SpatialFieldManager>(0);
                        m_sfm = sfm;
                        //TaskDialog.Show("ah hah", "found spatial field manager adding to cache");
                        //}

                    }
                    if (m_sfm != null)
                    {
                        // if we find an sfm has been updated and it matches what  already have one cached, send it to dyanmo
                        //foreach (ElementId addedElemId in data.GetAddedElementIds())
                        //{
                        //SpatialFieldManager sfm = doc.get_Element(addedElemId) as SpatialFieldManager;
                        //if (sfm != null)
                        //{
                        // TaskDialog.Show("ah hah", "found spatial field manager yet, passing to dynamo");
                        dynElementSettings.SharedInstance.SpatialFieldManagerUpdated = sfm;
                        //Dynamo.Elements.OnDynElementReadyToBuild(EventArgs.Empty);//kick it
                        //}
                        //}
                    }
                }
                else
                {
                    //TaskDialog.Show("ah hah", "no spatial field manager yet, please run sr tool");
                }
            }
            catch (Exception ex)
            {

            }
        }
        public string GetAdditionalInformation()
        {
            return "Watch for changes to Analysis Results object (Spatial Field Manager) and pass this to Dynamo";
        }
        public ChangePriority GetChangePriority()
        {
            return ChangePriority.FloorsRoofsStructuralWalls;
        }
        public UpdaterId GetUpdaterId()
        {
            return m_updaterId;
        }
        public string GetUpdaterName()
        {
            return "Dyanmo Analysis Results Watcher";
        }
    }

    public class SunAndShadowUpdater : IUpdater
    {
        public static bool _updateActive = false;
        static AddInId _appId;
        static UpdaterId _updaterId;

        // constructor takes the AddInId for the add-in associated with this updater
        public SunAndShadowUpdater(AddInId id)
        {
            _appId = id;
            _updaterId = new UpdaterId(_appId, new Guid("16f5cd24-f0e5-4ef3-8731-a171ec45e6a4"));
        }

        public void Execute(UpdaterData data)
        {
            try
            {
                if (_updateActive == false) { return; }

                //see if there is an active transaction
                //if there is, commit it
                if (dynElementSettings.SharedInstance.SubTransaction != null)
                {
                    if (!dynElementSettings.SharedInstance.SubTransaction.HasEnded())
                    {
                        TransactionStatus ts = dynElementSettings.SharedInstance.SubTransaction.Commit();
                    }
                    dynElementSettings.SharedInstance.SubTransaction.Dispose();
                }
                
                //trigger and event on the workbench that says sun and shadow settings have been updated
                dynElementSettings.SharedInstance.Bench.OnSunAndShadowChanged(EventArgs.Empty);

            }
            catch (Exception ex)
            {
                TaskDialog.Show("Exception", ex.Message);
            }
        }

        public string GetAdditionalInformation()
        {
            return "Watch for changes to Sun and Shadow Settings and passes this to Dynamo";
        }
        public ChangePriority GetChangePriority()
        {
            return ChangePriority.Views;
        }
        public UpdaterId GetUpdaterId()
        {
            return _updaterId;
        }
        public string GetUpdaterName()
        {
            return "Dyanmo Sun and Shadow Watcher";
        }
    }

}
