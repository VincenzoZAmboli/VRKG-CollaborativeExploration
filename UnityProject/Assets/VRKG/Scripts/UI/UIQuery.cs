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


public class UIQuery{
    public string Title; // serve x ui Prendere nome Predicate?
    public string sparqle_query;//da rimuovere tutto?    
    public QueryEntry entry; // Necessario x gen
    public UIQuery(string query){
        sparqle_query=query;
        
    } 

}