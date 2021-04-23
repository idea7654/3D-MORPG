using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System;
using System.Threading.Tasks;
public enum PlayerState { Idle = 0, Move }
public enum PlayerStateAttack {None = 0, Attack}

public class PlayerController : MonoBehaviour
{
    #region Variable & Property
    [SerializeField]
    //private Camera mainCamera;
    public Animator Animator {private set; get;}
    public NavMeshAgent NavMeshAgent {private set; get;}
    public PlayerState PlayerState {set; get;}
    public PlayerStateAttack PlayerStateAttack {set; get;}
    struct CharacterPosition{
        public float x;
        public float y;
        public float z;
        public float angle_x;
        public float angle_y;
        public float angle_z;
        public long currentTime;
        public PlayerMove playerMove;
        public string message;
        public string nickname;
        public PlayerStateAttack playerStateAttack;
    }
    CharacterPosition charaPos;
    private Vector3 moveDirection;
    public float speed = 3.0f;
    public enum PlayerMove{
        stop = 0,
        turn_left = 1,
        turn_right = 2,
        moveFront = 3,
        moveBack = 4,
        moveFrontLeft,
        moveFrontRight,
        moveBackLeft,
        moveBackRight
    };
    public PlayerMove before_move = PlayerMove.stop;
    public PlayerMove after_move = PlayerMove.stop;
    public PlayerStateAttack before_action = PlayerStateAttack.None;
    public PlayerStateAttack after_action = PlayerStateAttack.None;
    private Network_Login NetworkManager;
    private long Timer;
    #endregion

    private void Awake(){
        Animator = GetComponentInChildren<Animator>();
        //NavMeshAgent = GetComponent<NavMeshAgent>();

        PlayerState = PlayerState.Idle;
        PlayerStateAttack = PlayerStateAttack.None;
    }

    // Start is called before the first frame update
    void Start()
    {
        charaPos.nickname = GameObject.Find("NetworkManager").GetComponent<Network_Login>().PlayerName.ToString();
        charaPos.message = "PlayerMove";
        charaPos.x = transform.position.x;
        charaPos.y = transform.position.y;
        charaPos.z = transform.position.z;
        charaPos.angle_x = transform.eulerAngles.x;
        //charaPos.angle_y = transform.rotation.y;
        charaPos.angle_y = transform.eulerAngles.y;
        charaPos.angle_z = transform.eulerAngles.z;
        NetworkManager = GameObject.Find("NetworkManager").GetComponent<Network_Login>();
        StartCoroutine(CheckMove());
    }

    // Update is called once per frame
    void Update()
    {
        //ObjectDetector(); //마우스 클릭으로 오브젝트 검출
        MoveCharacter();
        SimpleFSM(); //플레이어의 상태(PlayerState)에 따라 행동 [Idle, Move, Etc..]
    }

    private void MoveCharacter()
    {
        GetKey();
        moveDirection = new Vector3(charaPos.x, 0, charaPos.z);
        transform.position += moveDirection * Time.deltaTime;
        //transform.rotation = Quaternion.Euler(new Vector3(0, charaPos.angle_y, 0) * Time.deltaTime);
        transform.Rotate(new Vector3(0, charaPos.angle_y, 0) * Time.deltaTime);
    }

