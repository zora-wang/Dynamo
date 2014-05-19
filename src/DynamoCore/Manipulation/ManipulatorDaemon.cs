using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dynamo.Models;

namespace Dynamo.Manipulation
{
    public class ManipulatorDaemon
    {
        private readonly Dictionary<Type, INodeManipulatorCreator> registeredManipulators;

        private readonly Dictionary<Guid, IDisposable> activeManipulators =
            new Dictionary<Guid, IDisposable>();

        private ManipulatorDaemon(Dictionary<Type, INodeManipulatorCreator> manips)
        {
            registeredManipulators = manips;
        }

        public static ManipulatorDaemon Create(IManipulatorDaemonInitializer initializer)
        {
            return new ManipulatorDaemon(initializer.GetManipulators());
        }

        public void CreateManipulator(NodeModel model)
        {
            INodeManipulatorCreator creator;
            if (registeredManipulators.TryGetValue(model.GetType(), out creator))
                activeManipulators[model.GUID] = creator.Create(model, new DynamoContext());
        }

        public void KillManipulator(NodeModel model)
        {
            IDisposable disposable;
            if (activeManipulators.TryGetValue(model.GUID, out disposable))
                disposable.Dispose();
        }
    }
}
