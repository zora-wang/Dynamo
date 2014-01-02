using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Dynamo.Nodes;
using NUnit.Framework;
using ProtoCore.AST.AssociativeAST;
using RevitServices.Persistence;

namespace Dynamo.Tests
{
    [TestFixture]
    class SelectionTests
    {
        [Test]
        public void SelectElementASTGeneration()
        {
            ReferencePoint refPoint = null;

            using (var trans = new Transaction(DocumentManager.GetInstance().CurrentDBDocument, "CreateAndDeleteAreReferencePoint"))
            {
                trans.Start();

                FailureHandlingOptions fails = trans.GetFailureHandlingOptions();
                fails.SetClearAfterRollback(true);
                trans.SetFailureHandlingOptions(fails);

                refPoint = DocumentManager.GetInstance().CurrentDBDocument.FamilyCreate.NewReferencePoint(new XYZ());

                trans.Commit();
            }

            var sel = new DSModelElementSelection {SelectedElement = refPoint};

            var buildOutput = sel.BuildOutputAst(new List<AssociativeNode>());

            var funCall = (FunctionCallNode)((BinaryExpressionNode)buildOutput.First()).RightNode;

            Assert.IsInstanceOf<IdentifierNode>(funCall.Function);
            Assert.AreEqual(1, funCall.FormalArguments.Count);
            Assert.IsInstanceOf<IntNode>(funCall.FormalArguments[0]);

            Assert.AreEqual(refPoint.Id.IntegerValue.ToString(), ((IntNode)funCall.FormalArguments[0]).value);
        }
    }
}
