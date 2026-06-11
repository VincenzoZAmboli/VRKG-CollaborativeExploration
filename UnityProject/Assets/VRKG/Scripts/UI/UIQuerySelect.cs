using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using System.Xml;
using System.Dynamic;
using System.Diagnostics;

using Debug = UnityEngine.Debug;
using System.Security.Cryptography.X509Certificates;
using System.Xml.Linq;
using System.Runtime.InteropServices;
//using System.Threading.Tasks.Dataflow;


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


//Esecuzione query su nodo in focus 


[Serializable]
public class UIQueryButton 
{
    public GameObject Parent;
    public TextMeshProUGUI Text;
}



public class UIQuerySelect : MonoBehaviour
{

    public Vector3 OffsetFromCamera;
    public FocusHandler FocusHndlr;
    public MPGraphGenerator generator;//local graph gen taken from 
    private GameObject UiContainer;
    private GameObject QueryInfo;
    private TextMeshProUGUI infoText;
    private List<UIQueryButton> buttons;
    private QueryEntry queryEntryT;
    private List<UIQuery> available_queries;
    private RequestHandler reqHandler;
    private GameObject focused_node;

    public SpawnedNode spawnPointNode;
    private int query_limit; 

   
    private void Awake()
    {
        buttons = new List<UIQueryButton>();
        available_queries = new List<UIQuery>();
        reqHandler = gameObject.AddComponent<RequestHandler>();

/////////////////////
/// FORMATO QUERY DEVE ESSERE:
/// (Subject,SubjectLabel,SubjectComment,Predicate,PredicateLabel,Object,ObjectLabel)
/// 
        // gen example query  //template generico
      /*  UIQuery test = new UIQuery("SELECT ?subject ?subjectLabel ?subjectComment ?predicate ?predicateLabel ?object ?objectLabel WHERE { " +
                                    "?subject ?predicate ?object. " +
                                    "?subject rdfs:label ?subjectLabel. " +
                                    "?subject rdfs:comment ?subjectComment. " +
                                    "?predicate rdfs:label ?predicateLabel. " +
                                    "?object rdfs:label ?objectLabel. " +
                                    "FILTER(LANG(?subjectLabel) = 'en' && LANG(?predicateLabel) = 'en' && LANG(?objectLabel) = 'en') " +
                                    "LIMIT 100");*/
        //modo per inserimento q

        UIQuery test = new UIQuery("SELECT ?Subject ?SubjectLabel (?CleanComment AS ?SubjectComment) ?Predicate ?PredicateLabel ?Object ?ObjectLabel "+
"WHERE {{ SELECT DISTINCT ?Subject WHERE {"+
    "?Subject wdt:P31 wd:Q5 ;"+
    " wdt:P27 wd:Q40 ;"+
    " wdt:P106 wd:Q1028181 .}LIMIT 10  }"+

  "BIND(wdt:P106 AS ?Predicate) BIND(wd:Q1028181 AS ?Object)"+

  "SERVICE wikibase:label { bd:serviceParam wikibase:language 'en'. "+ 
    "?Subject rdfs:label ?SubjectLabel ."+
    "?Subject schema:description ?RawComment ."+
    "?propEntity wikibase:directClaim ?Predicate ."+
    "?propEntity rdfs:label ?PredicateLabel ."+
   " ?Object rdfs:label ?ObjectLabel . }"+

  "BIND(REPLACE(STR(?RawComment), ';', ',') AS ?CleanComment)}");
        test.Title = "Wikidata connection test";
        /////////////RIMUOVERE TEST quando hai trovato modo di inserire query dinamicamente con criterio di scelta
        available_queries.Add(test);
////////////////////////////////////
        
}
    

    IEnumerator Start()
    {
        yield return null;

        Transform chTr = transform.Find("Content/UiContainer");
        if (chTr != null) {
            UiContainer= chTr.gameObject;
        }
        else{Debug.Log("NULLO CONT");}
            

        Transform childTransform = transform.Find("Content/QueryInfo");
        if (childTransform != null) {
            QueryInfo= childTransform.gameObject;
        }
        else{Debug.Log("NULLO QURY");}
            
        infoText = QueryInfo.gameObject.GetComponent<TextMeshProUGUI>();
        if(infoText == null){Debug.Log("Textnotfound");}


///////remove later when thay become ui pieces
UiSetQueryLimit(20);
//

        gameObject.SetActive(false);
        QueryInfo.SetActive(false);

        
        
    }
    
    public void OnJoinedRoom()
    {
        Vector3 focusPoint = Camera.main.transform.position + Camera.main.transform.forward * OffsetFromCamera.z 
                                                            + Camera.main.transform.right * OffsetFromCamera.x;
        transform.position = focusPoint;
        transform.LookAt(Camera.main.transform.position);
        transform.Rotate(0f, 180f, 0f);
        //ui piece not moving with camera, but always in front of it at a certain distance   

        


    }
  
    public void RegisterButton(GameObject obj, TextMeshProUGUI txt, int index)
    {
        while (index >= buttons.Count)
        {
            buttons.Add(null);
        }
        buttons[index] = new UIQueryButton{Parent = obj, Text = txt};
    }

///implement via ui??
    public void UiSetQueryLimit(int limit)
    {
        query_limit=limit;
    }
///////////////////////////////////////////////////////////////////////////


