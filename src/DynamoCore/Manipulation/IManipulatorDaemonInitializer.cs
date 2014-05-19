using System;
using System.Collections.Generic;

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