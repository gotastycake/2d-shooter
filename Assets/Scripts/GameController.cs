using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameController : MonoBehaviour
{
    // Start is called before the first frame update

    public GameObject playerPrefab, pacmanPrefab, wallPrefab, medkitPrefab, damagePrefab;
    public Text resultText;
    static List<GameObject> players = new List<GameObject>(); // spawn at (-15.5, 7.5), (15.5, 7.5), (-15.5, -7.5), (-15.5, -7.5)
    public static Dictionary<int, GameObject> packmans = new Dictionary<int, GameObject>(), boosters = new Dictionary<int, GameObject>();
    public static int packmansSpawned = 0, boostersSpawned;
    public static int playersAmount = 0;
    Vector3[] positions = {
        new Vector3(-15.5f, 7.5f, 0),
        new Vector3(-15.5f, -7.5f, 0),
        new Vector3(15.5f, 7.5f, 0),
        new Vector3(15.5f, -7.5f, 0),
    };

    void Awake()
    {
        resultText.gameObject.SetActive(false);
        if (Shared.networkType == Shared.NetworkType.host)
        {
            GameController.playersAmount = NetworkController.clients.Count + 1;
            Shared.id = 1;
            InitPlayers();
            Message msg = new Message();
            msg.type = "initgame";
            msg.data["players"] = playersAmount.ToString();
            NetworkController.BroadcastPacket(msg);
        }

        if (Shared.networkType == Shared.NetworkType.host) {
            for (int i = 0; i < 12; i ++) {
                int r = Random.value > 0.5f ? 90 : 0;
                Quaternion rotation = Quaternion.AngleAxis(r, Vector3.forward);
                Vector3 position = new Vector3(Random.Range(-13f, 13f), Random.Range(-6f, 6f), 0);

                GameObject.Instantiate(wallPrefab, position, rotation);

                Move msg = new Move();
                msg.type = "wall";
                msg.x = position.x;
                msg.y = position.y;
                msg.angle = r;
                NetworkController.BroadcastPacket(msg);
            }
        }
    }

    public void SpawnWall(Move msg) {
        Quaternion rotation = Quaternion.AngleAxis(msg.angle, Vector3.forward);
        Vector3 position = new Vector3(msg.x, msg.y, 0);

        GameObject.Instantiate(wallPrefab, position, rotation);
    }

    void Update()
    {
        if (playersAmount > 0 && players != null && players.Count == 0)
        {
            InitPlayers();
        }

        if (playersAmount == 1 && !resultText.gameObject.activeSelf) {
            resultText.gameObject.SetActive(true);
            resultText.text = "YOU WIN!";
        }
    }

    void FixedUpdate() {
        if (Shared.networkType != Shared.NetworkType.host) { return; }
        int r = Random.Range(0, 300);
        GameObject g;
        Booster b;
        if (r == 2) {
            Vector3 v = new Vector3(Random.Range(-10f, 10f), Random.Range(-5f, 5f), 0);
            g = GameObject.Instantiate(medkitPrefab, v, Quaternion.identity);
            b = g.GetComponent<Booster>();
            Move msg = new Move();
            msg.id = b.id;
            msg.type = "spawn_heal";
            msg.x = g.transform.position.x;
            msg.y = g.transform.position.y;
            NetworkController.BroadcastPacket(msg);
        }
        if (r == 3) {
            Vector3 v = new Vector3(Random.Range(-10f, 10f), Random.Range(-5f, 5f), 0);
            g = GameObject.Instantiate(damagePrefab, v, Quaternion.identity);
            b = g.GetComponent<Booster>();
            Move msg = new Move();
            msg.id = b.id;
            msg.type = "spawn_damage";
            msg.x = g.transform.position.x;
            msg.y = g.transform.position.y;
            NetworkController.BroadcastPacket(msg);
        }
    }

    public static void InitGame(Message msg)
    {
        int playersAmount = 0;
        bool ok = int.TryParse(msg.data["players"], out playersAmount);
        if (ok)
        {
            GameController.playersAmount = playersAmount;
        }
        else
        {
            NetworkController.LogMessage("Invalid players amount:", msg.data["players"]);
        }
    }

    void InitPlayers()
    {
        players = new List<GameObject>();
        for (int i = 0; i < playersAmount; i++)
        {
            GameObject player = GameObject.Instantiate(playerPrefab, positions[i], Quaternion.identity);
            player.GetComponent<PlayerController>().id = i + 1;
            players.Add(player);
        }
    }

    public static void RenderMove(Move move)
    {
        // NetworkController.LogMessage("New move, id:", move.id.ToString(), "x:", move.x.ToString(), "y:", move.y.ToString(), "angle:", move.angle.ToString());
        GameObject player = players[move.id - 1];
        Vector3 v = new Vector3(move.x, move.y, 0);
        player.transform.position = v;
        PlayerController pc = player.GetComponent<PlayerController>();
        pc.hand.transform.rotation = Quaternion.AngleAxis(move.angle, Vector3.forward);
    }

    public static void HandleMove(Move move)
    {
        GameObject player = players[move.id - 1];
        Vector3 v = new Vector3(player.transform.position.x + move.x, player.transform.position.y + move.y, 0);
        PlayerController pc = player.GetComponent<PlayerController>();
        pc.hand.transform.rotation = Quaternion.AngleAxis(move.angle, Vector3.forward);
        player.GetComponent<Rigidbody2D>().MovePosition(v);

        Move moveMsg = new Move();
        moveMsg.id = move.id;
        moveMsg.type = "render_move";
        moveMsg.x = player.transform.position.x;
        moveMsg.y = player.transform.position.y;
        moveMsg.angle = move.angle;
        NetworkController.BroadcastPacket(moveMsg);
    }

    public GameObject CreatePacman(Move move) {
        Vector3 v = new Vector3(move.x, move.y, 0);
        Quaternion q = Quaternion.AngleAxis(move.angle, Vector3.forward);
        GameObject p = GameObject.Instantiate(pacmanPrefab, v, q);
        p.transform.Translate(new Vector3(1,0,0));
        return p;
    }

    public static void BroadcastPacman(GameObject p) {
        Move msg = new Move();
        msg.type = "create_packman";
        msg.x = p.transform.position.x;
        msg.y = p.transform.position.y;
        msg.pid = Shared.id;
        msg.angle = p.transform.rotation.eulerAngles.z;

        NetworkController.BroadcastPacket(msg);
    }

    public static void HandleMeHit(Message msg) {
        GameObject player = players[msg.id-1];
        PlayerController pc = player.GetComponent<PlayerController>();
        pc.health -= 1;
        pc.UpdateHealthBar();
        NetworkController.LogMessage("Player", msg.id.ToString(), "was hit, current health", pc.health.ToString());
        Message newMsg = new Message();
        newMsg.id = msg.id;
        newMsg.type = pc.health <= 0 ? "kill" : "hit";
        newMsg.data["health"] = pc.health.ToString();
        NetworkController.BroadcastPacket(newMsg);
        if (newMsg.type == "kill") {
            Destroy(player);
            playersAmount -= 1;
        }
    }

    public static void HandleHit(Message msg) {
        NetworkController.LogMessage("Player", msg.id.ToString(), "was hit, handle:", msg.data["health"]);
        GameObject player = players[msg.id-1];
        PlayerController pc = player.GetComponent<PlayerController>();
        int.TryParse(msg.data["health"], out pc.health);
        pc.UpdateHealthBar();
    }

    public void HandleKill(Message msg) {
        Destroy(players[msg.id - 1]);
        if (msg.id == Shared.id) {
            resultText.gameObject.SetActive(true);
            resultText.text = "YOU LOSE!";
        }
        playersAmount -= 1;
    }

    public void ExitButton() {
        NetworkController.udp.Close();
        Application.Quit();
    }

    public static void HandleBooster(Message msg) {
        GameObject player = players[msg.id - 1];
        PlayerController pc = player.GetComponent<PlayerController>();
        Message newMsg = new Message();
        newMsg.id = msg.id;
        newMsg.data["boosterId"] = msg.data["boosterId"];
        if (msg.type == "heal") {
            pc.health = pc.maxHealth;
            pc.UpdateHealthBar();
            newMsg.data["health"] = pc.health.ToString();
        }
        if (msg.type == "damage") {
            pc.damage = 2;
            newMsg.data["damage"] = pc.damage.ToString();
        }

        int boosterId = 0;
        int.TryParse(msg.data["boosterId"], out boosterId);
        Destroy(boosters[boosterId]);
        newMsg.type = msg.type+"_up";
        NetworkController.BroadcastPacket(newMsg);
    }

    public static void HealthUp(Message msg) {
        PlayerController pc = players[msg.id - 1].GetComponent<PlayerController>();
        int.TryParse(msg.data["health"], out pc.health);
        pc.UpdateHealthBar();
        int boosterId = 0;
        int.TryParse(msg.data["boosterId"], out boosterId);
        Destroy(boosters[boosterId]);
    }

    public static void DamageUp(Message msg) {
        PlayerController pc = players[msg.id - 1].GetComponent<PlayerController>();
        int.TryParse(msg.data["damage"], out pc.damage);
        int boosterId = 0;
        int.TryParse(msg.data["boosterId"], out boosterId);
        Destroy(boosters[boosterId]);
    }

    public void SpawnHeal(Move msg) {
        GameObject.Instantiate(medkitPrefab, new Vector3(msg.x, msg.y, 0), Quaternion.identity);
    }

    public void SpawnDamage(Move msg) {
        GameObject.Instantiate(damagePrefab, new Vector3(msg.x, msg.y, 0), Quaternion.identity);
    }
}
