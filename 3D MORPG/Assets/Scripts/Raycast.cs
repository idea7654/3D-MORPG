using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class Raycast : MonoBehaviour
{
    private GameObject target;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetMouseButtonDown(0)){
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if(Physics.Raycast(ray, out hit, 100))
            {
                if(hit.transform.gameObject.tag == "Player"){
                    //여기서 상대 정보 불러오기!!!!
                    GameObject.Find("InfoCanvas").transform.FindChild("InfoPanel").gameObject.SetActive(true);
                    Text text = GameObject.Find("Name").GetComponent<Text>();
                    text.text = hit.transform.gameObject.name;
                }else{
                    GameObject.Find("InfoCanvas").transform.FindChild("InfoPanel").gameObject.SetActive(false);
                }
            }
        }
    }
}
