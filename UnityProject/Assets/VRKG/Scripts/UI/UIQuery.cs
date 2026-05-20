using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

[Serializable]
public class QueryEntry //Indispensabile?
{ //rendere graph gen indipendente
    public string Name;
    public string Preview;
    public string CsvFileName;
    public string GraphicsProfile;
}


public class Query_attributes
{public  String Descrizione,Tipo,Sottoclasse;};


public class WDatt : Query_attributes
{
    public String Descrizione = "schema:description";
    public String Tipo= "wdt:P31";
    public String Sottoclasse= "wdt:P279";
}

public class DBPatt : Query_attributes
{
    public String Descrizione = "rdfs:comment";
    public String Tipo= "rdf:type";
    public String Sottoclasse= "rdfs:subClassOf";
} 
 

public class UIQuery{
    public string Title; // serve x ui Prendere nome Predicate?
    public string nodeID; //per identificare query -selzionabili/da rimuovere- su una specifica entità
    public string sparqle_query;//da rimuovere tutto?    

    public Query_attributes type; 
    public QueryEntry entry; // Necessario x gen
    public UIQuery(string query){
        sparqle_query=query;
        
    } 

}