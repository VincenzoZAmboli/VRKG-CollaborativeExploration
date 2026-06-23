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
using Newtonsoft.Json.Linq;
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

    public string operation; 
    // LookupEntity - esegue ricerca del label inserito (per ora HC) su api wikidata (una volta rimosso questo btt non torna +)
    // ShowEntity - rappresenta un obj UIEntity- mostra descr.- disattiva tutti gli altri bottoni tranne i prossimi 2
    //          UnshowEntity  - descrizione rimossi - tornano tutti i bottoni
    //          SelectEntity - fa partire la pipeline di ricerca - aggiornamento tramite TMP
    // ExecuteQuery- alla fine se tutto va bene exe- e gen grafo.

    public UIEntity uiEntity;//obv può essere nullo se non c'entra con quell'operzione
}

public class UIEntity
{
    public string ID,Label,Description;
    //build constructior for UIEntity
    public UIEntity(string id, string label, string description)
    {
        ID = id;
        Label = label;
        Description = description;
    }
}



public class UIQuerySelect : MonoBehaviour
{

    public Vector3 OffsetFromCamera;
    public FocusHandler FocusHndlr;
    public MPGraphGenerator generator;//local graph gen taken from 
    private GameObject UiContainer;
    private GameObject QueryInfo;
    public GameObject DescriptionPanel; // ui piece assigned from editor
    public GameObject DescriptionText; // modifiable text of said ui piece assigned from editor
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
        
        available_queries.Add(new UIQuery("","LookupEntity"){Title = "Lookup entity"});


////////////////////////////////
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
        test.operation = "ExecuteQuery";
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

       // gameObject.SetActive(false); //non + adesso mi serve attivo fin dall'inizio per ricevere input da microfono e fare ricerca su wikidata
        QueryInfo.SetActive(false);

        updateList();
        
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

