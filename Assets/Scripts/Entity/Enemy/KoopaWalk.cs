﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.Tilemaps;

public class KoopaWalk : HoldableEntity {
    [SerializeField] float walkSpeed, kickSpeed, wakeup = 15;
    [SerializeField] public bool red, blue, shell, stationary, hardkick, upsideDown;
    public bool left = true, putdown = false;
    public float wakeupTimer;
    private BoxCollider2D worldHitbox;
    Vector2 blockOffset = new Vector3(0, 0.05f);
    private float dampVelocity;
    new void Start() {
        base.Start();
        hitbox = GetComponentInChildren<BoxCollider2D>();
        worldHitbox = GetComponent<BoxCollider2D>();

        base.body.velocity = new Vector2(-walkSpeed, 0);
    }

    void FixedUpdate() {
        if (GameManager.Instance && GameManager.Instance.gameover) {
            base.body.velocity = Vector2.zero;
            base.body.angularVelocity = 0;
            base.animator.enabled = false;
            base.body.isKinematic = true;
            return;
        }

        base.sRenderer.flipX = !left;
        
        if (!base.dead) {
            if (upsideDown) {
                dampVelocity = Mathf.Min(dampVelocity + Time.fixedDeltaTime * 3, 1);
                transform.eulerAngles = new Vector3(
                    transform.eulerAngles.x, 
                    transform.eulerAngles.y, 
                    Mathf.Lerp(transform.eulerAngles.z, 180f, dampVelocity) + (wakeupTimer < 3 && wakeupTimer > 0 ? (Mathf.Sin(wakeupTimer * 120f) * 15f) : 0));
            } else {
                dampVelocity = 0;
                transform.eulerAngles = new Vector3(
                    transform.eulerAngles.x, 
                    transform.eulerAngles.y, 
                    (wakeupTimer < 3 && wakeupTimer > 0 ? (Mathf.Sin(wakeupTimer * 120f) * 15f) : 0));
            }
        }

        if (photonView && !photonView.IsMine)
            return;

        HandleTile();
        if (!blue) {
            base.animator.SetBool("shell", shell || holder != null);
            base.animator.SetFloat("xVel", Mathf.Abs(body.velocity.x));
        }

        if (shell) {
            if (stationary) {
                if ((wakeupTimer -= Time.fixedDeltaTime) < 0) {
                    if (photonView.IsMine)
                        photonView.RPC("WakeUp", RpcTarget.All);
                }

            } else {
                wakeupTimer = wakeup;
            }
        }

        if ((red || blue) && !shell && !Physics2D.Raycast(body.position + new Vector2(0.1f * (left ? -1 : 1), 0), Vector2.down, 0.5f, LayerMask.GetMask("Ground", "Semisolids"))) {
            if (photonView)
                photonView.RPC("Turnaround", RpcTarget.All, left);
            else
                Turnaround(left);
        }
        if (!stationary) {
            if (shell) {
                base.body.velocity = new Vector2(kickSpeed * (left ? -1 : 1) * (hardkick ? 1.2f : 1f), body.velocity.y);
            } else {
                base.body.velocity = new Vector2(walkSpeed * (left ? -1 : 1), body.velocity.y);
            }
        }
    }
    public override void InteractWithPlayer(PlayerController player) {
        Vector2 damageDirection = (player.body.position - body.position).normalized;
        bool attackedFromAbove = Vector2.Dot(damageDirection, Vector2.up) > 0f;
        if (holder) return;

        if (player.inShell || player.invincible > 0 || player.state == Enums.PowerupState.Giant) {
            photonView.RPC("SpecialKill", RpcTarget.All, !player.facingRight, false);
        } else if (player.groundpound && player.state != Enums.PowerupState.Mini && attackedFromAbove) {
            photonView.RPC("EnterShell", RpcTarget.All);
            if (!blue) {
                photonView.RPC("Kick", RpcTarget.All, player.body.position.x < body.position.x, player.groundpound);
                player.photonView.RPC("SetHoldingOld", RpcTarget.All, photonView.ViewID);
            }
        } else if (attackedFromAbove && (!shell || !IsStationary())) {
            if (player.state != Enums.PowerupState.Mini || player.groundpound) {
                photonView.RPC("EnterShell", RpcTarget.All);
                if (player.state == Enums.PowerupState.Mini)
                    player.groundpound = false;
            }
            player.photonView.RPC("PlaySound", RpcTarget.All, "enemy/goomba");
            player.bounce = true;
        } else {
            if (shell && IsStationary()) {
                if (!holder) {
                    if (player.state != Enums.PowerupState.Mini && !player.holding && player.running && !player.flying && !player.crouching && !player.dead && !player.onLeft && !player.onRight && !player.doublejump && !player.triplejump) {
                        photonView.RPC("Pickup", RpcTarget.All, player.photonView.ViewID);
                        player.photonView.RPC("SetHolding", RpcTarget.All, photonView.ViewID);
                    } else {
                        photonView.RPC("Kick", RpcTarget.All, player.body.position.x < body.position.x, player.groundpound);
                        player.photonView.RPC("SetHoldingOld", RpcTarget.All, photonView.ViewID);
                    }
                }
            } else {
                player.photonView.RPC("Powerdown", RpcTarget.All, false);
            }
        }
    }

