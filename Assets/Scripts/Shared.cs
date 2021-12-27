using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shared : MonoBehaviour
{
    // Start is called before the first frame update
    public enum NetworkType { host, client, none };
    public static NetworkType networkType = NetworkType.none;
    public static int id = 0;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
