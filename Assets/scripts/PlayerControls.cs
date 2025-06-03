using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
public enum directions { Left, Right }
public class PlayerControls : NetworkBehaviour
{
    #region Variables
    private NetworkVariable<float> hp = new NetworkVariable<float>(
    100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    [SerializeField] private float punchDMG = 5;
    [SerializeField] private float kickDMG = 10;
    [SerializeField] private float doubleTapThreshold = 0.3f;
    [SerializeField] private float dashForce = 20;
    [SerializeField] private Animator animator;
    [SerializeField] private float maxBlockDuration = 3f;
    [SerializeField] private float blockCooldown = 3f;
    [SerializeField] private Transform body;
    [SerializeField] private List<CombatColliders> combatComponents = new List<CombatColliders>();
    [SerializeField] private SpriteRenderer hpBar;
    [SerializeField] private SpriteRenderer hpBarFull;
    [SerializeField] private NetworkTransform head;
    
    private List<string> animationStates = new List<string>();

    private bool isBlocking = false;
    private bool canBlock = true;
    private float blockTimer = 0f;
    private float cooldownTimer = 0f;

    private float lastLeftTapTime = -1f;
    private float lastRightTapTime = -1f;

    [DoNotSerialize] public directions lookDirection = directions.Right;

    private Rigidbody rb;

    //private bool canChangeDirection = true;
    #endregion
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        animationStates.Add("Dash_F");
        animationStates.Add("Kick");
        animationStates.Add("Punch");
        animationStates.Add("Hit");
        animationStates.Add("KB");
        animationStates.Add("GetUp");
        animationStates.Add("Block");
    }
    void Update()
    {
        if (!IsOwner) return;

        HandleInput();

        UpdateBlockingTimers();
    }
    private void LateUpdate()
    {
        FixHPBars();
    }
    private bool CanWalk()
    {
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        foreach(string state in animationStates)
        {
            if (stateInfo.IsName(state))
            {
                return false;
            }
        }

        return true;
    }
    private void HandleInput()
    {
        Combat();

        Movement();
    }
    
    #region Movement

    #region Input
    private void Movement()
    {
        // Walking
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ||
            Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            DoWalking();

            // Dashing
            DetectDoubleTapDash();
        }