    [PunRPC]
    public override void Kick(bool fromLeft, bool groundpound) {
        left = !fromLeft;
        stationary = false;
        hardkick = groundpound;
        base.body.velocity = new Vector2(kickSpeed * (left ? -1 : 1) * (hardkick ? 1.2f : 1f), (hardkick ? 3.5f : 0));
        photonView.RPC("PlaySound", RpcTarget.All, "enemy/shell_kick");
    }
    [PunRPC]
    public override void Throw(bool facingLeft, bool crouch) {
        if (holder == null) {
            return;
        }
        stationary = crouch;
        hardkick = false;
        transform.position = new Vector2(holder.transform.position.x, transform.position.y);
        this.holder = null;
        shell = true;
        photonView.TransferOwnership(PhotonNetwork.MasterClient);
        this.left = facingLeft;
        if (crouch) {
            base.body.velocity = new Vector2(2f * (facingLeft ? -1 : 1), body.velocity.y);
            putdown = true;
        } else {
            base.body.velocity = new Vector2(kickSpeed * (facingLeft ? -1 : 1), body.velocity.y);
        }
    }
    [PunRPC]
    public void WakeUp() {
        shell = false;
        base.body.velocity = new Vector2(-walkSpeed, 0);
        left = true;
        upsideDown = false;
        stationary = false;
        if (holder)
            holder.photonView.RPC("HoldingWakeup", RpcTarget.All);
        holder = null;
    }
    [PunRPC]
    public void EnterShell() {
        if (blue) {
            if (photonView.IsMine) {
                PhotonNetwork.Destroy(photonView);
            }
            if (PhotonNetwork.IsMasterClient) {
                PhotonNetwork.Instantiate("Prefabs/Powerup/BlueShell", transform.position, Quaternion.identity, 0, new object[]{0.1f});
            }
        }
        base.body.velocity = Vector2.zero;
        wakeupTimer = wakeup;
        shell = true;
        stationary = true;
    }

    void OnTriggerEnter2D(Collider2D collider) {
        if ((photonView && !photonView.IsMine) || !shell || IsStationary() || putdown) {
            return;
        }
        GameObject obj = collider.gameObject;
        switch (obj.tag) {
        case "koopa":
        case "bobomb":
        case "bulletbill":
        case "goomba": {
            KillableEntity killa = obj.GetComponentInParent<KillableEntity>();
            if (killa.dead) break;
            killa.photonView.RPC("SpecialKill", RpcTarget.All, killa.body.position.x > body.position.x, false);
            if (holder) {
                photonView.RPC("SpecialKill", RpcTarget.All, killa.body.position.x < body.position.x, false);
            }
            break;
        }
        case "piranhaplant": {
            KillableEntity killa = obj.GetComponentInParent<KillableEntity>();
            if (killa.dead) break;
            killa.photonView.RPC("Kill", RpcTarget.All);
            if (holder) {
                photonView.RPC("Kill", RpcTarget.All);
            }
            break;
        }
        }
    }

    void HandleTile() {
        if (holder)
            return;
        physics.Update();
        
        bool sound = false;
        ContactPoint2D[] collisions = new ContactPoint2D[20];
        int collisionAmount = worldHitbox.GetContacts(collisions);
        for (int i = 0; i < collisionAmount; i++) {
            var point = collisions[i];
            Vector2 p = point.point + (point.normal * -0.15f);
            if (Mathf.Abs(point.normal.x) > 0.2f) {
                
                if (photonView)
                    photonView.RPC("Turnaround", RpcTarget.All, point.normal.x > 0);
                else
                    Turnaround(point.normal.x > 0);

                if (!putdown && shell && !stationary) {
                    if (!sound) {
                        photonView.RPC("PlaySound", RpcTarget.All, "player/block_bump");
                        sound = true;
                    }
                    Vector3Int tileLoc = Utils.WorldToTilemapPosition(p + blockOffset);
                    TileBase tile = GameManager.Instance.tilemap.GetTile(tileLoc);
                    if (tile == null) continue;
                    if (!shell) continue;
                    
                    if (tile is InteractableTile) {
                        ((InteractableTile) tile).Interact(this, InteractableTile.InteractionDirection.Up, Utils.TilemapToWorldPosition(tileLoc));
                    }
                }
            } else if (point.normal.y > 0 && putdown) {
                body.velocity = new Vector2(0, body.velocity.y);
                putdown = false;
            }
        }
    }
    
    [PunRPC]
    void Turnaround(bool hitWallOnLeft) {
        if (stationary)
            return;
        left = !hitWallOnLeft;
        if (shell) {
            body.velocity = new Vector2(kickSpeed * (left ? -1 : 1) * (hardkick ? 1.2f : 1f), body.velocity.y);
        } else {
            body.velocity = new Vector2(walkSpeed * (left ? -1 : 1), body.velocity.y);
        }
    }

    [PunRPC]
    void Bump() {
        if (blue) {
            if (photonView.IsMine) {
                PhotonNetwork.Destroy(photonView);
            }
            if (PhotonNetwork.IsMasterClient) {
                PhotonNetwork.Instantiate("Prefabs/Powerup/BlueShell", transform.position, Quaternion.identity);
            }
            return;
        }
        if (!shell) {
            stationary = true;
            putdown = true;
        }
        wakeupTimer = wakeup;
        shell = true;
        upsideDown = true;
        base.body.velocity = new Vector2(body.velocity.x, 5.5f);
    }

    public bool IsStationary() {
        return !holder && stationary;
    }
    [PunRPC]
    public override void Kill() {
        EnterShell();
    }

    [PunRPC]
    public override void SpecialKill(bool right = true, bool groundpound = false) {
        base.SpecialKill(right, groundpound);
        shell = true;
        if (holder)
            holder.holding = null;
        holder = null;
    } 
}