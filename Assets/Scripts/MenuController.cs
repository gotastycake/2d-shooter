using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Text.RegularExpressions;

public class MenuController : MonoBehaviour
{
    // Start is called before the first frame update

    public GameObject page1, page2, page3, startGameButton, errorMsg;
    void Start()
    {
        page1.SetActive(true);
        page2.SetActive(false);
        page3.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void CreateButton() {
        EnablePage2();

        Shared.networkType = Shared.NetworkType.host;
    }
    
    public void ConnectButton() {
        EnablePage2();

        Shared.networkType = Shared.NetworkType.client;
    }

    void EnablePage2() {
        page1.SetActive(false);
        page2.SetActive(true);
    }

    public void BackToPage1Button() {
        page1.SetActive(true);
        page2.SetActive(false);

        Shared.networkType = Shared.NetworkType.none;
    }

    public void BackToPage2Button() {
        page2.SetActive(true);
        page3.SetActive(false);
        if (Shared.networkType == Shared.NetworkType.client) {
            Message msg = new Message();
            msg.type = "disconnect";
            msg.id = Shared.id;
            NetworkController.SendPacketToServer(msg);
        }
        NetworkController.udp.Close();
        NetworkController.udp = null;
    }

    public void StartButton() {
        NetworkController.StartGame();
        StartGame();
    }

    public static void StartGame() {
        SceneManager.LoadScene("Game");
    }

    public void NextButton() {
        string serverIP = GameObject.Find("IPText").GetComponent<Text>().text;
        if (serverIP == "") {
             serverIP = NetworkController.serverIP;
        }
        int port = 0;
        int.TryParse(GameObject.Find("PORTText").GetComponent<Text>().text, out port);
        if (port == 0) {
            port = NetworkController.port;
        }
        Regex ipCheck = new Regex("^(?:[0-9]{1,3}\\.){3}[0-9]{1,3}$");
        if (!ipCheck.IsMatch(serverIP)) {
            errorMsg.SetActive(true);
            return;
        }
        errorMsg.SetActive(false);

        NetworkController.serverIP = serverIP;
        NetworkController.port = port;
        page2.SetActive(false);
        page3.SetActive(true);

        switch (Shared.networkType) {
            case Shared.NetworkType.host:
                NetworkController.CreateServer();
                startGameButton.SetActive(true);
                break;
            case Shared.NetworkType.client:
                startGameButton.SetActive(false);
                NetworkController.ConnectToServer();
                // wait for load msg
                break;
        }
    }

    public static void IncConnectedAmount(Message msg) {
        int id = 0;
        bool ok = int.TryParse(msg.data["id"], out id);
        NetworkController.LogMessage("ID: ", id.ToString(), ok.ToString());
        if (ok) {
            Shared.id = id;
            GameObject.Find("ConnectedAmount").GetComponent<Text>().text = "Connected: " + id + "/4";
        }
    }
}
