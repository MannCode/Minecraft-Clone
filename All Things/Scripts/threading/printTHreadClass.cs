using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class printTHreadClass
{
    float count;

    public printTHreadClass(float count) {
        this.count = count;
    }
    public void printIt() {
        Debug.Log("Count: " + this.count);
    }
}
