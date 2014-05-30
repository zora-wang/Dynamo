using Dynamo.Controls;
using Dynamo.Utilities;
using Dynamo.ViewModels;

namespace Dynamo.Manipulation
{
    public class DynamoManipulatorContext
    {
        internal DynamoManipulatorContext () { }

        public DynamoController Controller { get { return dynSettings.Controller; } }
        public DynamoView View { get; set; }
        public AttributesViewModel AttributesViewModel { get { return dynSettings.Controller.AttributesViewModel; } }
    }
}