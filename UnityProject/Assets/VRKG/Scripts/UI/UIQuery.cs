using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

[Serializable]
public class QueryEntry //indica file csv da usare x gen inizale grafo
{ 
    public string Name;
    public string Preview;
    public string CsvFileName;
    public string GraphicsProfile;
}

public class UIQuery{//per costruzione query da ui
    public string Title; //Relazione che lega Nodo Origine a risultati query (nullo se nodo sorgente)
    public string nodeID; //ID wdt nodo di partenza (nullo se sorgente)
    public string sparqle_query;    //infovina un po'
    public QueryEntry entry; // obv serve solo se sorgente (forse non serve proprio qua ma se l'ho lasciato ci sta un mitvo poi vrimm)
    public UIQuery(string query){
        sparqle_query=query;
    } 

}