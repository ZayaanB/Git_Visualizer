using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace GitVisualizer.Models
{
    public struct GraphNodeData : INetworkSerializable
    {
        public Vector3 Position;
        public FixedString64Bytes Sha;
        public FixedString512Bytes Message;
        public FixedString128Bytes AuthorName;
        public FixedString64Bytes Date;
        public int IndexInBranch;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Position);
            serializer.SerializeValue(ref Sha);
            serializer.SerializeValue(ref Message);
            serializer.SerializeValue(ref AuthorName);
            serializer.SerializeValue(ref Date);
            serializer.SerializeValue(ref IndexInBranch);
        }
    }

    public struct BranchVisualizationData : INetworkSerializable
    {
        public FixedString64Bytes BranchName;
        public Vector3 ColorRgb;
        public int NodeCount;
        public List<GraphNodeData> Nodes;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref BranchName);
            serializer.SerializeValue(ref ColorRgb);
            serializer.SerializeValue(ref NodeCount);
            if (serializer.IsReader)
            {
                Nodes ??= new List<GraphNodeData>();
                Nodes.Clear();
                for (int i = 0; i < NodeCount; i++)
                {
                    var node = new GraphNodeData();
                    node.NetworkSerialize(serializer);
                    Nodes.Add(node);
                }
            }
            else
            {
                for (int i = 0; i < NodeCount && i < Nodes.Count; i++)
                    Nodes[i].NetworkSerialize(serializer);
            }
        }
    }

    public struct GraphVisualizationPayload : INetworkSerializable
    {
        public int BranchCount;
        public List<BranchVisualizationData> Branches;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref BranchCount);
            if (serializer.IsReader)
            {
                Branches ??= new List<BranchVisualizationData>();
                Branches.Clear();
                for (int i = 0; i < BranchCount; i++)
                {
                    var branch = new BranchVisualizationData();
                    branch.NetworkSerialize(serializer);
                    Branches.Add(branch);
                }
            }
            else
            {
                for (int i = 0; i < BranchCount && i < Branches.Count; i++)
                    Branches[i].NetworkSerialize(serializer);
            }
        }
    }
}
