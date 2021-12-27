using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float maxSpeed;
    float epsilon = 0.1f;
    float hMove, vMove;
    public int id = -1;
    public GameObject hand, pacman;
    public Sprite[] sprites;
    // Start is called before the first frame update

    public int health, maxHealth, damage = 1;

    public SpriteRenderer healthBar;
    Rigidbody2D rb;

    void Start() {
        rb = GetComponent<Rigidbody2D>();
        GetComponent<SpriteRenderer>().sprite = sprites[id-1];
        if (Shared.id != id) {
            this.enabled = false;
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        float hPower = Input.GetAxis("Horizontal");
        float vPower = Input.GetAxis("Vertical");

        hMove = 0;
        vMove = 0;
        if (Mathf.Abs(hPower) > epsilon)
        {
            hMove = Mathf.Sign(hPower) * maxSpeed;
        }

        if (Mathf.Abs(vPower) > epsilon)
        {
            vMove = Mathf.Sign(vPower) * maxSpeed;
        }

        var dir = Input.mousePosition - Camera.main.WorldToScreenPoint(transform.position);
        var angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90;
        if (hMove != 0 || vMove != 0 || Mathf.Abs((360 + angle) % 360 - hand.transform.rotation.eulerAngles.z) > 5f)
        {
            Move(new Vector2(hMove, vMove), angle);
        }
    }

    void Update() {
        if (Input.GetMouseButtonDown(0)) {
            if (Shared.networkType == Shared.NetworkType.host) {
                Vector3 v = new Vector3(hand.transform.position.x, hand.transform.position.y, 0);
                Quaternion q = Quaternion.AngleAxis(90 + hand.transform.rotation.eulerAngles.z, Vector3.forward);
                GameObject p = GameObject.Instantiate(pacman, v, q);
                p.transform.Translate(new Vector3(1,0,0));
                GameController.BroadcastPacman(p);
            } else {
                Move msg = new Move();
                msg.type = "shot_pacman";
                msg.x = transform.position.x;
                msg.y = transform.position.y;
                msg.angle = hand.transform.rotation.eulerAngles.z + 90;
                NetworkController.SendPacketToServer(msg);
            }
        }
        UpdateHealthBar();
    }

    public void UpdateHealthBar() {
        healthBar.transform.localPosition = new Vector3(-0.48f * (1 - (float)health/(float)maxHealth), 0, 0);
        healthBar.transform.localScale = new Vector3(0.01f + 0.96f * ((float)health/(float)maxHealth), 0.77f, 0);
        healthBar.color = new Color(1 - (float)health/(float)maxHealth, (float)health/(float)maxHealth, healthBar.color.b);
    }

    public void Move(Vector2 move, float angle) { // send message to server "i want to move"
        Move moveMsg = new Move();
        moveMsg.id = Shared.id;
        if (Shared.networkType == Shared.NetworkType.host) {
            rb.MovePosition(new Vector3(transform.position.x + move.x, transform.position.y + move.y, 0));
            hand.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            moveMsg.x = transform.position.x;
            moveMsg.y = transform.position.y;
            moveMsg.angle = angle;
            moveMsg.type = "render_move";
            NetworkController.BroadcastPacket(moveMsg);
        } else {
            moveMsg.x = move.x;
            moveMsg.y = move.y;
            moveMsg.angle = angle;
            moveMsg.type = "make_move";
            NetworkController.SendPacketToServer(moveMsg);
        }
    }
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!this.enabled) { return; }
        Debug.Log("Player "+ Shared.id + " was hit as " + Shared.networkType);
        Message msg = new Message();
        msg.id = Shared.id;
        if (other.gameObject.tag == "medkit" || other.gameObject.tag == "damage") {
            msg.type = other.gameObject.tag == "medkit" ? "heal" : "damage";
            int boosterId = other.gameObject.GetComponent<Booster>().id;
            msg.data["boosterId"] = boosterId.ToString();
            if (Shared.networkType == Shared.NetworkType.host) {
                GameController.HandleBooster(msg);
            } else {
                NetworkController.SendPacketToServer(msg);
            }
            return;
        }
        
        if (Shared.networkType == Shared.NetworkType.host) {
            GameController.HandleMeHit(msg);
        } else {
            msg.type = "me_hit";
            NetworkController.SendPacketToServer(msg);
        }
    }
}
