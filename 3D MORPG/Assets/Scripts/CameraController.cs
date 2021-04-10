using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField]
    private Transform target;
    [SerializeField]
    private float minDistance = 2.0f; //target과 카메라의 최소거리
    [SerializeField]
    private float maxDistance = 20.0f; //target과 카메라의 최대 거리

    private float xSpeed = 250.0f; //카메라의 y축 회전 속도
    private float ySpeed = 120.0f; // 카메라의 x축 회전 속도
    private float yMinLimit = 5.0f; //카메라의 x축 최소 각도
    private float yMaxLimit = 80.0f; //카메라의 x축 최대 각도
    private float wheelSpeed = 500.0f; //카메라의 Zoom 속도
    private float x, y;
    private float distance;

    private void Awake()
    {
        distance = Mathf.Lerp(minDistance, maxDistance, 0.5f);

        Vector3 angles = transform.eulerAngles;
        x = angles.y;
        y = angles.x;
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(!target)
        {
            return;
        }
        if(Input.GetMouseButton(1))
        {
            UpdateRotate();
        }
        UpdatePosition();
    }

    private void UpdateRotate()
    {
        x += Input.GetAxisRaw("Mouse X") * xSpeed * Time.deltaTime;
        //마우스의 x축 위치 변화를 바탕으로 카메라의 y축 회전 값 설정
        y -= Input.GetAxisRaw("Mouse Y") * ySpeed * Time.deltaTime;
        //마우스의 y축 위치 변화를 바탕으로 카메라의 x축 회전 값 설정
        y = ClampAngle(y, yMinLimit, yMaxLimit);
        
        transform.rotation = Quaternion.Euler(y, x, 0);
    }

    private float ClampAngle(float angle, float min, float max)
    {
        if(angle < -360) angle += 360;
        if(angle > 360) angle -= 360;

        return Mathf.Clamp(angle, min, max);
    }

    private void UpdatePosition()
    {
        distance -= Input.GetAxisRaw("Mouse ScrollWheel") * wheelSpeed * Time.deltaTime;
        distance = Mathf.Clamp(distance, minDistance, maxDistance);

        transform.position = target.position + transform.rotation * Vector3.back * distance;
    }
}
