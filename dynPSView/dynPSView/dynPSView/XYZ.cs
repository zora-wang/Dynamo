//Copyright © Autodesk, Inc. 2012. All rights reserved.
//
//Licensed under the Apache License, Version 2.0 (the "License");
//you may not use this file except in compliance with the License.
//You may obtain a copy of the License at
//
//http://www.apache.org/licenses/LICENSE-2.0
//
//Unless required by applicable law or agreed to in writing, software
//distributed under the License is distributed on an "AS IS" BASIS,
//WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//See the License for the specific language governing permissions and
//limitations under the License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Autodesk.Revit.DB
{
    // Animplementation of XYZ covering the minimal functionality requirements
    class XYZ
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

        public XYZ(double x, double y, double z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        public XYZ()
        {
            this.X = 0;
            this.Y = 0;
            this.Z = 0;
        }

        public bool IsAlmostEqualTo(XYZ c)
        {
            if ((this - c).Norm() < 1e-8)
                return true;

            return false;
        }

        public double DotProduct(XYZ c2)
        {
            return this.X * c2.X + this.Y * c2.Y + this.Z * c2.Z;
        }

        public double Norm()
        {
            return Math.Sqrt(this.DotProduct(this));
        }

        public XYZ Add(XYZ c2)
        {
            return this + c2;
        }

        public XYZ Subtract(XYZ c2)
        {
            return this - c2;
        }

        public static XYZ operator +(XYZ c1, XYZ c2) 
        {
            return new XYZ(c1.X + c2.X, c1.Y + c2.Y, c1.Z + c2.Z);
        }

        public static XYZ operator -(XYZ c1, XYZ c2)
        {
            return new XYZ(c1.X - c2.X, c1.Y - c2.Y, c1.Z - c2.Z);
        }

        public static XYZ operator *(double c1, XYZ c2)
        {
            return new XYZ(c1 * c2.X, c1 * c2.Y, c1 * c2.Z);
        }

        public static XYZ operator *(XYZ c2, double c1)
        {
            return new XYZ(c1 * c2.X, c1 * c2.Y, c1 * c2.Z);
        }

        public static XYZ operator /(XYZ c2, double v)
        {
            return new XYZ(c2.X / v, c2.Y / v, c2.Z / v);
        }

        public static XYZ operator -(XYZ c2)
        {
            return new XYZ(-c2.X, -c2.Y, -c2.Z);
        }


    }
}
