﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PiranhaPlantController : KillableEntity {
    public Vector2 playerDetectSize = new Vector2(3,3);
    public float popupTimerRequirement = 6f;
    public float popupTimer;
    private bool upsideDown;

    public new void Start() {
        base.Start();
        upsideDown = transform.eulerAngles.z != 0;
    }

    public new void Update() {
        if (GameManager.Instance && GameManager.Instance.gameover) {
            base.animator.enabled = false;
            return;
        }
        base.Update();

        if (photonView && !base.dead && photonView.IsMine && Utils.GetTileAtWorldLocation(transform.position + Vector3.down) == null) {
            photonView.RPC("Kill", RpcTarget.All);
            return;
        }

        base.animator.SetBool("dead", dead);
        if (base.dead || (photonView && !photonView.IsMine)) {
            return;
        }

        foreach (var hit in Physics2D.OverlapBoxAll(transform.transform.position + (Vector3) (playerDetectSize*new Vector2(0, (upsideDown ? -0.5f : 0.5f))), playerDetectSize, transform.eulerAngles.z)) {
            if (hit.transform.tag == "Player") {
                return;
            }
        }

        if ((popupTimer += Time.deltaTime) >= popupTimerRequirement) {
            base.animator.SetTrigger("popup");
            popupTimer = 0;
        }
    }

    public override void InteractWithPlayer(PlayerController player) {
        if (player.invincible > 0 || player.inShell || player.state == Enums.PowerupState.Giant) {
            photonView.RPC("Kill", RpcTarget.All);
        } else {
            player.photonView.RPC("Powerdown", RpcTarget.All, false);
        }
    }

    [PunRPC]
    public void Respawn() {
        base.dead = false;
        popupTimer = 3;
        base.hitbox.enabled = true;
    }

    [PunRPC]
    public override void Kill() {
        PlaySound("enemy/shell_kick");
        PlaySound("enemy/piranhaplant-die");
        base.dead = true;
        base.hitbox.enabled = false;
        Instantiate(Resources.Load("Prefabs/Particle/Puff"), transform.position + new Vector3(0, (upsideDown ? -0.5f : 0.5f), 0), Quaternion.identity);
        if (photonView.IsMine) {
            PhotonNetwork.Instantiate("Prefabs/LooseCoin", transform.position + new Vector3(0, (upsideDown ? -1f : 1f), 0), Quaternion.identity);
        }
    }
    
    void OnDrawGizmosSelected() {
        Gizmos.color = new Color(1, 0, 0, 0.5f);
        Gizmos.DrawCube(transform.transform.position + (Vector3) (playerDetectSize*new Vector2(0, (transform.eulerAngles.z != 0 ? -0.5f : 0.5f))), playerDetectSize);
    }
}
