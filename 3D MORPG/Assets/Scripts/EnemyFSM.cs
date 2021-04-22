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
    public Transform player;

    float chaseDistance = 5f; //몬스터가 추적을 시작할 거리(어그로)
    float attackDistance = 2.5f; //공격 시작할 거리
    float reChaseDistance = 3f; //플레이어가 도망갈 경우 얼마나 떨어져야 다시 추적
    float rotAnglePerSecond = 360f; //초당 회전각도
    float moveSpeed = 1.3f; //몬스터의 이동속도

    float attackDelay = 2f;
    float attackTimer = 0f;
    void Start()
    {
        myAni = GetComponent<EnemyAni>();
        ChangeState(State.Idle, EnemyAni.IDLE);

        Network_Login network = GameObject.Find("NetworkManager").GetComponent<Network_Login>();
        string playerName = network.PlayerName;
        player = GameObject.Find(playerName).transform;
    }

    void UpdateState()
    {
        switch(currentState)
        {
            case State.Idle:
                IdleState();
                break;
            case State.Chase:
                ChaseState();
                break;
            case State.Attack:
                AttackState();
                break;
            case State.Dead:
                DeadState();
                break;
            case State.NoState:
                NoState();
                break;
        }
    }

    public void ChangeState(State newState, string aniName)
    {
        if(currentState == newState)
        {
            return;
        }

        currentState = newState;
        myAni.ChangeAni(aniName);
    }

    void IdleState()
    {
        if(GetDistanceFromPlayer() < chaseDistance)
        {
            ChangeState(State.Chase, EnemyAni.WALK);
        }
    }

    void ChaseState()
    {
        if(GetDistanceFromPlayer() < attackDistance)
        {
            ChangeState(State.Attack, EnemyAni.ATTACK);
        }
        else{
            TurnToDestination();
            MoveToDestination();
        }
    }

    void AttackState()
    {
        if(GetDistanceFromPlayer() > reChaseDistance)
        {
            attackTimer = 0f;
            ChangeState(State.Chase, EnemyAni.WALK);
        }
        else{
            if(attackTimer > attackDelay)
            {
                transform.LookAt(player.position);
                myAni.ChangeAni(EnemyAni.ATTACK);

                attackTimer = 0f;
            }
            attackTimer += Time.deltaTime;
        }
    }

    void DeadState()
    {

    }

    void NoState()
    {

    }

    void TurnToDestination()
    {
        Quaternion lookRotation = Quaternion.LookRotation(player.position - transform.position);

        transform.rotation = Quaternion.RotateTowards(transform.rotation, lookRotation, Time.deltaTime * rotAnglePerSecond);
    }

    void MoveToDestination()
    {
        transform.position = Vector3.MoveTowards(transform.position, player.position, moveSpeed * Time.deltaTime);
    }

    float GetDistanceFromPlayer()
    {
        float distance = Vector3.Distance(transform.position, player.position);
        return distance;
    }
    // Update is called once per frame
    void Update()
    {
        UpdateState();
    }
}
