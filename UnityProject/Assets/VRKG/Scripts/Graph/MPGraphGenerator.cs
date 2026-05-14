using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Photon.Pun;
using UnityEngine;
using ObjectManip = Microsoft.MixedReality.Toolkit.UI.ObjectManipulator;
using Random = UnityEngine.Random;

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

/* Handles asynchronous multi user graph generation in the scene*/
public class MPGraphGenerator : MonoBehaviourPun
{
    public int MaxNodes;
    public int MaxEdgesPerNode;
    public float FirstNodeAngle;
    public float FirstNodeDistance;
    public float AdjacentNodesDistance;
    public float OtherNodesDistance;
    public float OtherNodesMaxAngle;
    public SpringParameters SpringParams;
    
    public GameObject Parent;
    public Transform NodePrefab;
    public Transform EdgePrefab;
    public FocusHandler FocusHndlr;
    public GraphContainer GraphCont;
    public FileServerStorage Storage;
    public StartingAnimation StartAnim;
    public GraphicsProfileManager ProfilesManager;

    public KnowledgeGraphImporter KgImporter;
    public List<SpawnedNode> spawnedNodes;
    private List<KGNode> toSpawnNodes;
    private List<KGEdge> toSpawnEdges;
    private List<SpawnedNode> openNodes;
    private List<SpawnedNode> closedNodes;
    private bool semaphore;

    public void OnRoomCreated(QueryEntry query)
    {
        semaphore = false;
        Debug.Log("1");
        GenerateGraphFromCsv(query);
        Debug.Log("2");
        StartCoroutine(WaitAndGenerateGraph());
    }

    IEnumerator WaitAndGenerateGraph()
    {
        yield return new WaitUntil(() => semaphore);
        Debug.Log("5");
        yield return null;
        GenerateGraph();
        StartAnim.OnGraphCreated();
    }

    public void GenerateGraphFromCsv(QueryEntry query)
    {
        StartCoroutine(Storage.GetQueryContent(query, OnCsvRetrieved));
    }

    public async void OnCsvRetrieved(string csvText)
    {
        Debug.Log("3");
        await Task.Run(() => KgImporter.CreateGraphFromCSVContent(csvText));
        Debug.Log("4");
        semaphore = true;
    }

//using this one 4subgraph to make sure graph is generated only after csv 
     public async Task OnCsvRetrievedAsync(string csvText)
    {
        Debug.Log("3");
        await Task.Run(() => KgImporter.CreateGraphFromCSVContent(csvText));
        Debug.Log("4");
        semaphore = true;
    }

    //to avoid null errors on first spawn
    //and confusion with old nodes when creating subgraph
    //make separate funcion to reset everyreference to old graph 
    // before generating new subgraph
    public void resetEverything()//dachiamare prima di csvretreive 
    {
        //make funciotn in kg importer to clear its own graph data structures
        //to avoid confusion when adding subgraph
        KgImporter.ClearTableAndGraph();       
        //same with graph container
        GraphCont.ClearGraph();

        //celar all lists first and then create new
        spawnedNodes.Clear();
        toSpawnNodes.Clear();
        toSpawnEdges.Clear();
        openNodes.Clear();
        closedNodes.Clear();

    }


    
    Vector3 GetCentralPosition()
    {
        return FocusHndlr.FocusPoint + Quaternion.Euler(Random.Range(-FirstNodeAngle, FirstNodeAngle), Random.Range(-FirstNodeAngle, FirstNodeAngle), 0f) * new Vector3(0f, 0f, FirstNodeDistance);
    }
    
    void InitDataStructures()
    {   spawnedNodes = new List<SpawnedNode>();
        toSpawnNodes = new List<KGNode>();      
        toSpawnNodes.AddRange(KgImporter.Graph.Nodes);
        toSpawnEdges = new List<KGEdge>();
        toSpawnEdges.AddRange(KgImporter.Graph.Edges);
        openNodes = new List<SpawnedNode>();
        closedNodes = new List<SpawnedNode>();
    }
    
