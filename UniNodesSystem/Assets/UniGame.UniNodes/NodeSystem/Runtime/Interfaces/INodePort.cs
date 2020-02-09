namespace UniGreenModules.UniNodeSystem.Runtime.Core
{
    using System;
    using System.Collections.Generic;
    using UniRx;
    using UnityEngine;

    public interface INodePort : IMessageBroker
    {
        ulong Id { get; }
        
        int ConnectionCount { get; }

        /// <summary> Return the first non-null connection </summary>
        INodePort Connection { get; }

        PortIO Direction { get; }
        
        ConnectionType ConnectionType { get; }

        /// <summary> Is this port connected to anytihng? </summary>
        bool IsConnected { get; }
        bool IsInput { get; }
        bool IsOutput { get; }
        
        string FieldName { get; }

        IReadOnlyList<Type> ValueTypes { get; }

        UniBaseNode Node { get; set; }
        
        bool IsDynamic { get; }
        
        bool IsStatic { get; }

        ulong UpdateId();

        /// <summary> Checks all connections for invalid references, and removes them. </summary>
        void VerifyConnections();

        /// <summary> Connect this <see cref="NodePort"/> to another </summary>
        /// <param name="port">The <see cref="NodePort"/> to connect to</param>
        void Connect(NodePort port);

        List<INodePort> GetConnections();
        
        INodePort GetConnection(int i);

        /// <summary> Get index of the connection connecting this and specified ports </summary>
        int GetConnectionIndex(NodePort port);

        bool IsConnectedTo(NodePort port);

        /// <summary> Disconnect this port from another port </summary>
        void Disconnect(NodePort port);

        /// <summary> Disconnect this port from another port </summary>
        void Disconnect(int i);

        void ClearConnections();

        /// <summary> Get reroute points for a given connection. This is used for organization </summary>
        List<Vector2> GetReroutePoints(int index);

        /// <summary> Swap connections with another node </summary>
        void SwapConnections(NodePort targetPort);

        /// <summary> Copy all connections pointing to a node and add them to this one </summary>
        void AddConnections(NodePort targetPort);

        /// <summary> Move all connections pointing to this node, to another node </summary>
        void MoveConnections(NodePort targetPort);

        /// <summary> Swap connected nodes from the old list with nodes from the new list </summary>
        void Redirect(List<UniBaseNode> oldNodes, List<UniBaseNode> newNodes);
    }
}