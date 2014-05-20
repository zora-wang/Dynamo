using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dynamo.Controls;
using Dynamo.Models;

namespace Dynamo.Manipulation
{
    public class ManipulatorDaemon
    {
        private readonly Dictionary<Type, IEnumerable<INodeManipulatorCreator>> registeredManipulators;

        private readonly Dictionary<Guid, IDisposable> activeManipulators =
            new Dictionary<Guid, IDisposable>();

        private ManipulatorDaemon(Dictionary<Type, IEnumerable<INodeManipulatorCreator>> manips)
        {
            registeredManipulators = manips;
        }

        public static ManipulatorDaemon Create(IManipulatorDaemonInitializer initializer)
        {
            return new ManipulatorDaemon(initializer.GetManipulators());
        }

        public void CreateManipulator(NodeModel model, DynamoView dynamoView)
        {
            IEnumerable<INodeManipulatorCreator> creators;
            if (registeredManipulators.TryGetValue(model.GetType(), out creators))
            {
                activeManipulators[model.GUID] =
                    new CompositeManipulator(
                        creators.Select(
                            creator => creator.Create(model, new DynamoContext { View = dynamoView }))
                            .Where(manipulator => manipulator != null));
            }
        }

        public void KillManipulators(NodeModel model)
        {
            IDisposable disposable;
            if (activeManipulators.TryGetValue(model.GUID, out disposable))
            {
                disposable.Dispose();
                activeManipulators.Remove(model.GUID);
            }
        }

        internal void KillAll()
        {
            foreach (var manip in activeManipulators.Values)
                manip.Dispose();
            activeManipulators.Clear();
        }
    }

    internal class CompositeManipulator : IManipulator
    {
        private readonly IEnumerable<IManipulator> subs;
        public CompositeManipulator(IEnumerable<IManipulator> subs)
        {
            this.subs = subs;
        }

        public void Dispose()
        {
            foreach (var sub in subs)
                sub.Dispose();
        }
    }
}
