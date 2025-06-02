using System.Collections;
using System.Globalization;
using UnityEngine;
using Unity.Netcode;
using static UnityEngine.UI.GridLayoutGroup;
public enum attackTypes { Kick, Punch, None }
public class CombatColliders : NetworkBehaviour
{
    public bool isAttacking = false;
    public attackTypes attackType = attackTypes.None;
    private Collider myCollider;
    private void Awake()
    {
        myCollider = GetComponent<Collider>();
    }
    private void OnTriggerEnter(Collider collision)
    {
        if (collision.tag != "Player" || !isAttacking) return;

        Transform root = collision.transform.root;
        if (root != transform.root)
        {
            PlayerControls player = root.gameObject.GetComponent<PlayerControls>();

            if (player != null)
            {
                if (attackType == attackTypes.Punch)
                {
                    if (myCollider.enabled)
                    {
                        if (IsOwner)
                        {
                            NetworkObject netObj = player.GetComponent<NetworkObject>();
                            if (netObj != null)
                            {
                                NetworkObjectReference netRef = new NetworkObjectReference(netObj);
                                player.ReceivePunchServerRpc(netRef); // or ReceiveKickServerRpc(netRef);
                            }
                        }
                        myCollider.enabled = false;
                    }
                }
                else if (attackType == attackTypes.Kick)
                {
                    if (myCollider.enabled)
                    {
                        if (IsOwner)
                        {
                            NetworkObject netObj = player.GetComponent<NetworkObject>();
                            if (netObj != null)
                            {
                                NetworkObjectReference netRef = new NetworkObjectReference(netObj);
                                player.ReceivePunchServerRpc(netRef); // or ReceiveKickServerRpc(netRef);
                            }
                        }
                        myCollider.enabled = false;
                    }
                }
            }
        }
    }
    public void StartAttack(attackTypes type)
    {
        switch(type)
        {
            case attackTypes.None:
                break;
            case attackTypes.Kick:
                attackType = attackTypes.Kick;
                StartCoroutine(Attack(0.9f));
                break;
            case attackTypes.Punch:
                attackType = attackTypes.Punch;
                StartCoroutine(Attack(0.5f));
                break;
        }
    }
    private IEnumerator Attack(float time)
    {
        myCollider.enabled = true;
        isAttacking = true;
        yield return new WaitForSeconds(time);
        isAttacking = false;
        attackType = attackTypes.None;
        myCollider.enabled = false;
    }
}