        // Idling
        else if (Input.GetAxis("Horizontal") == 0 && !Input.GetKey(KeyCode.Space) && !Input.GetMouseButtonDown(0) && !Input.GetMouseButtonDown(1))
        {
            StartCoroutine(SetTrigger("Idle"));
        }
    }
    #endregion

    #region walking
    private void DoWalking()
    {
        if (!CanWalk()) return;

        if (Input.GetAxis("Horizontal") < 0) 
        {
            body.localEulerAngles = new Vector3(0, 180, 0);
            lookDirection = directions.Left;
        }
        else 
        {
            body.localEulerAngles = new Vector3(0, 0, 0);
            lookDirection = directions.Right;
        }

        StartCoroutine(SetTrigger("Walk_F"));
    }
    #endregion

    #region dash
    private void DetectDoubleTapDash()
    {
        float now = Time.time;

        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            if (now - lastRightTapTime <= doubleTapThreshold)
            {
                Dash(directions.Right);
                rb.AddForce(Vector3.right * dashForce, ForceMode.VelocityChange);
                lastRightTapTime = -1f; // reset
            }
            else
            {
                lastRightTapTime = now;
            }
        }

        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            if (now - lastLeftTapTime <= doubleTapThreshold)
            {
                Dash(directions.Left);
                rb.AddForce(Vector3.left * dashForce, ForceMode.VelocityChange);
                lastLeftTapTime = -1f; // reset
            }
            else
            {
                lastLeftTapTime = now;
            }
        }
    }
    private void Dash(directions direction)
    {
        if (direction != lookDirection)
        {
            StartCoroutine(DashAndResume("Dash_B"));
        }

        else if (direction == lookDirection)
        {
            StartCoroutine(DashAndResume("Dash_F"));
        }
    }
    private IEnumerator DashAndResume(string dashTrigger)
    {
        animator.SetTrigger(dashTrigger);
        yield return new WaitForSeconds(0.12f); // Duration of dash anim
        animator.ResetTrigger(dashTrigger);

        // Check current input again to resume walking if still holding key
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ||
            Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            DoWalking(); // Resume walking animation
        }
        else
        {
            StartCoroutine(SetTrigger("Idle")); // Return to idle
        }
    }
    #endregion

    #endregion

    #region Combat
    private void Combat()
    {
        if (Input.GetMouseButtonDown(0))
        {
            foreach(CombatColliders component in combatComponents)
            {
                component.StartAttack(attackTypes.Punch);
            }
            StartCoroutine(SetTrigger("Punch"));
        }

        if (Input.GetMouseButtonDown(1))
        {
            foreach (CombatColliders component in combatComponents)
            {
                component.StartAttack(attackTypes.Kick);
            }
            StartCoroutine(SetTrigger("Kick"));
        }

        if (Input.GetKeyDown(KeyCode.Space) && canBlock)
        {
            StartBlocking();
        }
        if (Input.GetKeyUp(KeyCode.Space) && isBlocking)
        {
            StopBlocking();
        }
    }

    #region blocking
    private void StartBlocking()
    {
        isBlocking = true;
        blockTimer = 0f;
        StartCoroutine(SetTrigger("Block"));
    }
    private void StopBlocking()
    {
        isBlocking = false;
        canBlock = false;
        cooldownTimer = 0f;
        StartCoroutine(SetTrigger("Idle")); // Transition to idle or another state
    }
    private void UpdateBlockingTimers()
    {
        if (isBlocking)
        {
            blockTimer += Time.deltaTime;
            if (blockTimer >= maxBlockDuration)
            {
                StopBlocking();
            }
        }
        else if (!canBlock)
        {
            cooldownTimer += Time.deltaTime;
            if (cooldownTimer >= blockCooldown)
            {
                canBlock = true;
            }
        }
    }
    #endregion

    #region GettingHit
    public void HitByPunch()
    {
        if (isBlocking) return;
        StartCoroutine(SetTrigger("Hit"));
        hp.Value -= punchDMG;
        hp.Value = Mathf.Max(hp.Value, 0);
        UpdateHPBar(); // Local update

        if (hp.Value <= 0 && IsServer)
        {
            string winScene = (OwnerClientId == 0) ? "Player2Vic" : "Player1Vic";
            NetworkManager.SceneManager.LoadScene(winScene, LoadSceneMode.Single);
        }
    }
    public void HitByKick()
    {
        if (isBlocking) return;
        StartCoroutine(SetTrigger("Hit"));
        hp.Value -= kickDMG;
        hp.Value = Mathf.Max(hp.Value, 0);
        UpdateHPBar(); // Local update

        if (hp.Value <= 0 && IsServer)
        {
            string winScene = (OwnerClientId == 0) ? "Player2Vic" : "Player1Vic";
            NetworkManager.SceneManager.LoadScene(winScene, LoadSceneMode.Single);
        }
    }
    [ServerRpc(RequireOwnership = false)]
    public void ReceivePunchServerRpc(NetworkObjectReference targetRef, ServerRpcParams rpcParams = default)
    {
        if (targetRef.TryGet(out NetworkObject targetObj))
        {
            PlayerControls target = targetObj.GetComponent<PlayerControls>();
            if (target != null)
                target.HitByPunch();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void ReceiveKickServerRpc(NetworkObjectReference targetRef, ServerRpcParams rpcParams = default)
    {
        if (targetRef.TryGet(out NetworkObject targetObj))
        {
            PlayerControls target = targetObj.GetComponent<PlayerControls>();
            if (target != null)
                target.HitByKick();
        }
    }
    #endregion

    #endregion
    private IEnumerator SetTrigger(string trigger)
    {
        animator.SetTrigger(trigger);
        yield return new WaitForSeconds(0.2f);
        animator.ResetTrigger(trigger);
    }
    private void FixHPBars()
    {
        float y = hpBar.transform.position.y;
        hpBar.transform.position = new Vector3(head.transform.position.x, y, head.transform.position.z);
        hpBarFull.transform.position = new Vector3(head.transform.position.x, y, head.transform.position.z);
    }
    private void UpdateHPBar()
    {
        float percentage = hp.Value / 100f;
        hpBar.transform.localScale = new Vector3(3 * percentage, hpBar.transform.localScale.y, hpBar.transform.localScale.z);
    }
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        hp.OnValueChanged += OnHPChanged;
    }

    private void OnHPChanged(float oldVal, float newVal)
    {
        UpdateHPBar();
    }
}