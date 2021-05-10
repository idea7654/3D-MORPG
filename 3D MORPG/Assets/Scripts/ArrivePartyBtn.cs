using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class ArrivePartyBtn : MonoBehaviour
{
    Button button;
    Network_Login networkManager;
    private class ArrivePacket{
        public string playerName;
        public string message;
    }
    // Start is called before the first frame update
    void Start()
    {
        button = GetComponent<Button>();
        networkManager = GameObject.Find("NetworkManager").GetComponent<Network_Login>();
        button.onClick.AddListener(ArriveParty);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void ArriveParty()
    {
        ArrivePacket arrivePacket = new ArrivePacket();
        arrivePacket.playerName = networkManager.PlayerName;
        arrivePacket.message = "ArriveParty";
        networkManager.SendPacket2CsServer(arrivePacket);
    }
}
