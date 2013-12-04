using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Dynamo.Controls;
using Dynamo.FSchemeInterop;
using Dynamo.Models;
using Dynamo.Nodes;
using Dynamo.Utilities;
using Dynamo.Selection;
using Dynamo.ViewModels;
using NUnit.Framework;
using System.Windows;
using DynCmd = Dynamo.ViewModels.DynamoViewModel;

namespace Dynamo.Tests
{
    internal class CoreTests : DynamoUnitTest
    {
        
        [Test]
        public void CanOpenGoodCommunicationFile()
        {
            var model = dynSettings.Controller.DynamoModel;

            string openPath = Path.Combine(GetTestDirectory(), @"core\multiplicationAndAdd\multiplicationAndAdd.dyn");
            model.Open(openPath);

            Assert.AreEqual(5, Controller.DynamoViewModel.CurrentSpace.Nodes.Count);
        }

        [Test]
        public void CanCreateUDPListenerNode()
        {
            var model = dynSettings.Controller.DynamoModel;
            model.CreateNode(400.0, 100.0, "UDP Listener");
            Assert.AreEqual(Controller.DynamoViewModel.CurrentSpace.Nodes.Count, 1);
        }

        [Test]
        public void CanCreateUDPBroadcasterNode()
        {
            var model = dynSettings.Controller.DynamoModel;
            model.CreateNode(400.0, 100.0, "UDP Broadcaster");
            Assert.AreEqual(Controller.DynamoViewModel.CurrentSpace.Nodes.Count, 1);
        }

    }
}