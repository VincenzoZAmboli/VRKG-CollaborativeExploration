using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;

public class RequestHandler : MonoBehaviour
{
    // Per ora Wikidata endpoint (swappeabile con DBpedia o altri endpoint SPARQL)
    public string endpointUrl = "https://query.wikidata.org/sparql";

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
            webRequest.SetRequestHeader("Accept", "text/csv"); //Perchè ritornain xml e non csv?

            //wikidata e dbpedia richiedono header user agent per evitare blocchi
            // se va cambiato con info di unisa , mantieni formato ApplicationName/Version (ContactInfo)   
            webRequest.SetRequestHeader("User-Agent", "Vrkg/1.0 (Contact: your_email@example.com)");

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