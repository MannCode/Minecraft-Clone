using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Android;
using System.Threading;

public class testThread : MonoBehaviour
{
    // printTHreadClass printTHreadClass;
    // Start is called before the first frame update
    void Start()
    {
        for (int i = 0; i < 10; i++) {
            printTHreadClass printTHreadClass = new printTHreadClass(i);
            Thread t = new Thread(() => printTHreadClass.printIt());
            t.Start();
        }
    }
}
