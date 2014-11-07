using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.DesignScript.Geometry;

namespace DynamoZeroTouchLibrary
{
    public class ZeroTouchClass : IDisposable
    {
        /// <summary>
        /// An example private constructor.
        /// </summary>
        private ZeroTouchClass()
        {
            // Default private constructor.    
        }

        /// <summary>
        /// An example public static constructor.
        /// </summary>
        /// <param name="name">An example parameter with a default value.</param>
        /// <returns>A ZeroTouchClass object.</returns>
        public static ZeroTouchClass ByName(string name = "Hello Dynamo")
        {
            return new ZeroTouchClass();
        }

        public void Dispose()
        {
            // Cleanup here.
        }
    }
}
