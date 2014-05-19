using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Dynamo.Utilities;

namespace Dynamo.Manipulation
{
    public interface IManipulator : IDisposable
    {
    }

    public class DynamoContext
    {
        public DynamoController Controller { get { return dynSettings.Controller; } }
    }
}
