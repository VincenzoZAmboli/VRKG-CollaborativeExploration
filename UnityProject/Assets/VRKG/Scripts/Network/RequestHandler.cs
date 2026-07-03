using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;


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
    ""prompt"": ""stiamo lavorando sui knowledge graph, ti fornisco un csv di due colonne PredicateID-PredicateLabel, guardando il significato rispettivo di ogni predicato dal suo label, seleziona min.5 max.20 PredicateID dei predicati più significativi, che possono darci le informazioni più importanti e interessanti per l'entità: {0} che ha questa descrizione: {2} , seleziona SOLO i predicati STRETTAMENTE CORRELATI AL CONTESTO DATO IN DESCRIZIONE,  ESCLUDI A PRIORI TUTTO CIO CHE INIZIA CON -Identificativo di ... -  NON includere informazioni inutili (NIENTE NUMERI IDENTIFICATIVI,NO RIFERIMENTI A FILE AUDIO VIDEO ETC. ) , NO INTRO/OUTRO TEXT, NON MODIFICARE IN ALCUN MODO INPUT, RIPORTA ID SELEZIONATI ESATTAMENTE COME FORNITI, OUTPUT FINALE: SOLO PredicateID SEPARATI DA VIRGOLE (NON LABEL, SOLO ID NUMERICO). RISPONDI VELOCEMENTE SU QUESTO CSV:  {1} "" , 
    ""stream"": false,
    ""think"": false } ";
//da adattare tutto x generare query complesse o lasciamo stare e solo esplorazione coadiuvata da llm locale?

//chiama coroutine e ritorna direttamente array predicati 
    public string[] GetSignificantPredicates(string subject, string csv, string description)
    {   string[] predicates= {""};
        StartCoroutine(OllamaConnection(subject,csv,description,
        onSuccess =>
        {
            if(System.Text.RegularExpressions.Regex.IsMatch(onSuccess, @"^(\s+,)*\s+$"))
                predicates= onSuccess.Split(",");
            else
                Debug.Log("qwen ha fatto qualche stronzata");
        }, onError=>{
            Debug.Log("LLMNOTCONNECTED: "+ onError);
            //debug log e retry
        }));
        return predicates;
    }//si potrebbe togliere e usare solo la versione async, ma per ora lasciamo entrambe


    public static string[] ExtractPredicates(string jsonResponse)
    {
        try
        {
            JObject json = JObject.Parse(jsonResponse);
            string rawResponse = json["response"]?.ToString();//per prendere solo il campo response dal json di ritorno daollama
            if (string.IsNullOrEmpty(rawResponse))
            {
                return new string[0];
            }
            string[] predicates = rawResponse
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries) //separa virg
                .Select(p => p.Trim().ToUpper())// sparql case sensitive può dare problemi                        
                .ToArray();

            return predicates;
        }
        catch (Exception e)
        {
            Debug.LogError("Errore durante il parsing del JSON di Ollama: " + e.Message);
            return new string[0];
        }
    }




    public Task<string[]> GetSignificantPredicatesAsync(string subject, string csv, string description)
    {
        var tcs = new TaskCompletionSource<string[]>();

        StartCoroutine(OllamaConnection(subject, csv,description,
            onSuccess =>
            {
                if (string.IsNullOrWhiteSpace(onSuccess))
                {
                    tcs.TrySetResult(Array.Empty<string>());
                    return;
                }
                //parse the json response to extract the text field wt commas

                string[] preds = ExtractPredicates(onSuccess);
                //aggiungi check x errori ^^^
                Debug.Log("LLMCONNECTED: preds found:" + string.Join(", ", preds));
                tcs.TrySetResult(preds);
            },
            onError =>
            {
                Debug.LogError("LLMNOTCONNECTED: " + onError);
                tcs.TrySetException(new Exception(onError));
            }));

        return tcs.Task;
    }

//invia prompt modello locale, ricevei lista predicati  
    private IEnumerator OllamaConnection(string subject, string csv , string description, Action<string> onSuccess, Action<string> onError)
    {
        // Escape helper to make strings safe inside a JSON string literal
        string EscapeForJson(string s)
        {
            if (s == null) return "";
            return s.Replace("\\", "\\\\")
                    .Replace("\"", "\\\"")
                    .Replace("\r", "\\r")
                    .Replace("\n", "\\n")
                    .Replace("\t", "\\t");
        }

        string finalPayload= jsonPayload.Replace("{0}", EscapeForJson(subject));
        finalPayload=finalPayload.Replace("{1}", EscapeForJson(csv));
        finalPayload=finalPayload.Replace("{2}", EscapeForJson(description));

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
    
    

    public void SendWikiRequest(string EntityName, Action<string> onSuccess, Action<string> onError)
    {
        StartCoroutine(WDsearch(EntityName, onSuccess, onError));
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

            webRequest.timeout = 30;

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