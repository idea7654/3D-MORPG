using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class AlertToggle : MonoBehaviour
{
    // Start is called before the first frame update
    public Button button;
    void Start()
    {
        button.onClick.AddListener(onToggle);
    }

    void onToggle()
    {
        GameObject.Find("OverLogin").SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
