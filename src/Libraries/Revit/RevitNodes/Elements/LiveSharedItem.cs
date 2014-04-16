using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Policy;
using Autodesk.DesignScript.Geometry;
using Autodesk.DesignScript.Runtime;
using Autodesk.Revit.DB;
using DSNodeServices;
using Revit.GeometryConversion;
using Revit.References;
using RevitServices.Persistence;
using RevitServices.Transactions;
using Revit.Elements;
using Face = Autodesk.DesignScript.Geometry.Face;
using Plane = Autodesk.DesignScript.Geometry.Plane;

namespace Live.Shared
{
   /// <summary>
   /// A Live Shared Element with Groundhog and other client
   /// </summary>

   public class LiveSharedItem : IDisposable
   {
      #region PrivateSharedData

      private Dictionary<string, string> mStringItemData;
      private Dictionary<string, int> mIntItemData;
      private Dictionary<string, double> mDoubleItemData;
      private Dictionary<string, Url> mUrlItemData;
      private Dictionary<string, Autodesk.DesignScript.Geometry.Geometry> mGeometryItemData;

      //for grounhog search if needed
      private Guid _myGuid;


      #endregion

      #region Public Methods

      [IsVisibleInDynamoLibrary(false)]
      public virtual void Dispose()
      {

         // Do not cleanup Revit elements if we are shutting down Dynamo.
         if (DisposeLogic.IsShuttingDown)
            return;
      }

      /// <summary>
      /// get item data of type string
      /// </summary>
      /// <param name="key"></param>
      public String GetStringItemData(string key)
      {
         if (!mStringItemData.ContainsKey(key))
            return null;
         return mStringItemData[key];
      }

      /// <summary>
      /// set item data of type string
      /// </summary>
      /// <param name="key"></param>
      /// <param name="data"></param>
      public bool SetStringItemData(string key, string data)
      {
         bool result = mStringItemData.ContainsKey(key);
         mStringItemData[key] = data;
         return result;
      }

      /// <summary>
      /// set list of item data of type string
      /// </summary>
      /// <param name="keys"></param>
      /// <param name="dataList"></param>
      public void SetStringItemDataFromList(List<string> keys, List<string> dataList)
      {
         var keyEnum = keys.GetEnumerator();
         var dataEnum = dataList.GetEnumerator();
         for (; keyEnum.MoveNext() && dataEnum.MoveNext(); )
         {
            SetStringItemData(keyEnum.Current, dataEnum.Current);
         }
      }

      /// <summary>
      /// get item data of type int
      /// </summary>
      /// <param name="key"></param>
      public int GetIntItemData(string key)
      {
         if (!mIntItemData.ContainsKey(key))
            throw new Exception("no int data for given key");
         return mIntItemData[key];
      }

      /// <summary>
      /// set item data of type int
      /// </summary>
      /// <param name="key"></param>
      /// <param name="data"></param>
      public bool SetIntItemData(string key, int data)
      {
         bool result = mIntItemData.ContainsKey(key);
         mIntItemData[key] = data;
         return result;
      }

      /// <summary>
      /// set list of item data of type int
      /// </summary>
      /// <param name="keys"></param>
      /// <param name="dataList"></param>
      public void SetIntItemDataFromList(List<string> keys, List<int> dataList)
      {
         var keyEnum = keys.GetEnumerator();
         var dataEnum = dataList.GetEnumerator();
         for (; keyEnum.MoveNext() && dataEnum.MoveNext(); )
         {
            SetIntItemData(keyEnum.Current, dataEnum.Current);
         }
      }

      /// <summary>
      /// get item data of type double
      /// </summary>
      /// <param name="key"></param>
      public double GetDoubleItemData(string key)
      {
         if (!mDoubleItemData.ContainsKey(key))
            throw new Exception("no double data for given key");
         return mDoubleItemData[key];
      }

