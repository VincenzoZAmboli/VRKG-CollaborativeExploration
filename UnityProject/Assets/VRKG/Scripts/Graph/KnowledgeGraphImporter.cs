using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

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

/* Creates graphs and imports data from csv files */
public class KnowledgeGraphImporter : MonoBehaviour
{
    [HideInInspector]
    public KGTable Table;
    [HideInInspector]
    public KGDescriptor Graph;
    public int MaxCharactersNodeTitle;
#if UNITY_EDITOR
    public string TableFileLocation;
#endif

    public async void CreateGraphFromCSV(string csvFileName)
    {
        await Task.Run(() => GetTableFromCSV(csvFileName));
        await Task.Run(GetGraphFromTable);
    }
    
    public void CreateGraphFromCSVContent(string csvText)
    {
        GetTableFromCSVContent(csvText);
        GetGraphFromTable();
    }
    

    public void ClearTableAndGraph()//added for testing purposes to avoid confusion when adding subgraph
    {
        Table = new KGTable();
        //Graph = new KGDescriptor();///!!avoid err atln.151??
        //creare nuovo graph mi dà errore 
        //tecnicamente non è necessaraio crearea nuovo gdesc 
        //le liste vegnono gia pulite in GetGraphFromTable
    }




    void GetTableFromCSVContent(string csvText)
    {
        // split cells in a string[,]
        string[,] csv = SplitCsvGrid(csvText);
        // fill Table
        Table.Entries = new List<KGTableEntry>();
        for (int i = 1; i < csv.GetUpperBound(1); ++i)
        {
            KGTableEntry newEntry = new KGTableEntry();
            newEntry.Subject = csv[0, i];
            newEntry.SubjectLabel = csv[1, i];
            newEntry.SubjectComment = csv[2, i];
            newEntry.Predicate = csv[3, i];
            newEntry.PredicateLabel = csv[4, i];
            newEntry.Object = csv[5, i];
            newEntry.ObjectLabel = csv[6, i];
            Table.Entries.Add(newEntry);
        }
    }


    public void printTable()//vediamo se è il risulato a creare confusione quando aggiungo subgraph
    {
        foreach (var entry in Table.Entries)
        {
            Debug.Log($"Subject: {entry.Subject}, SubjectLabel: {entry.SubjectLabel}, SubjectComment: {entry.SubjectComment}, Predicate: {entry.Predicate}, PredicateLabel: {entry.PredicateLabel}, Object: {entry.Object}, ObjectLabel: {entry.ObjectLabel}");
        }
    }

    void GetTableFromCSV(string csvFileName)
    {
        string readText = File.ReadAllText(csvFileName);
        GetTableFromCSVContent(readText);//tolto ridondanza
    }

#if UNITY_EDITOR
    [ContextMenu ("GetTableFromCSV")]
    void GetTableFromCSV()
    {
        GetTableFromCSV(TableFileLocation);
        EditorUtility.SetDirty(this);
    }

    [ContextMenu ("GetGraphFromTable")]
    void GetGraphFromTable_Editor()
    {
        GetGraphFromTable();
        EditorUtility.SetDirty(this);
    }
#endif
    
    List<KGNode> GetSubjectNodes(List<KGTableEntry> allEntries)
    {
        List<KGNode> subjNodes = new List<KGNode>();
        
        foreach (var curEntry in allEntries.GroupBy(e => e.Subject).Select(g => g.First()))
        {
            KGNode newNode = new KGNode();
            newNode.ID = curEntry.Subject;
            newNode.Label = curEntry.SubjectLabel;
            newNode.Comment = curEntry.SubjectComment;
            subjNodes.Add(newNode);
        }
        return subjNodes;
    }
    
    void GetGraphFromTable()
    {
        List<string> subjectTitles = Table.Entries.Select(e => e.Subject).Distinct().ToList();
        List<KGNode> detailedNodes = GetSubjectNodes(Table.Entries);
        List<KGNode> endNodes = new List<KGNode>();
        List<KGEdge> edges = new List<KGEdge>();
        foreach (KGTableEntry curEntry in Table.Entries)
        {
            if (curEntry.Predicate != "rdfs:label" && curEntry.Predicate != "rdfs:comment")
            {
                KGEdge newEdge = new KGEdge();
                newEdge.Label = curEntry.PredicateLabel;
                newEdge.IDNode1 = curEntry.Subject;
                newEdge.IDNode2 = curEntry.Object;
                edges.Add(newEdge);
                if (!subjectTitles.Contains(newEdge.IDNode2))
                {
                    KGNode newNode = new KGNode();
                    newNode.ID = curEntry.Object;
                    newNode.Label = Utils.AddEllipsis(curEntry.ObjectLabel, MaxCharactersNodeTitle);
                    newNode.Comment = curEntry.ObjectLabel;
                    endNodes.Add(newNode);
                    subjectTitles.Add(newNode.ID);
                }
            }
        }
        Graph.Nodes.Clear();
        Graph.Nodes.AddRange(detailedNodes);
        Graph.Nodes.AddRange(endNodes);
        Graph.Edges.Clear();
        Graph.Edges.AddRange(edges);
    }
    
    static string[,] SplitCsvGrid(string csvText)
    {
        string[] lines = csvText.Split("\n"[0]);

        // finds the max width of row
        int width = 0;
        for (int i = 0; i < lines.Length; i++)
        {
            string[] row = SplitCsvLine(lines[i]);
            width = Mathf.Max(width, row.Length);
        }

        // creates new 2D string grid to output to
        string[,] outputGrid = new string[width, lines.Length];
        for (int y = 0; y < lines.Length; y++)
        {
            string[] row = SplitCsvLine(lines[y]);
            for (int x = 0; x < row.Length; x++)
            {
                outputGrid[x, y] = row[x];

                // This line was to replace "" with " in my output. 
                // Include or edit it as you wish.
                outputGrid[x, y] = outputGrid[x, y].Replace("\"\"", "\"");
            }
        }

        return outputGrid;
    }

    // splits a CSV row 
    static string[] SplitCsvLine(string line)
    {
        return (from System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(line,
                @"\s*(?:""(?<x>[^""]*(""""[^""]*)*)""\s*|(?<x>[^,;]*))(?:[;,]|$)",
                System.Text.RegularExpressions.RegexOptions.ExplicitCapture)
            select m.Groups[1].Value).ToArray();
    }
}
