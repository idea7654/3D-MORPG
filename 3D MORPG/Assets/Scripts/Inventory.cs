using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    // Start is called before the first frame update
    private bool isShowing = false;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.I)){
            transform.FindChild("InvenPanel").gameObject.SetActive(!isShowing);
            isShowing = !isShowing;
        }
    }
}
