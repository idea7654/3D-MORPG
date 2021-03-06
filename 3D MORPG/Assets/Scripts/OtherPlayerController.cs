using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class OtherPlayerController : MonoBehaviour
{
    [SerializeField]
    public Animator Animator {private set; get;}
    public PlayerState PlayerState {set; get;}
    public PlayerStateAttack PlayerStateAttack {set; get;}

    public int playerMove;
    public int playerAction;
    private int speed = 3;
    float xSpeed;
    float zSpeed;
    float yAngle;

    void Awake(){
        Animator = GetComponentInChildren<Animator>();

        PlayerState = PlayerState.Idle;
    }
    void Start()
    {
        playerMove = 0;
        playerAction = 0;
        yAngle = transform.rotation.y;
    }

    // Update is called once per frame
    void Update()
    {
        MoveByPacket();
        SimpleFSM();
    }

    void MoveByPacket()
    {
        if(playerMove == 0)
        {
            xSpeed = 0;
            zSpeed = 0;
            yAngle = 0;
            PlayerState = PlayerState.Idle;
        }
        if(playerMove == 3)
        {
            xSpeed = speed * Mathf.Sin(transform.eulerAngles.y * Mathf.PI / 180);
            zSpeed = speed * Mathf.Cos(transform.eulerAngles.y * Mathf.PI / 180);
            yAngle = 0;
            PlayerState = PlayerState.Move;
        }
        if(playerMove == 4)
        {
            xSpeed = -speed * Mathf.Sin(transform.eulerAngles.y * Mathf.PI / 180);
            zSpeed = -speed * Mathf.Cos(transform.eulerAngles.y * Mathf.PI / 180);
            yAngle = 0;
            PlayerState = PlayerState.Move;
        }
        if(playerMove == 1)
        {
            xSpeed = 0;
            zSpeed = 0;
            yAngle = -90;
            PlayerState = PlayerState.Move;
        }
        if(playerMove == 2)
        {
            xSpeed = 0;
            zSpeed = 0;
            yAngle = 90;
            PlayerState = PlayerState.Move;
        }
        if(playerMove == 5)
        {
            xSpeed = speed * Mathf.Sin(transform.eulerAngles.y * Mathf.PI / 180);
            zSpeed = speed * Mathf.Cos(transform.eulerAngles.y * Mathf.PI / 180);
            yAngle = -90;
            PlayerState = PlayerState.Move;
        }
        if(playerMove == 6)
        {
            xSpeed = speed * Mathf.Sin(transform.eulerAngles.y * Mathf.PI / 180);
            zSpeed = speed * Mathf.Cos(transform.eulerAngles.y * Mathf.PI / 180);
            yAngle = 90;
            PlayerState = PlayerState.Move;
        }
        if(playerMove == 7)
        {
            xSpeed = -speed * Mathf.Sin(transform.eulerAngles.y * Mathf.PI / 180);
            zSpeed = -speed * Mathf.Cos(transform.eulerAngles.y * Mathf.PI / 180);
            yAngle = -90;
            PlayerState = PlayerState.Move;
        }
        if(playerMove == 8)
        {
            xSpeed = -speed * Mathf.Sin(transform.eulerAngles.y * Mathf.PI / 180);
            zSpeed = -speed * Mathf.Cos(transform.eulerAngles.y * Mathf.PI / 180);
            yAngle = 90;
            PlayerState = PlayerState.Move;
        }
        
        if(playerAction == 0){
            PlayerStateAttack = PlayerStateAttack.None;
        }
        if(playerAction == 1){
            PlayerStateAttack = PlayerStateAttack.Attack;
        }

        transform.position += new Vector3(xSpeed, 0, zSpeed) * Time.deltaTime;
        //transform.rotation += Quaternion.Euler(new Vector3(0, yAngle, 0) * Time.deltaTime);
        //Debug.Log(yAngle);
        transform.Rotate(new Vector3(0, yAngle, 0) * Time.deltaTime);
    }
    private void SimpleFSM()
    {
        switch(PlayerState)
        {
            case PlayerState.Idle:
                //???????????????(0), ????????????(1)
                //Blend Tree: 0[stand_clam@loop] - 1[IdleCombatMode Blend Tree]
                Animator.SetFloat("movementSpeed", 0.0f);
                Animator.SetFloat("idleModeIsCombat", 0);

                //????????? ??????????????? 1 ?????? ??????????????? ????????? 0-1??? ???????????? ???????????? ????????? ???.
                //Blend Tree : ?????? 0%[stand_tired@loop] - 100%[Anim:Stand@loop]
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