    void DeleteCurrentGraph()
    {
        for (int i = 0; i < Parent.transform.childCount; ++i)
        {
            Destroy(transform.GetChild(i).gameObject);
        }
    }
    
    SpawnedNode SpawnNode(KGNode node, Vector3 pos)
    {
        GameObject firstNodeGO = PhotonNetwork.Instantiate(NodePrefab.gameObject.name, pos, Quaternion.identity);
        PhotonView firstNodePhotonView = firstNodeGO.GetComponent<PhotonView>();
        photonView.RPC("InitNode", RpcTarget.AllBuffered, firstNodePhotonView.ViewID, node.Label, node.Comment);
        SpawnedNode newNodeSpawned = new SpawnedNode();
        newNodeSpawned.Node = node;
        newNodeSpawned.GO = firstNodeGO;
        spawnedNodes.Add(newNodeSpawned);
        toSpawnNodes.Remove(node);
        return newNodeSpawned;
    }
    
    SpawnedEdge SpawnEdge(KGEdge edge)
    {
        GameObject curEdge = PhotonNetwork.Instantiate(EdgePrefab.gameObject.name,Vector3.zero, Quaternion.identity);
        GameObject edgeNode1 = spawnedNodes.First(n => n.Node.ID == edge.IDNode1).GO;
        GameObject edgeNode2 = spawnedNodes.First(n => n.Node.ID == edge.IDNode2).GO;
        PhotonView curEdgePhotonView = curEdge.GetComponent<PhotonView>();
        photonView.RPC("InitEdge", RpcTarget.AllBuffered, curEdgePhotonView.ViewID, edge.Label, edgeNode1.GetPhotonView().ViewID, edgeNode2.GetPhotonView().ViewID);
        SpawnedEdge newEdgeSpawned = new SpawnedEdge();
        newEdgeSpawned.Edge = edge;
        newEdgeSpawned.GO = curEdge;
        toSpawnEdges.Remove(edge);
        return newEdgeSpawned;
    }
    
    SpawnedNode SpawnMainNodeSubject()
    {
        KGNode firstNode = toSpawnNodes.FirstOrDefault();
        if(firstNode != null)
            return SpawnNode(firstNode, GetCentralPosition());
        return null;
    }

    List<KGNodesEdge> FilterOutClosedNodes(List<KGNodesEdge> nodesEdges, KGNode baseNode)
    {
        List<KGNodesEdge> filteredNodesEdges = new List<KGNodesEdge>();
        foreach (var curNodesEdge in nodesEdges)
        {
            if (curNodesEdge.Node1 == baseNode)
            {
                if (spawnedNodes.FirstOrDefault(n => n.Node == curNodesEdge.Node2) == null)
                {
                    filteredNodesEdges.Add(curNodesEdge);
                }
            }
            else if (curNodesEdge.Node2 == baseNode)
            {
                if (spawnedNodes.FirstOrDefault(n => n.Node == curNodesEdge.Node1) == null)
                {
                    filteredNodesEdges.Add(curNodesEdge);
                }
            }
        }

        return filteredNodesEdges.Distinct().Take(MaxEdgesPerNode).ToList();
    }

    List<SpawnedNode> SpawnNodesAndEdgesAdjacentToNode(SpawnedNode node)
    {
        List<KGNodesEdge> adjacentNodes = KgImporter.Graph.GetNodesAndEdgesAdjacentToNode(node.Node);
        List<KGNodesEdge> filteredAdjacentNodes = FilterOutClosedNodes(adjacentNodes, node.Node);
        List<SpawnedNode> newNodes = new List<SpawnedNode>();
        for (int i = 0; i < filteredAdjacentNodes.Count && spawnedNodes.Count < MaxNodes; ++i)
        {
            Vector3 curNodeDirection = Quaternion.AngleAxis(360f / filteredAdjacentNodes.Count * i, Vector3.forward) * Vector3.right;
            Vector3 curNodePosition = node.GO.transform.position + curNodeDirection * AdjacentNodesDistance;
            KGNode curNode = filteredAdjacentNodes[i].Node1 == node.Node ? filteredAdjacentNodes[i].Node2 : filteredAdjacentNodes[i].Node1;
            SpawnedNode newSpawnedNode = SpawnNode(curNode, curNodePosition);
            newNodes.Add(newSpawnedNode);
            SpawnEdge(filteredAdjacentNodes[i].Edge);
        }
        return newNodes;
    }

