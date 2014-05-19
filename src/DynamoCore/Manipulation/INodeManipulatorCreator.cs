using System;
using System.Collections.Generic;
using Dynamo.Models;

namespace Dynamo.Manipulation
{
    public interface INodeManipulatorCreator
    {
        IManipulator Create(NodeModel node, DynamoContext context);
    }
}