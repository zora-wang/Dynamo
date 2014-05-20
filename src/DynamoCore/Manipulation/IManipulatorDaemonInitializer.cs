using System;
using System.Collections.Generic;
using Dynamo.Nodes;

namespace Dynamo.Manipulation
{
    public interface IManipulatorDaemonInitializer 
    {
        Dictionary<Type, IEnumerable<INodeManipulatorCreator>> GetManipulators();
    }

    public class HardcodedInitializer : IManipulatorDaemonInitializer
    {
        public Dictionary<Type, IEnumerable<INodeManipulatorCreator>> GetManipulators()
        {
            return new Dictionary<Type, IEnumerable<INodeManipulatorCreator>>
            {
                { typeof(DSFunction), new[] { new DSFunctionManipulatorCreator() }},
                { typeof(Function), new[] { CustomNodeManipulatorCreator.Instance }}
            };
        }
    }

    public class ReflectionInitializer : IManipulatorDaemonInitializer
    {
        public Dictionary<Type, IEnumerable<INodeManipulatorCreator>> GetManipulators()
        {
            throw new NotImplementedException();
        }
    }
}