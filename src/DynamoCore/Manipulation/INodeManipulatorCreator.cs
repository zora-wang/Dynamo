using Dynamo.Models;

namespace Dynamo.Manipulation
{
    public interface INodeManipulatorCreator
    {
        string NodeType { get; }
        IManipulator Create(NodeModel node, DynamoContext context);
    }
}