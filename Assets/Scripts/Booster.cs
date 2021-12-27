using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Booster : MonoBehaviour
{
    // Start is called before the first frame update

    public int id;
    void Start()
    {
        id = ++GameController.boostersSpawned;
        GameController.boosters.Add(id, gameObject);
    }
}
