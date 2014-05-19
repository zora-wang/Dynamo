using Dynamo.Controls;
using Dynamo.Utilities;

namespace Dynamo.Manipulation
{
    public class DynamoContext
    {
        internal DynamoContext () { }

        public DynamoController Controller { get { return dynSettings.Controller; } }
        public DynamoView View { get; set; }
    }
}