using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public enum PlayerState { Idle = 0, Move }

public class PlayerController : MonoBehaviour
{
    #region Variable & Property
    [SerializeField]
    private Camera mainCamera;
    public Animator Animator {private set; get;}
    public NavMeshAgent NavMeshAgent {private set; get;}
    public PlayerState PlayerState {set; get;}
    struct CharacterPosition{
        public float x;
        public float y;
        public float z;
        public float angle_x;
        public float angle_y;
        public float angle_z;
    }
    CharacterPosition charaPos;
    private Vector3 moveDirection;
    [SerializeField]
    public float speed = 5.0f;
    #endregion

    private void Awake(){
        Animator = GetComponentInChildren<Animator>();
        NavMeshAgent = GetComponent<NavMeshAgent>();
        PlayerState = PlayerState.Idle;
        charaPos.angle_x = transform.rotation.x;
        charaPos.angle_y = 180;
        charaPos.angle_z = transform.rotation.z;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //ObjectDetector(); //마우스 클릭으로 오브젝트 검출
        MoveCharacter();
        SimpleFSM(); //플레이어의 상태(PlayerState)에 따라 행동 [Idle, Move, Etc..]
    }

    private void ObjectDetector()
    {
        Ray ray;
        RaycastHit hit;
        float rayDistance = 50.0f;
        LayerMask layerMask = -1; // -1 = Everything

        if(Input.GetMouseButton(0))
        {
            ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if(Physics.Raycast(ray, out hit, rayDistance, layerMask))
            {
                if(Vector3.Distance(transform.position, hit.point) < 0.5f)
                //현재 플레이어가 서있는 곳이나 너무 가까운 곳을 클릭했을 때는 이동하지 않음.
                {
                    return;
                }
                PlayerState = PlayerState.Move; //플레이어 상태 설정
                Animator.SetFloat("movementSpeed", 1.0f); //애니메이션 설정
                NavMeshAgent.SetDestination(hit.point); //목표 위치 설정
            }
        }
    }

    private void MoveCharacter()
    {
        GetKey();
        moveDirection = new Vector3(charaPos.x, charaPos.y, charaPos.z);
        transform.position += moveDirection * Time.deltaTime;
        transform.rotation = Quaternion.Euler(new Vector3(0, charaPos.angle_y, 0));
    }

    private void GetKey()
    {
        if(Input.GetKey(KeyCode.LeftArrow)){
            charaPos.angle_y -= 3;
        }
        if(Input.GetKey(KeyCode.RightArrow)){
            charaPos.angle_y += 3;
        }
        if(Input.GetKey(KeyCode.UpArrow)){
            charaPos.x = speed * Mathf.Sin(charaPos.angle_y * Mathf.PI / 180);
            charaPos.z = speed * Mathf.Cos(charaPos.angle_y * Mathf.PI / 180);
            PlayerState = PlayerState.Move;
        }
        if(Input.GetKey(KeyCode.DownArrow)){
            charaPos.x = -speed * Mathf.Sin(charaPos.angle_y * Mathf.PI / 180);
            charaPos.z = -speed * Mathf.Cos(charaPos.angle_y * Mathf.PI / 180);
            PlayerState = PlayerState.Move;
        }
        if(!Input.GetKey(KeyCode.LeftArrow) && !Input.GetKey(KeyCode.RightArrow) && !Input.GetKey(KeyCode.UpArrow) && !Input.GetKey(KeyCode.DownArrow)){
            charaPos.x = 0.0f;
            charaPos.z = 0.0f;
            PlayerState = PlayerState.Idle;
        }
    }

    private void SimpleFSM()
    {
        switch(PlayerState)
        {
            case PlayerState.Idle:
                //비전투모드(0), 전투모드(1)
                //Blend Tree: 0[stand_clam@loop] - 1[IdleCombatMode Blend Tree]
                Animator.SetFloat("movementSpeed", 0.0f);
                Animator.SetFloat("idleModeIsCombat", 0);

                //두번째 매개변수에 1 대신 플레이어의 체력을 0-1로 정규화한 데이터를 넣어야 함.
                //Blend Tree : 체력 0%[stand_tired@loop] - 100%[Anim:Stand@loop]
                Animator.SetFloat("idleModeIsInjured", 1);
                break;
            case PlayerState.Move:
                // if((NavMeshAgent.destination - transform.position).sqrMagnitude < 0.01f)
                // {
                //     PlayerState = PlayerState.Idle;
                //     Animator.SetFloat("movementSpeed", 0.0f);
                //     transform.position = NavMeshAgent.destination;
                //     NavMeshAgent.ResetPath();
                // }
                Animator.SetFloat("movementSpeed", 1.0f);
                break;
        }
    }
}
