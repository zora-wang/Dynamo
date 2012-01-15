using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dynamo.Controls;
using Dynamo.Utilities;
using Autodesk.Revit.DB;

namespace Dynamo.Elements
{
    [ElementName("SunSettings")]
    [ElementDescription("An element which queries the active view's sun settings.")]
    [RequiresTransaction(false)]
    public class dynSunSettings : dynElement
    {

        public dynSunSettings()
        {
            //listen for the event on the workbench
            //that says the sun and shadow settings have changed
            dynElementSettings.SharedInstance.Bench.SunAndShadowChanged += new SunAndShadowChangedHandler(Bench_SunAndShadowChanged);

            //get the sun settings from the active view
            View v = dynElementSettings.SharedInstance.Doc.ActiveView;

            this.InPortData.Add(new Connectors.PortData(0, "f", "frame", typeof(dynInt)));

            this.OutPortData.Add(new Connectors.PortData(v.SunAndShadowSettings.GetFrameAzimuth(0), "az","Azimuth", typeof(dynDouble)));
            this.OutPortData.Add(new Connectors.PortData(v.SunAndShadowSettings.GetFrameAltitude(0), "al","Altitude", typeof(dynDouble)));
            this.OutPortData.Add(new Connectors.PortData(v.SunAndShadowSettings.GetFrameTime(0).DayOfYear, "h", "Day", typeof(dynDouble)));
            this.OutPortData.Add(new Connectors.PortData(v.SunAndShadowSettings.GetFrameTime(0).Month, "m", "Month", typeof(dynDouble)));
            this.OutPortData.Add(new Connectors.PortData(v.SunAndShadowSettings.GetFrameTime(0).Year, "y", "Year", typeof(dynDouble)));

            base.RegisterInputsAndOutputs();
        }

        void Bench_SunAndShadowChanged(object sender, EventArgs e)
        {
            if (CheckInputs())
            {
                int f = Convert.ToInt32(InPortData[0].Object);

                //get the sun settings from the active view
                View v = dynElementSettings.SharedInstance.Doc.ActiveView;

                this.OutPortData[0].Object = v.SunAndShadowSettings.GetFrameAzimuth(f);
                this.OutPortData[1].Object = v.SunAndShadowSettings.GetFrameAltitude(f);
                this.OutPortData[2].Object = v.SunAndShadowSettings.GetFrameTime(f).DayOfYear;
                this.OutPortData[3].Object = v.SunAndShadowSettings.GetFrameTime(f).Month;
                this.OutPortData[4].Object = v.SunAndShadowSettings.GetFrameTime(f).Year;

                //this.UpdateOutputs();
                OnDynElementReadyToBuild(EventArgs.Empty);
            }
        }

        public override void Draw()
        {
            base.Draw();
        }

        public override void Update()
        {
            OnDynElementReadyToBuild(EventArgs.Empty);
        }
    }
}
