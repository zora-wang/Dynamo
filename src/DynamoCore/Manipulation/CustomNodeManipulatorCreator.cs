using Dynamo.Nodes;

namespace Dynamo.Manipulation
{
    public class CustomNodeManipulatorCreator : LookupCreator<Function>
    {
        private static CustomNodeManipulatorCreator instance;
        public static CustomNodeManipulatorCreator Instance
        {
            get { return instance ?? (instance = new CustomNodeManipulatorCreator()); }
        }

        private CustomNodeManipulatorCreator() { }

        protected override string GetKey(Function dsfunc)
        {
            return dsfunc.Definition.FunctionId.ToString();
        }
    }
}