    List<SpawnedNode> SpawnNonCentralNodesAndEdgesAdjacentToNode(SpawnedNode node)
    {
        List<KGNodesEdge> adjacentNodes = KgImporter.Graph.GetNodesAndEdgesAdjacentToNode(node.Node);
        List<KGNodesEdge> filteredAdjacentNodes = FilterOutClosedNodes(adjacentNodes, node.Node);
        List<SpawnedNode> newNodes = new List<SpawnedNode>();
        for (int i = 0; i < filteredAdjacentNodes.Count && spawnedNodes.Count < MaxNodes; ++i)
        {
            Vector3 adjEdgeDirection = (node.GO.transform.position - GetCentralPosition()).normalized;
            float edgeAngleFactor = filteredAdjacentNodes.Count % 2 == 0 ? Mathf.Floor((float)i / 2) + 0.5f : Mathf.Ceil((float)i / 2); 
            float edgeAngle = (OtherNodesMaxAngle / filteredAdjacentNodes.Count) * edgeAngleFactor;
            if (i % 2 == 0)
                edgeAngle *= -1;
            Vector3 curNodeDirection =
                Quaternion.AngleAxis(edgeAngle, Vector3.forward) * adjEdgeDirection;
            Vector3 curNodePosition = node.GO.transform.position + curNodeDirection * OtherNodesDistance;
            
            KGNode curNode = filteredAdjacentNodes[i].Node1 == node.Node ? filteredAdjacentNodes[i].Node2 : filteredAdjacentNodes[i].Node1;
            SpawnedNode newSpawnedNode = SpawnNode(curNode, curNodePosition);
            newNodes.Add(newSpawnedNode);

            SpawnEdge(filteredAdjacentNodes[i].Edge);
        }
        return newNodes;
    }
    
    public void GenerateGraph()
    {
        InitDataStructures();
        DeleteCurrentGraph();
        while (spawnedNodes.Count < MaxNodes && spawnedNodes.Count < KgImporter.Graph.Nodes.Count)
        {
            SpawnedNode firstNode = SpawnMainNodeSubject();
            if (firstNode != null)
            {
                openNodes.Add(firstNode);
                while (openNodes.Count > 0 && spawnedNodes.Count < MaxNodes)
                {
                    SpawnedNode curNode = openNodes[0];
                    List<SpawnedNode> newNodes;
                    if (closedNodes.Count == 0)
                    {
                        newNodes = SpawnNodesAndEdgesAdjacentToNode(curNode);
                    }
                    else
                    {
                        newNodes = SpawnNonCentralNodesAndEdgesAdjacentToNode(curNode);
                    }

                    openNodes.RemoveAt(0);
                    openNodes.AddRange(newNodes);
                    closedNodes.Add(curNode);
                }
            }

        }
        
    }


