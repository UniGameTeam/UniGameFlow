﻿namespace UniModules.GameFlow.Runtime.Core
{
    using Interfaces;
    using Runtime.Interfaces;

    public interface IPortConnection 
    {

        int NodeId { get; }

        string PortName { get; }

        INodePort Port { get; }
    }
}