    public void OnNodeSelected(GameObject node)
    {
        focused_node= node;//4later query
        spawnPointNode = generator.spawnedNodes.FirstOrDefault(n => n.GO == focused_node);
        string nodeid= spawnPointNode.Node.ID;
        Debug.Log("Selected node ID: "+ nodeid);
/// aggiungi criterio selezione x cui su alcuni nodi certe query non si possono eseguire  
/// ex: flag su nodo "vicolo cieco" di modo che le query che restituirebbero res. nullo non appaiono proprio
/// 
        available_queries.Add(SparqlGenerator(nodeid,chosen_endpoint,query_limit)); //default query (typeof)
        available_queries.Add(SparqlGenerator(nodeid,chosen_endpoint,query_limit,false,true));//subclass
        

        gameObject.SetActive(true);
        updateList();


    }


    public void updateList()
    {
        buttons.ForEach(b => b.Parent.SetActive(false));
        //scegli query disponibili
        //criterio per scelta qury??
        for(int i = 0; i < available_queries.Count; ++i)
        {  //Arr out of bound err??
            buttons[i].Parent.SetActive(true); 
            buttons[i].Text.text = available_queries[i].Title;
        }
    }

    public void OnNodeUnselected()
    {
        //remove unselected queries (where UIquery.nodeid== spawnpintnode.id)
        available_queries.RemoveAll(q => q.nodeID == spawnPointNode.Node.ID);
        updateList();
        gameObject.SetActive(false);
    }

    public void ReactivateButtons()
    {
        UiContainer.SetActive(true);
        QueryInfo.SetActive(false);
    }


    ///!!!!REFACTOR BOTTONI X SCELTA ENTITà ALLINIZIO
    public void ExecuteQuery(GameObject but) //genera grafo con query (originalmente scelta query in base a bottone )
    {
        UIQueryButton selected_button= buttons.FirstOrDefault(b => b.Parent == but);
        
        if(selected_button!=null)
        {
            int qindex= buttons.IndexOf(selected_button);
            UIQuery selected_query= available_queries[qindex];
            Debug.Log("SELECTED query: "+ selected_query.sparqle_query);
            

            //rendi button momentaneamente invisibili 
            UiContainer.SetActive(false);
            QueryInfo.SetActive(true);

            infoText.text = "Executing query...";
 
            //Esecuzione query
            reqHandler.SendSparqlRequest(selected_query.sparqle_query,
                
                async onSuccess=>{
                    infoText.text = "Query executed!\nGenerating graph...";
                    Debug.Log("QUERY RESULT : \n"+ onSuccess);
                    await GraphGen(onSuccess,selected_query.Title);
                    available_queries.RemoveAt(qindex);
                    //riattiva 
                    Invoke("ReactivateButtons", 5f);
                    updateList();
                },
                
                onError=>{
                    Debug.LogError(onError);
                    infoText.text = "Query failed!";
                    //riattiva 
                    Invoke("ReactivateButtons", 3f);
                    }
            ); 
        }
    }


    public async Task GraphGen(string csv, string label)
    {
        //before resetting graph save spawn point node from cuurr cragh
        //so i can create edge between it and first node of new subgraph
        if (spawnPointNode == null)
        {
            Debug.LogError("Spawn point node not found");
            return;
        }
        
        generator.resetEverything();//testing if this avoids confusion when adding subgrap
        await generator.OnCsvRetrievedAsync(csv);//aggiungere controllo su formato query?
        generator.GenerateGraphFromNode(spawnPointNode,label);

        infoText.text = "Graph generated!";

    }


//////////////////////////////////////////
    // 1) funzione searchterm-> chiamata api->return json parsato passato a ui come opzioni

    /// 2)  scelta opzione in ui-> return entityid
    ///questo x gen da zero, da qua in poi è uguale x nodo già esistente con id


///////////AGGIUNGERE GESTIONE ERRORE XOGNI STEP PIPELINE
    public string ExplorationQuery(string entityID, string entityLabel)//prende entità di partenza e esegue pipeline di esplora<ione 
    {
        string predicateList;//xrisulato init vvv
        string explore="SELECT DISTINCT ?p ?pLabel WHERE { " +entityID + " ?prop ?statement . " +      
        " ?p wikibase:directClaim ?prop . SERVICE wikibase:label { bd:serviceParam wikibase:language 'it','en'. } }";
        //query x ottenere tutti i predicacati

        reqHandler.SendSparqlRequest(explore,
        onSuccess =>
        {
            Debug.Debug.Log("PIPELINE1: "+ onSuccess);
            predicateList=reqHandler.GetSignificantPredicates(entityLabel,onSuccess);//chiamata llm   
        }, onError=>{Debug.Log("PIPELINE1 QUERY FALLITA");});

        string[] Predicates;
        if (predicateList == null || predicateList == "error")
        {Debug.Log("PIPELINE2 FALLITA"); return "";}//interrompo qua x rendere op atomica
        else{
            Debug.Log("PIPELINE2: "+predicateList);
            Predicates=predicateList.Split(',',20);
        }//se nulla mi ha fermato fin qua tutto apposto??
        //ci potrebbero stare altre cose di predicatelist che non vanno bene
            
        //abbiamo tutto x fare query finale


        string final = "";
        ///da cambiare

        //foreach( string p in predicateList)
            //replace {EXP} with subject - p- obj
       
       

       return final;
    }


    
}