    public void GenerateGraphFromNode(SpawnedNode spawnpoint , string label)
    {  
        InitDataStructures();
        
        // Add spawnpoint back to spawnedNodes so SpawnEdge can find it
        spawnedNodes.Add(spawnpoint);

        //make new spawnposition
        Vector3 spawnNOdePOs=spawnpoint.GO.transform.position;
        Vector3 curNodeDirection = Quaternion.AngleAxis(360f, Vector3.forward) * Vector3.right;
        Vector3 curNodePosition = spawnNOdePOs + curNodeDirection * AdjacentNodesDistance;

        //salvo nodo centrale xdopo
        SpawnedNode centralNode =null;

        while (spawnedNodes.Count <= MaxNodes && spawnedNodes.Count < KgImporter.Graph.Nodes.Count)
        {
            SpawnedNode firstNode; 
            KGNode first = toSpawnNodes.FirstOrDefault();
            if(first != null)
                firstNode = SpawnNode(first, curNodePosition);
            else{
                Debug.LogError("No nodes to spawn");
                return;
            }
            if (firstNode != null)
            {
                openNodes.Add(firstNode);
                
                while (openNodes.Count > 0 && spawnedNodes.Count <= MaxNodes)
                {
                    SpawnedNode curNode = openNodes[0];
                    List<SpawnedNode> newNodes;
                    if (closedNodes.Count == 0)
                    {
                        newNodes = SpawnNodesAndEdgesAdjacentToNode(curNode);
                        centralNode= curNode;
                    }
                    else
                    {
                        newNodes = SpawnNonCentralNodesAndEdgesAdjacentToNode(curNode);
                    }

                    openNodes.RemoveAt(0);
                    openNodes.AddRange(newNodes);
                    closedNodes.Add(curNode);
                }

                // Create edge between spawnpoint and firstNode AFTER firstNode is spawned
                if(centralNode!=null){
                    KGEdge newEdge = new KGEdge();
                    newEdge.IDNode1 = spawnpoint.Node.ID;
                    newEdge.IDNode2 = centralNode.Node.ID;
                    newEdge.Label = label;
                    toSpawnEdges.Add(newEdge);
                    SpawnEdge(newEdge);
                }

            }
        }
    }

    
    [PunRPC]
    void InitNode(int nodeViewID, string title, string content)
    {
        GameObject node = PhotonView.Find(nodeViewID).gameObject;
        node.transform.parent = Parent.transform;
        NodeContent nodeContent = node.GetComponent<NodeContent>();
        nodeContent.Title.text = title;
        nodeContent.Text = content;
        node.name = title;
        NodeMaterialController matController = node.GetComponent<NodeMaterialController>();
        matController.FocusHndlr = FocusHndlr;
        matController.ProfilesManager = ProfilesManager;
        NodeManipulationEvents manipEvents = node.GetComponent<NodeManipulationEvents>();
        manipEvents.FocusHndlr = FocusHndlr;
        manipEvents.GraphCont = GraphCont;
        manipEvents.Init();
        ObjectManip manipulator = node.GetComponent<ObjectManip>();
        manipulator.OnHoverEntered.AddListener(FocusHndlr.OnNodeHover);
        manipulator.OnHoverExited.AddListener(FocusHndlr.OnNodeUnhover);
        GraphCont.RegisterNode(node);
    }
    
    [PunRPC]
    void InitEdge(int edgeViewID, string edgeName, int node1ViewID, int node2ViewID)
    {
        GameObject edge = PhotonView.Find(edgeViewID).gameObject;
        edge.name = edgeName;
        edge.transform.parent = Parent.transform;
        EdgeManager edgeMan = edge.GetComponent<EdgeManager>();
        edgeMan.Node1 = PhotonView.Find(node1ViewID).gameObject;
        edgeMan.Node2 = PhotonView.Find(node2ViewID).gameObject;
        edgeMan.GraphCont = GraphCont;
        edgeMan.Title = edge.name;
        EdgeContent edgeContent = edge.GetComponent<EdgeContent>();
        edgeContent.Text = edgeName;
        edgeContent.TitleUi.text = edgeName;
        AddSpring(edgeMan.Node2, edgeMan.Node1);
    }
    
    void AddSpring(GameObject nodeSource, GameObject nodeDest)
    {
        SpringJoint newSpring = nodeSource.AddComponent<SpringJoint>();
        newSpring.connectedBody = nodeDest.GetComponent<Rigidbody>();
        newSpring.anchor = SpringParams.Anchor;
        newSpring.spring = SpringParams.Spring;
        newSpring.damper = SpringParams.Damper;
        newSpring.minDistance = SpringParams.MinDistance;
        newSpring.maxDistance = SpringParams.MaxDistance;
        newSpring.autoConfigureConnectedAnchor = true;
    }
}