      /// <summary>
      /// set item data of type double
      /// </summary>
      /// <param name="key"></param>
      /// <param name="data"></param>
      public bool SetDoubleItemData(string key, double data)
      {
         bool result = mStringItemData.ContainsKey(key);
         mDoubleItemData[key] = data;
         return result;
      }

      /// <summary>
      /// set list of item data of type double
      /// </summary>
      /// <param name="keys"></param>
      /// <param name="dataList"></param>
      public void SetDoubleItemDataFromList(List<string> keys, List<Double> dataList)
      {
         var keyEnum = keys.GetEnumerator();
         var dataEnum = dataList.GetEnumerator();
         for (; keyEnum.MoveNext() && dataEnum.MoveNext(); )
         {
            SetDoubleItemData(keyEnum.Current, dataEnum.Current);
         }
      }

      /// <summary>
      /// get item data of type Url
      /// </summary>
      /// <param name="key"></param>
      public Url GetUrlItemData(string key)
      {
         if (!mUrlItemData.ContainsKey(key))
            throw new Exception("Mullion should represent Revit's Mullion");
         return mUrlItemData[key];
      }

      /// <summary>
      /// set item data of type Url
      /// </summary>
      /// <param name="key"></param>
      /// <param name="data"></param>
      public bool SetUrlItemData(string key, Url data)
      {
         bool result = mStringItemData.ContainsKey(key);
         mUrlItemData[key] = data;
         return result;
      }

      /// <summary>
      /// set list of item data of type Url
      /// </summary>
      /// <param name="keys"></param>
      /// <param name="dataList"></param>
      public void SetUrlItemDataFromList(List<string> keys, List<Url> dataList)
      {
         var keyEnum = keys.GetEnumerator();
         var dataEnum = dataList.GetEnumerator();
         for (; keyEnum.MoveNext() && dataEnum.MoveNext(); )
         {
            SetUrlItemData(keyEnum.Current, dataEnum.Current);
         }
      }

      /// <summary>
      /// get item data of type ASM Geometry
      /// </summary>
      /// <param name="key"></param>
      public Geometry GetGeometryItemData(string key)
      {
         if (!mGeometryItemData.ContainsKey(key))
            return null;
         return mGeometryItemData[key];
      }

      /// <summary>
      /// set item data of type ASM Geometry
      /// </summary>
      /// <param name="key"></param>
      /// <param name="data"></param>
      public bool SetGeometryItemData(string key, Geometry data)
      {
         bool result = mGeometryItemData.ContainsKey(key);
         mGeometryItemData[key] = data;
         return result;
      }

      /// <summary>
      /// set list of item data of type ASM Geometry
      /// </summary>
      /// <param name="keys"></param>
      /// <param name="dataList"></param>
      public void SetItemDataFromList(List<string> keys, List<Geometry> dataList)
      {
         var keyEnum = keys.GetEnumerator();
         var dataEnum = dataList.GetEnumerator();
         for (; keyEnum.MoveNext() && dataEnum.MoveNext(); )
         {
            SetGeometryItemData(keyEnum.Current, dataEnum.Current);
         }
      }

      private string GetGeometryAsString(string key, string tempFileNamePath)
      {
         Geometry[] geoms = {GetGeometryItemData(key)};
         Geometry.ExportToSAT(geoms, tempFileNamePath);
         string textSat= System.IO.File.ReadAllText(tempFileNamePath);
         //delete file?
         return textSat;
      }

      private bool SetGeometryAsString(string key, string tempFileNamePath, string[] stringsSat)
      {
         System.IO.File.WriteAllLines(tempFileNamePath, stringsSat);
         Geometry[] geoms =  Geometry.ImportFromSAT(tempFileNamePath);
         if (geoms.Length < 1)
            return false;

         SetGeometryItemData(key, geoms[0]);

         return true;
      }

       #region Private constructors

      /// <summary>
      /// Create LiveSharedItem
      /// </summary>
 
