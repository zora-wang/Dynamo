using System;
using System.Collections.Generic;
using Dynamo.Nodes;

namespace Dynamo.Manipulation
{
    public interface IManipulatorDaemonInitializer 
    {
        Dictionary<Type, INodeManipulatorCreator> GetManipulators();
    }

    public class HardcodedInitializer : IManipulatorDaemonInitializer
    {
        public Dictionary<Type, INodeManipulatorCreator> GetManipulators()
        {
            return new Dictionary<Type, INodeManipulatorCreator>
            {
                { typeof(DSFunction), new DSFunctionManipulatorCreator() }
            };
        }
    }

    public class ReflectionInitializer : IManipulatorDaemonInitializer
    {
        public Dictionary<Type, INodeManipulatorCreator> GetManipulators()
        {
            throw new NotImplementedException();
        }
    }
}