        gameObject.SetActive(true);
        updateList();


    }


    public void updateList()
    {
        buttons.ForEach(b => b.Parent.SetActive(false));
        
        for(int i = 0; i < available_queries.Count; ++i)//change it so it's not sequential but based on text and operation

        {  //Arr out of bound err??
            buttons[i].Parent.SetActive(true); 
            buttons[i].Text.text = available_queries[i].Title;
            buttons[i].operation= available_queries[i].operation;
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

    public async Task WikiSend(string ItemLabel, Action<string> onSuccess, Action<string> onError)
    {
        reqHandler.SendWikiRequest(ItemLabel, onSuccess, onError);
        await Task.CompletedTask; // Await a completed task to satisfy the async method signature

    }

    public async Task WikiSearch(string ItemLabel)
    //takes the given parameter (item label) performs the WD search, maps the json result to UIEntity objects
    //than creates as many buttons with showentity as operation and  and adds them to availablequeries 
    {
       await WikiSend(ItemLabel,
            onSuccess => {
                List<UIEntity> entities = ParseWDSearchResult(onSuccess);
                entities.ForEach(e => {
                    UIQuery entityQuery = new UIQuery("", "ShowEntity") { Title = e.Label, uiEntity = e };
                    available_queries.Add(entityQuery); //qua vengono aggiunte correttamente le UIEntity ai bottoni, quindi il problema non è qua, ma quando seleziono un bottone, selected_button.uiEntity è null, perchè? 
                    Debug.Log("Added entity: " + e.Label + " with ID: " + e.ID+ " and description: " + e.Description);
                });
                updateList();
            },
            onError => {
                Debug.LogError("WikiSearch failed: " + onError);
            }
        );
        
    }

    public static List<UIEntity> ParseWDSearchResult(string jsonResponse)
    {
        List<UIEntity> entitiesList = new List<UIEntity>();

        try
        {
            // Analizza la stringa JSON in un oggetto navigabile
            JObject json = JObject.Parse(jsonResponse);
            
            // Accedi all'array "search" che contiene i risultati
            JArray searchResults = (JArray)json["search"];

            if (searchResults != null)
            {
                foreach (JToken item in searchResults)
                {
                    // 1. Estrazione ID (già pulito, es. "Q19970558")
                    string id = item["id"]?.ToString();

                    // 2. Estrazione Label (con fallback se mancante)
                    string label = item["label"]?.ToString() ?? "Senza nome";

                    // 3. Estrazione Description (spesso assente per entità minori)
                    string description = item["description"]?.ToString() ?? "";

                    // Creazione dell'istanza se l'ID è valido
                    if (!string.IsNullOrEmpty(id))
                    {
                        UIEntity newEntity = new UIEntity(id, label, description);
                        entitiesList.Add(newEntity);
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Errore durante il parsing del JSON di ricerca: " + e.Message);
        }

        return entitiesList;
    }


    public async Task runselectedoperation(GameObject but)
    {
       await selectOperation(but);
    }
    ///!!!!REFACTOR BOTTONI X SCELTA ENTITà ALLINIZIO
    public async Task selectOperation(GameObject but)
    {
        UIQueryButton selected_button = buttons.FirstOrDefault(b => b.Parent == but);
        
        // Find the corresponding UIQuery instead of relying on button.uiEntity
        int buttonIndex = buttons.IndexOf(selected_button);
        UIQuery selected_query = (buttonIndex >= 0 && buttonIndex < available_queries.Count) 
            ? available_queries[buttonIndex] 
            : null;

        switch (selected_button.operation)
        {
            case "LookupEntity":
                //ricevi input da microfono per ora simulato hard coded
////////////////////////per ora inserisci qua inout ricerca hard coded poi da mic
                await WikiSearch("Michelangelo");
                //await function completion and remove this button permanently from available_queries
                available_queries.RemoveAll(q => q.operation == "LookupEntity");
                updateList();
            break;
            case "ShowEntity":
                DescriptionPanel.SetActive(true);
                // Use selected_query.uiEntity instead of selected_button.uiEntity
                DescriptionText.GetComponent<TextMeshProUGUI>().text = 
                    selected_query?.uiEntity?.Description ?? "No description available";
                Debug.Log("Selected entity description: " + selected_query?.uiEntity?.Description);

                UIQuery unshowEntityQuery = new UIQuery("", "UnshowEntity") 
                { 
                    Title = "Unshow Entity", 
                    uiEntity = selected_query?.uiEntity 
                };
                UIQuery selectEntityQuery = new UIQuery("", "SelectEntity") 
                { 
                    Title = "Select Entity", 
                    uiEntity = selected_query?.uiEntity 
                };

                available_queries.Add(unshowEntityQuery);
                available_queries.Add(selectEntityQuery);
                updateList();
                buttons.ForEach(b => b.Parent.SetActive(b.operation == "UnshowEntity" || b.operation == "SelectEntity"));

                break;
            case "UnshowEntity":
                DescriptionPanel.SetActive(false);
                available_queries.RemoveAll(q => q.operation == "UnshowEntity" || q.operation == "SelectEntity");
                updateList();
                break;
            case "SelectEntity":
            //remove all buttons no one excluded well' put them back when it fails
                buttons.ForEach(b => b.Parent.SetActive(false));
                QueryInfo.SetActive(true);
                infoText.text = "Searching for significant predicates...";

                string entityID = selected_query?.uiEntity?.ID;
                string entityLabel = selected_query?.uiEntity?.Label;
                Debug.Log("Running exploration pipeline for entity ID: " + entityID + ", Label: " + entityLabel);

                string finalQuery = await ExplorationPipeline(entityID, entityLabel);
                infoText.text = "Predicati trovati! Query pronta per esecuzione";
                Debug.Log("Final Query: " + finalQuery);

                available_queries.Add(new UIQuery(finalQuery, "ExecuteQuery") { Title = "Execute Query" });
                updateList();
                break;
            case "ExecuteQuery":
                ExecuteQuery(but); //finalmente pd
                break;


        }
        


    } 
     
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
    public async Task<string> ExplorationQuery(string entityID)
    //prende entità di partenza(id) e esegue prima query esplorativa, restituisce il risultato della query come stringa
    {
        string explore="SELECT DISTINCT ?p ?pLabel WHERE {  wd:" +entityID + " ?prop ?statement . " +      
        " ?p wikibase:directClaim ?prop . SERVICE wikibase:label { bd:serviceParam wikibase:language 'it','en'. }  }";
        //query x ottenere tutti i predicacati

        var tcs = new TaskCompletionSource<string>();
        reqHandler.SendSparqlRequest(explore,
            onSuccess => {
                Debug.Log("PIPELINE1: " + onSuccess);
                tcs.TrySetResult(onSuccess);
            },
            onError => {
                Debug.Log("PIPELINE1 QUERY FALLITA: " + onError);
                tcs.TrySetException(new System.Exception(onError));
            }
        );

        return await tcs.Task;
    }


    public async Task<string[]> GetPredicatesFromRes(string expResult, string entityLabel)
    //prende il risultato query esplorativa, esegue chiamata al modello per prendere predicati importanti e li restituisce
    {
        string[] predicates = reqHandler.GetSignificantPredicates(entityLabel, expResult);
        return await Task.FromResult(predicates);
    }
    

    public async Task<string> ExplorationPipeline(string entityID, string entityLabel)
    //esegue chiamate asincrone delle funzioni di sparql e ollama, compone query finale e la restituisce come stringa
    {
        string valuesClause;
        //exe query esplorativa 
        string res = await ExplorationQuery(entityID);

        infoText.text = "Exploration query executed! Extracting significant predicates...";
        //estrai i predicati
        string[] preds = await GetPredicatesFromRes(res, entityLabel);
        infoText.text = "Significant predicates extracted! Composing final query...";

        if(preds != null && preds.Length > 0)
            valuesClause = string.Join(" ", preds.Select(p => $"wd:{p}"));
        else
            return "Error: No significant predicates found for the entity."; 

        //componi query finale
        return "string sparqlQuery = $@ " +
        " SELECT ?subject ?subjectLabel (?subjectDescription AS ?subjectComment) ?predicate (?propertyLabel AS ?predicateLabel) ?object ?objectLabel " +
        " WHERE {{ BIND(wd:{entityId} AS ?subject) . VALUES ?property {{ {valuesClause} }} . " +
        " ?property wikibase:directClaim ?predicate . ?subject ?predicate ?object . SERVICE wikibase:label {{ bd:serviceParam wikibase:language 'it,en'. }} }} " ;

    }
    
}


