using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

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



/* Manages a local file storage in a folder */
public class LocalQueryStorage : MonoBehaviour
{
    public string BaseFolder;
    public List<QueryEntry> CachedEntries;
    public bool ready;

    public string GetFullFilePath(string fileName)
    {
        return BaseFolder + fileName;
    }

    public bool IsReady
    {
        get
        {
            return ready;
        }
    }

    public void UpdateCache()
    {
        ready = false;
        UpdateCacheAsync();
    }
    
    async void UpdateCacheAsync()
    {
        await Task.Run(UpdateCachedEntries);
        ready = true;
    }

    void UpdateCachedEntries()
    {
        CachedEntries.Clear();
     
        var jsonFiles = Directory.GetFiles(BaseFolder, "*.json", SearchOption.TopDirectoryOnly);
        foreach (var curJsonFile in jsonFiles)
        {
            QueryEntry newEntry = JsonUtility.FromJson<QueryEntry>(File.ReadAllText(curJsonFile));
            if(File.Exists(BaseFolder + newEntry.CsvFileName))
                CachedEntries.Add(newEntry);
        }
    }

    public IEnumerator GetCachedEntries(UnityAction<List<QueryEntry>> callback)
    {
        if (CachedEntries.Count == 0)
        {
            UpdateCache();
            yield return new WaitUntil(() => ready);
        }

        callback(CachedEntries);
    }

    [ContextMenu ("Create Fake JSON")]
    void CreateFakeJson()
    {
        QueryEntry newEntry = new QueryEntry();
        newEntry.Name = "Van Gogh";
        newEntry.Preview = "Van Gogh Preview";
        newEntry.CsvFileName = "VanGoghKG.csv";
        string json = JsonUtility.ToJson(newEntry, true);
        File.WriteAllText(BaseFolder + "VanGoghKG.json", json);
    }
}
