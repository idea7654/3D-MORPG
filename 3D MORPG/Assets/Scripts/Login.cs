using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class Login : MonoBehaviour
{
    // Start is called before the first frame update
    public InputField Id;
    public InputField Password;
    public Button Button;
    public struct LoginForm
    {
        public string message;
        public string id;
        public string password;
    };
    public LoginForm loginInfo;
    void Start()
    {
        Button.onClick.AddListener(getLogin);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void getLogin()
    {
        loginInfo.message = "loginRequest";
        loginInfo.id = Id.text;
        loginInfo.password = Password.text;
        Network_Login manager = GameObject.Find("NetworkManager").GetComponent<Network_Login>();
        manager.SendPacket2Server(loginInfo);
    }
}
