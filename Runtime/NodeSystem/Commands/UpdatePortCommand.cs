﻿namespace UniModules.GameFlow.Runtime.Commands
{
    using System;
    using System.Diagnostics;
    using Interfaces;
    using UniModules.UniGame.Core.Runtime.Interfaces;

    [Serializable]
    public class UpdatePortCommand : SerializedNodeCommand
    {
        private static DummyPortsCommand emptyCommand = new DummyPortsCommand();
        
        public override ILifeTimeCommand Create(IUniNode node)
        {
            UpdateDynamicPorts(node);
            return emptyCommand;
        }

        [Conditional("UNITY_EDITOR")]
        private void UpdateDynamicPorts(INode node)
        {

            var fields = node.GetType();
            

        }
        
    }
}
