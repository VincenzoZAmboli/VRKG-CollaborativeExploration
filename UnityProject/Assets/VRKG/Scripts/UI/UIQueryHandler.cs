using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
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

///like 4 edges spawn but 4 query
public class UIQueryHandler : MonoBehaviour
{
    public UIQuerySelect selector;
    public TextMeshProUGUI Text;
    public int Index;

    private void Start()
    {
        selector.RegisterButton(gameObject, Text, Index);
    }

    public void OnClick()
    {
       selector.ExecuteQuery();
    }
}
