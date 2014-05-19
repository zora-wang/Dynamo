using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Dynamo.Manipulation
{
    public interface IManipulator : IDisposable
    {

    }

    internal class EmptyManipulator : IManipulator
    {
        public void Dispose() { }
    }
}
