using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Autodesk.Revit.Creation;

using RevitServices.Persistence;

using Document = Autodesk.Revit.DB.Document;

namespace Revit.Elements.InternalUtilities
{
    public static class JoinGeometryUtils
    {
        public static IList<Element> GetJoinedElements(Document document, Element element)
        {

            var matches = Autodesk.Revit.DB.JoinGeometryUtils.GetJoinedElements(document, element.InternalElement);

            var instances = matches
               .Select(x => ElementSelector.ByElementId(x.IntegerValue)).ToList();
            return instances;
        }
    }
}
