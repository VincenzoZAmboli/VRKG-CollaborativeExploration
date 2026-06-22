using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Security.Cryptography.X509Certificates;


public class RequestHandler : MonoBehaviour
{
    //Wikidata endpoint 
    public string endpointUrl = "https://query.wikidata.org/sparql";
    //endpoint ricerca wkdata
    public string searchAPI="https://www.wikidata.org/w/api.php?action=wbsearchentities&format=json&language=it&search=";
    //endpoint ollama
    public string ollamaUrl = "http://localhost:11434/api/generate";
    public string jsonPayload = @"{
    ""model"": ""qwen3.5:9b"", 
    ""prompt"": ""Dato un csv di due colonne PredicateID-PredicateLabel, guardando il significato rispettivo di ogni predicato, seleziona min.5 max.15 ID dei predicati più importanti e significativi per l'entità {0} , NO INTRO/OUTRO TEXT, NON MODIFICARE IN ALCUN MODO INPUT, RIPORTA ID SELEZIONATI ESATTAMENTE COME FORNITI, OUTPUT FINALE: SOLO ID SEPARATI DA VIRGOLE. RISPONDI VELOCEMENTE SU QUESTO CSV: {1} "", 
    ""stream"": false,
    ""think"": false }";
//da adattare tutto x generare query complesse o lasciamo stare e solo esplorazione coadiuvata da llm locale?

//chiama coroutine e ritorna direttamente array predicati 
    public string[] GetSignificantPredicates(string subject, string csv)
    {   string[] predicates= {""};
        StartCoroutine(OllamaConnection(subject,csv,
        onSuccess =>
        {
            if(System.Text.RegularExpressions.Regex.IsMatch(onSuccess, @"^(\s+,)*\s+$"))
                predicates= onSuccess.Split(",");
            else
                Debug.Log("gwen ha fatto qualche stronzata");
        }, onError=>{
            Debug.Log("LLMNOTCONNECTED: "+ onError);
            //debug log e retry
        }));
        return predicates;
    }



//invia prompt modello locale, ricevei lista predicati  
    private IEnumerator OllamaConnection(string subject, string csv , Action<string> onSuccess, Action<string> onError)
    {
        string finalPayload= jsonPayload.Replace("{0}",subject);
        finalPayload=finalPayload.Replace("{1}",csv);

        using (UnityWebRequest request = new UnityWebRequest(ollamaUrl, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(finalPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();

            switch (request.result)
            {
                case UnityWebRequest.Result.ConnectionError:
                case UnityWebRequest.Result.DataProcessingError:
                case UnityWebRequest.Result.ProtocolError:
                    onError?.Invoke($"Error: {request.error}\nResponse: {request.downloadHandler.text}");
                    break;
                case UnityWebRequest.Result.Success:
                    Debug.Log("LLMCONNECTED");
                    onSuccess?.Invoke(request.downloadHandler.text);
                    break;
            }
        }
    }
    
    public void WikiSearch(string Entity)
    {
        WDsearch(Entity,onSuccess=>{}, onError=>{});
    }
    //ricerca enità x nome tramite wikidata api
    private IEnumerator WDsearch(string EntityName,Action<string> onSuccess, Action<string> onError)
    {
        using (UnityWebRequest request = new UnityWebRequest(searchAPI+EntityName, "POST"))
        {
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("User-Agent", "VRKG/1.0 (Contact: your_email@example.com)");

            yield return request.SendWebRequest();
            switch (request.result)
            {
                case UnityWebRequest.Result.ConnectionError:
                case UnityWebRequest.Result.DataProcessingError:
                case UnityWebRequest.Result.ProtocolError:
                    Debug.LogError($"WDAPIerror: {request.error}\nResponse: {request.downloadHandler.text}");
                    onError?.Invoke(request.downloadHandler.text);
                    break;
                case UnityWebRequest.Result.Success:
                    onSuccess?.Invoke(request.downloadHandler.text);      
                    break;
            }
        }
    }


    //effettiva esecuzione query
    /// "sparqlQuery" plain text SPARQL query - On success ritorna sempre come stringa - altrimenti onerror
  
    public void SendSparqlRequest(string sparqlQuery, Action<string> onSuccess, Action<string> onError)
    {
        StartCoroutine(ExecuteQuery(sparqlQuery, onSuccess, onError));
    }

    private IEnumerator ExecuteQuery(string query, Action<string> onSuccess, Action<string> onError)
    {
        // FORZARE CSV nell'url della query funziona per entrambi o basta header??
        string fullUrl = $"{endpointUrl}?query={UnityWebRequest.EscapeURL(query)}";

        using (UnityWebRequest webRequest = UnityWebRequest.Get(fullUrl))
        {
            webRequest.SetRequestHeader("Accept", "text/csv"); 

            //wikidata e dbpedia richiedono header user agent per evitare blocchi
            // se va cambiato con info di unisa , mantieni formato ApplicationName/Version (ContactInfo)   
            webRequest.SetRequestHeader("User-Agent", "VRKG/1.0 (Contact: your_email@example.com)");

            yield return webRequest.SendWebRequest();

            switch (webRequest.result)
            {
                case UnityWebRequest.Result.ConnectionError:
                case UnityWebRequest.Result.DataProcessingError:
                case UnityWebRequest.Result.ProtocolError:
                    onError?.Invoke($"Error: {webRequest.error}\nResponse: {webRequest.downloadHandler.text}");
                    break;
                case UnityWebRequest.Result.Success:
                    onSuccess?.Invoke(webRequest.downloadHandler.text);//csv pased to func as string
                    break;
            }
        }
    }
}