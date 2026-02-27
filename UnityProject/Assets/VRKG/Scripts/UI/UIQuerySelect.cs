using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;


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

//Test for performing queries on focused node

[Serializable]
public class UIQueryButton
{
    public GameObject Parent;
    public TextMeshProUGUI Text;
}


//shows up on focus and shows possible queries on that node-- for now only test spawn node function

public class UIQuerySelect : MonoBehaviour
{

    public Vector3 OffsetFromCamera;
    public FocusHandler FocusHndlr;
    private List<UIQueryButton> buttons;

    private List<UIQuery> available_queries;
   
    private void Awake()
    {
        buttons = new List<UIQueryButton>();
        available_queries = new List<UIQuery>();
    }
    
    IEnumerator Start()
    {
        yield return null;
        gameObject.SetActive(false);
    }
    
    public void OnJoinedRoom()
    {
      


        Vector3 focusPoint = Camera.main.transform.position + Camera.main.transform.forward * OffsetFromCamera.z 
                                                            + Camera.main.transform.right * OffsetFromCamera.x;
        transform.position = focusPoint;
        transform.LookAt(Camera.main.transform.position);
        transform.Rotate(0f, 180f, 0f);
    }
  
    public void RegisterButton(GameObject obj, TextMeshProUGUI txt, int index)
    {
        while (index >= buttons.Count)
        {
            buttons.Add(null);
        }
        buttons[index] = new UIQueryButton{Parent = obj, Text = txt};
    }

    public void OnNodeSelected(GameObject node)
    {
        gameObject.SetActive(true);
        buttons.ForEach(b => b.Parent.SetActive(false));
        //scegli query disponibili
        //criterio per scelta qury??
          // example query
        UIQuery test = new UIQuery("test_query");
        available_queries.Add(test);
        //
        
        //qtest        
        for(int i = 0; i < available_queries.Count; ++i)
        {  //Arr out of bound err??
            buttons[i].Parent.SetActive(true);
            
            buttons[i].Text.text = available_queries[i].Title;
        }
    }

    public void OnNodeUnselected()
    {
        gameObject.SetActive(false);
    }

    public void ExecuteQuery(GameObject but)
    {
        int qindex= but.Index;
        UIQuery selected_query= available_queries[qindex];
        Debug.Log("query executed: " + selected_query.Title);
        //graph gen?
        
    }

}
