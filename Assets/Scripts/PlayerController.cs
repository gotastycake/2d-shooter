using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float maxSpeed;
    float epsilon = 0.1f;
    // Start is called before the first frame update

    Rigidbody2D rb;
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        float hPower = Input.GetAxis("Horizontal");
        float vPower = Input.GetAxis("Vertical");

        if (Mathf.Abs(hPower) > epsilon || Mathf.Abs(vPower) > epsilon) {
            rb.MovePosition(new Vector3(transform.position.x + maxSpeed * hPower, transform.position.y + maxSpeed * vPower, 0));
        }
    }
}
