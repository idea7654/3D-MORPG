using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class Inventory : MonoBehaviour
{
    // Start is called before the first frame update
    private bool isShowing = false;
    private Network_Login network;
    public Texture hp_potion;

    void Start()
    {
        network = GameObject.Find("NetworkManager").GetComponent<Network_Login>();
        for(int i = 0; i < network.items.Count; i++){
            //1. 해당 name에 해당하는 이미지 찾기
            //2. 해당이미지를 image컴포넌트에 등록시키기
            //transform.GetChild(i).
            if(network.items[i].item_name == "hp_potion"){
                //transform.GetChild(0).transform.GetChild(1).transform.GetChild(0).transform.GetChild(0).transform.GetChild(i).transform.GetChild(0).GetComponent<RawImage>().Texture = hp_potion;
                Transform target = transform.GetChild(0).transform.GetChild(1).transform.GetChild(0).transform.GetChild(0).transform.GetChild(i);
                target.name = "hp_potion";
                target.GetChild(0).GetComponent<RawImage>().texture = hp_potion;
                target.GetChild(1).gameObject.SetActive(true);
                target.GetChild(1).GetComponent<TextMeshProUGUI>().text = network.items[i].count.ToString();
            }
        }
        transform.FindChild("InvenPanel").gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.I)){
            transform.FindChild("InvenPanel").gameObject.SetActive(!isShowing);
            isShowing = !isShowing;
        }
    }

    public void UpdatePlusInventory(int index)
    {
        network.items[index].count += 1;
        Transform target = transform.GetChild(0).transform.GetChild(1).transform.GetChild(0).transform.GetChild(0).transform.GetChild(index);
        target.GetChild(1).GetComponent<TextMeshProUGUI>().text = network.items[index].count.ToString();
    }

    public void UpdateMinusInventory(int index)
    {
        network.items[index].count -= 1;
        Transform target = transform.GetChild(0).transform.GetChild(1).transform.GetChild(0).transform.GetChild(0).transform.GetChild(index);
        target.GetChild(1).GetComponent<TextMeshProUGUI>().text = network.items[index].count.ToString();
    }
}