      protected LiveSharedItem()
      {
         _myGuid = new Guid();
         mStringItemData = new Dictionary<string, string>();
         mIntItemData = new Dictionary<string, int> ();
         mDoubleItemData = new Dictionary<string, double>();
         mUrlItemData = new Dictionary<string, Url>();
         mGeometryItemData = new Dictionary<string, Geometry>();
      }

      #endregion

      #endregion

      #region Static constructors

      /// <summary>
      /// LiveShareItem with purpose set for manual addition of data
      /// </summary>
      /// /// <param name="purpose"></param>
      public static LiveSharedItem ByPurposeOnly(string purpose)
      {
         var result = new LiveSharedItem();
         result.SetStringItemData("purpose for live sharing", purpose);
         return result;
      }

      /// <summary>
      /// LiveShareItem from Revit Element
      /// </summary>
      /// /// <param name="element"></param>
      public static LiveSharedItem ByElement(Revit.Elements.Element element)
      {
         string elementPurpose = "Revit Element " + element.Id.ToString();
         var result =  LiveSharedItem.ByPurposeOnly(elementPurpose);
         //put in type and id
         result.SetIntItemData("Revit Id", element.InternalElement.Id.IntegerValue);
         var typeElement = Revit.Elements.Element.Document.GetElement(element.InternalElement.GetTypeId());
         result.SetStringItemData("Revit Type", typeElement.Name);

         //put in parameters from type first
         var typeParms = typeElement.Parameters;
         var typeEnum = typeParms.GetEnumerator();
         for (; typeEnum.MoveNext();)
         {
            var thisParam = (Autodesk.Revit.DB.Parameter)typeEnum.Current;
            string key = thisParam.Definition.Name;
            
            if (thisParam.StorageType == StorageType.String)
              result.SetStringItemData(key, thisParam.AsValueString());
            else if (thisParam.StorageType == StorageType.Integer || thisParam.StorageType == StorageType.ElementId)
            {
               if (thisParam.StorageType == StorageType.Integer)
               {
                  result.SetIntItemData(key, thisParam.AsInteger());
               }
               else
               {
                  result.SetIntItemData(key, thisParam.AsElementId().IntegerValue);
               }
            }
            else if (thisParam.StorageType == StorageType.Double)
               result.SetDoubleItemData(key, thisParam.AsDouble());
            else 
               result.SetStringItemData(key, thisParam.AsValueString());
         }

         var parms = element.InternalElement.Parameters;
         var elemEnum = typeParms.GetEnumerator();
         for (; elemEnum.MoveNext(); )
         {
            var thisParam = (Autodesk.Revit.DB.Parameter)elemEnum.Current;
            string key = thisParam.Definition.Name;

            if (thisParam.StorageType == StorageType.String)
               result.SetStringItemData(key, thisParam.AsValueString());
            else if (thisParam.StorageType == StorageType.Integer || thisParam.StorageType == StorageType.ElementId)
            {
               if (thisParam.StorageType == StorageType.Integer)
               {
                  result.SetIntItemData(key, thisParam.AsInteger());
               }
               else
               {
                  result.SetIntItemData(key, thisParam.AsElementId().IntegerValue);
               }
            }
            else if (thisParam.StorageType == StorageType.Double)
               result.SetDoubleItemData(key, thisParam.AsDouble());
            else
               result.SetStringItemData(key, thisParam.AsValueString());
         }

         //put in location
         if (element is Revit.Elements.Mullion)
         {
            var mullion = element as Revit.Elements.Mullion;
            result.SetGeometryItemData("LocationCurve", mullion.LocationCurve);
         }
         if (element is Revit.Elements.CurtainPanel)
         {
            var curtainPanel = element as Revit.Elements.CurtainPanel;
            var bounds = curtainPanel.Boundaries;
            for (int index = 0; index < bounds.Length; index++)
            {
               string key = "BoundaryCurves no. " + index.ToString();
               result.SetGeometryItemData(key, curtainPanel.Boundaries[index]);
            }
         }
 

         return result;
      }

      //add synchronization to Groundhog

      //add method to make LiveSharedItem from Groundhog project db (using element id)

      #endregion


   }
}