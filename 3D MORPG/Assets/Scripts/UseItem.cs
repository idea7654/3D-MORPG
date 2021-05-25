using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class UseItem : MonoBehaviour
{
    Button button;
    Network_Login network;
    public class ItemInfo{
        public string message;
        public string item_name;
        public string nickname;
    }
    ItemInfo itemInfo;
    // Start is called before the first frame update
    void Start()
    {
        button = GetComponent<Button>();
        itemInfo = new ItemInfo(); 
        network = GameObject.Find("NetworkManager").GetComponent<Network_Login>();
        button.onClick.AddListener(useItem);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void useItem()
    {
        itemInfo.message = "useItem";
        itemInfo.item_name = transform.name;
        itemInfo.nickname = network.PlayerName;
        network.SendPacket2CsServer(itemInfo);
    }
}
