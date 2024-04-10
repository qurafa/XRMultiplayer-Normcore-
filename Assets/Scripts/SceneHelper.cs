/************************************************************************************
Copyright : Copyright 2019 (c) Speak Geek (PTY), LTD and its affiliates. All rights reserved.

Developer : Dylan Holshausen

Script Description : Helper for Normcore Realtime Component

************************************************************************************/

using UnityEngine;

public class SceneHelper : MonoBehaviour
{
    [SerializeField]
    protected GameObject m_LocalPlayer;
    [SerializeField]
    protected GameObject m_Room; //Percept lab in the second scene
    [SerializeField]
    protected DataManager m_DataManager;

    protected Transform _spawnTransform;

    public virtual void Start()
    {
        PlayerAvatarSetUp();
    }

    //Realtime Event when Connecting to a Room
    public virtual void InitializePlayer()
    {
        m_LocalPlayer.transform.SetPositionAndRotation(_spawnTransform.position, _spawnTransform.rotation);
    }

    public void SetPlayerOffset(MyTransform offset)
    {
        _spawnTransform = new GameObject().transform;
        _spawnTransform.position = m_Room.transform.position + offset.position;//player related to the room's position
        _spawnTransform.eulerAngles = offset.eulerAngles; //tansform is the player's transform exactly where it was in the previous scene
        _spawnTransform.RotateAround(m_Room.transform.position, Vector3.up, -offset.rotAbt.y);
        Debug.Log($"PlayerCenterReference {_spawnTransform.transform.position.ToString()} Room {m_Room.transform.position}");
    }

    public void SetPlayerOffset(Transform offset)
    {
        SetPlayerOffset(new MyTransform(offset.position, offset.eulerAngles));
    }

    public void SetPlayerOffset(Vector3 pos, Quaternion rot)
    {
        SetPlayerOffset(new MyTransform(pos, rot.eulerAngles));
    }

    public virtual void JoinRoom(Transform transform)
    {
        SetPlayerOffset(transform);
        InitializePlayer();
    }

    public virtual void JoinRoom(MyTransform transform)
    {
        SetPlayerOffset(transform);
        InitializePlayer();
    }

    public virtual void JoinRoom(Vector3 pos, Quaternion rot)
    {
        SetPlayerOffset(pos, rot);
        InitializePlayer();
    }

    public virtual void PlayerAvatarSetUp()
    {
        AvatarInfoPub aPub = FindAnyObjectByType<AvatarInfoPub>();

        if(!aPub) aPub.SetPlayerID(0);
        m_DataManager.CreatePlayerFile(0);
    }
}