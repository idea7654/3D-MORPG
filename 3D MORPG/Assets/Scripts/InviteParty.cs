using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class InviteParty : MonoBehaviour
{
    // Start is called before the first frame update
    private Button button;
    private class Party{
        public string targetName;
        public string playerName;
        public string message;
    }
    private Party PartyPacket;
    private Slider slider;
    private Network_Login NetworkManager;
    void Start()
    {
        button = gameObject.GetComponent<Button>();
        button.onClick.AddListener(InviteToParty);
        PartyPacket = new Party();
        slider = GameObject.Find("HPBar").GetComponent<Slider>();
        NetworkManager = GameObject.Find("NetworkManager").GetComponent<Network_Login>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void InviteToParty()
    {
        string targetName = GameObject.Find("Name").GetComponent<Text>().text;
        string playerName = GameObject.Find("NetworkManager").GetComponent<Network_Login>().PlayerName;
        PartyPacket.targetName = targetName;
        PartyPacket.playerName = playerName;
        PartyPacket.message = "CreateParty";
        NetworkManager.SendPacket2CsServer(PartyPacket);
    }
}
