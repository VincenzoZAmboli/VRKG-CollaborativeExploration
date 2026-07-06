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
    public string sparqle_query;    //nullo x qualsiasi op che non sia ExecuteQuery--
    //pessimo modo di fare refactoring dell'ogetto - chiedo scusa 
    public QueryEntry entry; // obv serve solo se sorgente (forse non serve proprio qua ma se l'ho lasciato ci sta un mitvo poi vrimm)
    public UIQuery(string query, string op="ExecuteQuery"){
        sparqle_query=query;
        operation=op;
    } 

///incluso anche qua perchè sennò non funziona 
/// Indica l'operazione che il bottone deve eseguire quando viene premuto
/// sto perdendo la capa è l'unico modo in cui sono riuscito a farlo scusatemi

    public string operation; 
    // LookupEntity - esegue ricerca del label inserito (per ora HC) su api wikidata (una volta rimosso questo btt non torna +)
    // ShowEntity - rappresenta un obj UIEntity- mostra descr.- disattiva tutti gli altri bottoni tranne i prossimi 2
    //          UnshowEntity  - descrizione rimossi - tornano tutti i bottoni
    //          SelectEntity - fa partire la pipeline di ricerca - aggiornamento tramite TMP
    // ExecuteQuery- (xDatatype originale a cui era destinato) alla fine se tutto va bene exe- e gen grafo.

    public UIEntity uiEntity;//obv può essere nullo se non c'entra con quell'operzione

}