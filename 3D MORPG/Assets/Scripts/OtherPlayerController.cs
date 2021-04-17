using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OtherPlayerController : MonoBehaviour
{

    public int playerMove;
    private int speed = 3;
    float xSpeed;
    float zSpeed;
    float yAngle;
    void Start()
    {
        playerMove = 0;
        yAngle = transform.rotation.y;
    }

    // Update is called once per frame
    void Update()
    {
        MoveByPacket();
    }

    void MoveByPacket()
    {
        if(playerMove == 0)
        {
            xSpeed = 0;
            zSpeed = 0;
            yAngle = 0;
        }
        if(playerMove == 3)
        {
            xSpeed = -speed * Mathf.Sin(transform.rotation.y * Mathf.PI / 180);
            zSpeed = -speed * Mathf.Cos(transform.rotation.y * Mathf.PI / 180);
            //yAngle = 0;
        }
        if(playerMove == 4)
        {
            xSpeed = speed * Mathf.Sin(transform.rotation.y * Mathf.PI / 180);
            zSpeed = speed * Mathf.Cos(transform.rotation.y * Mathf.PI / 180);
            //yAngle = 0;
        }
        if(playerMove == 1)
        {
            xSpeed = 0;
            zSpeed = 0;
            yAngle = 90;
        }
        if(playerMove == 2)
        {
            xSpeed = 0;
            zSpeed = 0;
            yAngle = -90;
        }

        transform.position += new Vector3(xSpeed, 0, zSpeed) * Time.deltaTime;
        //transform.rotation += Quaternion.Euler(new Vector3(0, yAngle, 0) * Time.deltaTime);
        transform.Rotate(new Vector3(0, yAngle, 0) * Time.deltaTime);
    }
}
