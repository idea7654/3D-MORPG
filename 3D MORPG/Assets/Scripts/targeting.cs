using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
public class targeting : MonoBehaviour
{
    // Start is called before the first frame update
    public GameObject Targeting;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Tab)){
            if(Targeting){
                Targeting.transform.FindChild("Cone").gameObject.SetActive(false);
            }
            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
            if(Targeting == null){
                foreach (GameObject item in enemies)
                {
                    float distance = Vector3.Distance(item.transform.position, transform.position);
                    if(Targeting == null){
                        Targeting = item;
                    }else{
                        if(distance < Vector3.Distance(Targeting.transform.position, transform.position)){
                            Targeting = item;
                        }
                    }
                }
            }else{
                if(enemies.Length > 1){
                    enemies = enemies.Where(val => val != Targeting).ToArray();
                    Targeting = null;
                    foreach (var item in enemies)
                    {
                        float distance = Vector3.Distance(item.transform.position, transform.position);
                        if(Targeting == null){
                            Targeting = item;
                        }else{
                            if(distance < Vector3.Distance(Targeting.transform.position, transform.position)){
                                Targeting = item;
                            }
                        }
                    }
                }
                
            }
            Targeting.transform.FindChild("Cone").gameObject.SetActive(true);
        }
    }
}
