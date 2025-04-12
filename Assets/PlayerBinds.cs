using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using InControl;

public class PlayerBinds : PlayerActionSet
{
    public PlayerAction Left, Right, Up, Down;
    public PlayerAction Jump, Sprint, Start, Select, Pound;
    public PlayerTwoAxisAction Move;

    public PlayerBinds()
    {
		if (Application.platform == RuntimePlatform.Switch){
			Left = CreatePlayerAction("Move Left");
			Right = CreatePlayerAction("Move Right");
			Down = CreatePlayerAction("Move Down");
			Up = CreatePlayerAction("Move UnityEnginep");
			Jump = CreatePlayerAction("Jump");
			Sprint = CreatePlayerAction("Sprint");
			Select = CreatePlayerAction("Select");
			Start = CreatePlayerAction("Start");
			Pound = CreatePlayerAction("Ground Pound");
			Move = CreateTwoAxisPlayerAction(Left, Right, Down, Up);
		}
    }
}