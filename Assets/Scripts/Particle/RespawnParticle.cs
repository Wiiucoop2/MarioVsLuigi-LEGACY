using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RespawnParticle : MonoBehaviour {

    float respawnTimer = 1.5f;
    public PlayerController player;

    void Update() {
        if (!player || !player.photonView.IsMine) return;
        if (respawnTimer > 0 && (respawnTimer -= Time.deltaTime) <= 0) {
            if (!player) return;
            player.photonView.RPC("Respawn", Photon.Pun.RpcTarget.All);
        }
    }
}
