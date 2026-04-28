using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;

/*
 * MIT License

Copyright (c) 2023 Alberto Accardo, Daniele Monaco, Maria Angela Pellegrino, Vittorio Scarano, Carmine Spagnuolo

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

 */

/* stores the references to all nodes and edges present in the scene,
        forwards events and manipulates physics parameters*/
public class GraphContainer : MonoBehaviour
{
    public UIEdgesManager UIEdges;
    public StartingAnimation StartAnim;
    public OwnershipManager OwnershipMan;
    private List<GameObject> Nodes;
    private List<GameObject> Edges;

    private void Awake()
    {
        Nodes = new List<GameObject>();
        Edges = new List<GameObject>();
    }

    public void RegisterNode(GameObject node)
    {
        Nodes.Add(node);
        StartAnim.OnNodeRegistered(node);
    }
    
    public void UnregisterNode(GameObject node)
    {
        Nodes.Remove(node);
    }

    public void OnNodeGrabbed(GameObject node)
    {
        if (OwnershipMan.TrySetMeAsOwner())
        {
            foreach (GameObject curNode in Nodes)
            {
                if (curNode != node)
                {
                    curNode.GetComponent<NodeManipulationEvents>().OnOtherNodeGrabbed();
                }
            }
        }
    }

    public void OnNodeReleased(GameObject node)
    {
        OwnershipMan.ResetOwnership();
    }
    
    public void ResetAllMasses()
    {
        foreach (GameObject curNode in Nodes)
        {
            curNode.GetComponent<NodeManipulationEvents>().ResetInitialMass();
        }
    }
    
    public void SetAllManipulationEnabled(bool enabled)
    {
        foreach (GameObject curNode in Nodes)
        {
            curNode.GetComponent<NodeManipulationEvents>().SetManipulationEnabled(enabled);
        }
    }

    public void TransferNodeOwnershipToMe()
    {
        foreach (GameObject curNode in Nodes)
        {
            PhotonView view = curNode.GetPhotonView();
            if(!view.AmOwner)
                view.TransferOwnership(PhotonNetwork.LocalPlayer);
        }
    }

    public void RegisterEdge(GameObject edge)
    {
        Edges.Add(edge);
        UIEdges.RegisterEdge(edge.GetComponent<EdgeManager>());
        StartAnim.OnEdgeRegistered(edge);
    }
    
    public void UnregisterEdge(GameObject edge)
    {
        Edges.Remove(edge);
        UIEdges.UnregisterEdge(edge.GetComponent<EdgeManager>());
    }

    public void ClearGraph()
    {
       Nodes.Clear();//destroying nodes and edges is handled by the MPGraphGenerator, here we just clear the lists
       Edges.Clear();//testing if this avoids confusiion when adding subgraph
    }
    
}
