using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class EnemyFSM : MonoBehaviour
{
    // Start is called before the first frame update
    public enum State{
        Idle = 0,
        Chase,
        Attack,
        Dead,
        NoState
    }

    public State currentState = State.Idle;
    public EnemyAni myAni;
    //private Transform target;
    public string target;
    public float moveAngle = 0;

    float chaseDistance = 5f; //몬스터가 추적을 시작할 거리(어그로)
    float attackDistance = 2.5f; //공격 시작할 거리
    float reChaseDistance = 3f; //플레이어가 도망갈 경우 얼마나 떨어져야 다시 추적
    float rotAnglePerSecond = 360f; //초당 회전각도
    float moveSpeed = 1.5f; //몬스터의 이동속도

    float attackDelay = 2f;
    float attackTimer = 0f;
    public Network_Login network;
    public int StateA;
    public PlayerController playerController;
    public float PlayerAttackTime = 0f;
    public struct EnemyInfo{
        public float x;
        public float z;
        public float angle_y;
        public int id;
        public float damage;
        public State state;
        public string message;
    }
    public string enemyState;
    public EnemyInfo enemyInfo;
    public Vector3 targetPosition;
    public bool attack = false;
    public ParticleSystem hitEffect;
    void Start()
    {
        myAni = GetComponent<EnemyAni>();
        ChangeState(State.Idle, EnemyAni.IDLE);

        network = GameObject.Find("NetworkManager").GetComponent<Network_Login>();
        string playerName = network.PlayerName;
        playerController = GameObject.Find(playerName).GetComponent<PlayerController>();
        StateA = (int)playerController.PlayerStateAttack;
        enemyInfo = new EnemyInfo();
        //enemyInfo.hp = 3;
        enemyInfo.x = transform.position.x;
        enemyInfo.z = transform.position.z;
        enemyInfo.angle_y = transform.eulerAngles.y;
        enemyInfo.message = "EnemyAction";

        hitEffect = GameObject.Find("Spark").GetComponent<ParticleSystem>();
        hitEffect.Stop();
    }

    void UpdateState()
    {
        StateA = (int)playerController.PlayerStateAttack;
    }

    public void ChangeState(State newState, string aniName)
    {
        if(currentState == newState)
        {
            return;
        }else{
            enemyInfo.state = newState;
        }

        currentState = newState;
        myAni.ChangeAni(aniName);
    }
    void DeadState()
    {

    }

    void TurnToDestination()
    {
        Transform player = GameObject.Find(target).GetComponent<Transform>();
        Quaternion lookRotation = Quaternion.LookRotation(player.position - transform.position);

        transform.rotation = Quaternion.RotateTowards(transform.rotation, lookRotation, Time.deltaTime * rotAnglePerSecond);
    }

    void MoveToDestination()
    {
        Transform player = GameObject.Find(target).GetComponent<Transform>();
        //float distance = GetDistanceFromPlayer();
        if(GetDistanceFromPlayer() > 2f){
            transform.position = Vector3.MoveTowards(transform.position, player.position, moveSpeed * Time.deltaTime);
        }
        
        ChangeState(State.Chase, EnemyAni.WALK);
        //float x = Mathf.Sin(moveAngle / 180 * Mathf.PI) * 1.5f * Time.deltaTime;
        //float z = Mathf.Cos(moveAngle / 180 * Mathf.PI) * 1.5f * Time.deltaTime;
        //transform.position += new Vector3(x, 0, z);
    }

    float GetDistanceFromPlayer()
    {
        Transform player = GameObject.Find(target).transform;
        float distance = Vector3.Distance(transform.position, player.position);
        return distance;
    }
    // Update is called once per frame
    void Update()
    {
        Move();
        UpdateState();
    }

    void Move()
    {
        switch(enemyState)
        {
            case "Chase":
                //Chase();
                TurnToDestination();
                MoveToDestination();
                break;
            case "EnemyIdle":
                //Debug.Log("멈춤");
                Interpolation();
                break;
            case "Attack":
                Interpolation();
                TurnToDestination();
                AttackAnimation();
                break;
        }
    }

    void Interpolation(){
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
        ChangeState(State.Idle, EnemyAni.IDLE);
    }

    void AttackAnimation()
    {
        if(attack){
            myAni.ChangeAni(EnemyAni.ATTACK);
        }
    }

    public void ShowHitEffect()
    {
        Invoke("Play", 0.5f);
        Invoke("StopHitEffect", 1.5f);
    }

    void Play()
    {
        hitEffect.Play();
    }

    void StopHitEffect()
    {
        hitEffect.Stop();
    }
}

