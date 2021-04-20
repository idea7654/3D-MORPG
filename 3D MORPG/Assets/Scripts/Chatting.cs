using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Chatting : MonoBehaviour
{
    private Text chatText = null;
    private ScrollRect scroll_rect = null;
    public InputField input;
    private bool onFocus = false;
    private struct ChatPacket{
        public string message;
        public string nickname;
        public string chat;
    }
    Network_Login network;
    // Start is called before the first frame update
    void Start()
    {
        chatText = GameObject.Find("Chat").GetComponent<Text>();
        scroll_rect = GameObject.Find("Scroll View").GetComponent<ScrollRect>();
        network = GameObject.Find("NetworkManager").GetComponent<Network_Login>();
    }

    // Update is called once per frame
    public void Update()
    {
        //if(Input.GetMouseButtonDown(0))
        //{
        //    chatText.text += "이예에에ㅔㅔ" + "\n";
        //    scroll_rect.verticalNormalizedPosition = 0.0f;
        //
        if(Input.GetKeyDown(KeyCode.Return)){
            //input.ActivateInputField();
            input.Select ();
            onFocus = !onFocus;
            ChatPacket ChatJson = new ChatPacket();
            ChatJson.message = "Chatting";
            ChatJson.chat = input.text;
            ChatJson.nickname = network.PlayerName;
            if(!onFocus)
            {
                //여기에 send이벤트 보내고
                network.SendPacket2CsServer(ChatJson);
                input.text = "";
            }
        }
    }

    public void AddChat(string nickname, string chat)
    {
        chatText.text += "[" + nickname + "]: " + chat + "\n";
        scroll_rect.verticalNormalizedPosition = 0.0f;
    }
}
