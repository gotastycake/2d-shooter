using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PacmanController : MonoBehaviour
{
    // Start is called before the first frame update
    public float maxSpeed = 0;
    public int id = 0;
    void Start()
    {
        id = ++GameController.packmansSpawned;
        GameController.packmans.Add(id, gameObject);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        transform.Translate(maxSpeed, 0, 0);
    }

    private void OnTriggerEnter2D(Collider2D other) {
        GameController.packmans.Remove(id);
        Destroy(gameObject);
    }
}
