using Dynamo.Controls;
using Dynamo.Utilities;

namespace Dynamo.Manipulation
{
    public class DynamoManipulatorContext
    {
        internal DynamoManipulatorContext () { }

        public DynamoController Controller { get { return dynSettings.Controller; } }
        public DynamoView View { get; set; }
    }
}