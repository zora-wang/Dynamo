using Dynamo.Controls;
using Dynamo.Utilities;

namespace Dynamo.Manipulation
{
    public class DynamoContext
    {
        public DynamoContext(DynamoView view)
        {
            this.View = view;
        }

        public DynamoController Controller { get { return dynSettings.Controller; } }
        public DynamoView View { get; set; }
    }
}