    private void GetKey()
    {
        // if(charaPos.angle_y > 360){
        //     charaPos.angle_y = charaPos.angle_y - 360;
        // }
        // if(charaPos.angle_y < 0){
        //     charaPos.angle_y = charaPos.angle_y + 360;
        // }
        if(Input.GetKey(KeyCode.LeftArrow) && Input.GetKey(KeyCode.UpArrow))
        {
            charaPos.angle_y = -90;
            charaPos.x = speed * Mathf.Sin(transform.eulerAngles.y * Mathf.PI / 180);
            charaPos.z = speed * Mathf.Cos(transform.eulerAngles.y * Mathf.PI / 180);
            PlayerState = PlayerState.Move;
            charaPos.playerMove = PlayerMove.moveFrontLeft;
            before_move = PlayerMove.moveFrontLeft;
        }
        else if(Input.GetKey(KeyCode.RightArrow) && Input.GetKey(KeyCode.UpArrow))
        {
            charaPos.angle_y = 90;
            charaPos.x = speed * Mathf.Sin(transform.eulerAngles.y * Mathf.PI / 180);
            charaPos.z = speed * Mathf.Cos(transform.eulerAngles.y * Mathf.PI / 180);
            PlayerState = PlayerState.Move;
            charaPos.playerMove = PlayerMove.moveFrontRight;
            before_move = PlayerMove.moveFrontRight;
        }
        else if(Input.GetKey(KeyCode.RightArrow) && Input.GetKey(KeyCode.DownArrow))
        {
            charaPos.angle_y = 90;
            charaPos.x = -speed * Mathf.Sin(transform.eulerAngles.y * Mathf.PI / 180);
            charaPos.z = -speed * Mathf.Cos(transform.eulerAngles.y * Mathf.PI / 180);
            PlayerState = PlayerState.Move;
            charaPos.playerMove = PlayerMove.moveBackRight;
            before_move = PlayerMove.moveBackRight;
        }
        else if(Input.GetKey(KeyCode.LeftArrow) && Input.GetKey(KeyCode.DownArrow))
        {
            charaPos.angle_y = -90;
            charaPos.x = -speed * Mathf.Sin(transform.eulerAngles.y * Mathf.PI / 180);
            charaPos.z = -speed * Mathf.Cos(transform.eulerAngles.y * Mathf.PI / 180);
            PlayerState = PlayerState.Move;
            charaPos.playerMove = PlayerMove.moveBackLeft;
            before_move = PlayerMove.moveBackLeft;
        }
        else if(Input.GetKey(KeyCode.LeftArrow)){
            charaPos.angle_y = -90;
            charaPos.x = 0;
            charaPos.z = 0;
            charaPos.playerMove = PlayerMove.turn_left;
            before_move = PlayerMove.turn_left;
        }
        else if(Input.GetKey(KeyCode.RightArrow)){
            charaPos.angle_y = 90;
            charaPos.x = 0;
            charaPos.z = 0;
            charaPos.playerMove = PlayerMove.turn_right;
            before_move = PlayerMove.turn_right;
        }
        else if(Input.GetKey(KeyCode.UpArrow)){
            charaPos.x = speed * Mathf.Sin(transform.eulerAngles.y * Mathf.PI / 180);
            charaPos.z = speed * Mathf.Cos(transform.eulerAngles.y * Mathf.PI / 180);
            charaPos.angle_y = 0;
            PlayerState = PlayerState.Move;
            charaPos.playerMove = PlayerMove.moveFront;
            before_move = PlayerMove.moveFront;
        }
        else if(Input.GetKey(KeyCode.DownArrow)){
            charaPos.x = -speed * Mathf.Sin(transform.eulerAngles.y * Mathf.PI / 180);
            charaPos.z = -speed * Mathf.Cos(transform.eulerAngles.y * Mathf.PI / 180);
            charaPos.angle_y = 0;
            PlayerState = PlayerState.Move;
            charaPos.playerMove = PlayerMove.moveBack;
            before_move = PlayerMove.moveBack;
        }
        else{    
            charaPos.x = 0;
            charaPos.z = 0;
            charaPos.angle_y = 0;
            PlayerState = PlayerState.Idle;
            charaPos.playerMove = PlayerMove.stop;
            before_move = PlayerMove.stop;
        }

        if(Input.GetKeyDown(KeyCode.A))
        {
            if(DateTimeOffset.Now.ToUnixTimeMilliseconds() - Timer > 1000){
                PlayerStateAttack = PlayerStateAttack.Attack;
                before_action = PlayerStateAttack.Attack;
                Invoke("Change2Idle", 0.94f);
                Timer = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            }
        }
    }

    void Change2Idle()
    {
        PlayerStateAttack = PlayerStateAttack.None;
        before_action = PlayerStateAttack.None;
    }

    IEnumerator CheckMove()
    {
        while(true)
        {
            if(before_move != after_move)
            {
                charaPos.message = "PlayerMove";
                charaPos.x = transform.position.x;
                charaPos.y = transform.position.y;
                charaPos.z = transform.position.z;
                charaPos.angle_y = transform.eulerAngles.y;
                charaPos.currentTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                //Debug.Log(DateTimeOffset.Now.ToUnixTimeMilliseconds());
                NetworkManager.SendPacket2CsServer(charaPos);
            }

            if(before_action != after_action)
            {
                charaPos.message = "PlayerAction";
                charaPos.playerStateAttack = PlayerStateAttack;
                NetworkManager.SendPacket2CsServer(charaPos);
            }
            after_move = before_move;
            after_action = before_action;
            yield return null;
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
        switch(PlayerStateAttack)
        {
            case PlayerStateAttack.Attack:
                Animator.SetFloat("attackSpeed", 1.1f);
                break;
            case PlayerStateAttack.None:
                Animator.SetFloat("attackSpeed", 0.0f);
                break;
        }
    }
}
