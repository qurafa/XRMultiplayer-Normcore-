/************************************************************************************
Copyright : Copyright 2019 (c) Speak Geek (PTY), LTD and its affiliates. All rights reserved.

Developer : Dylan Holshausen

Script Description : Helper for Normcore Realtime Component

************************************************************************************/

using UnityEngine;
using Normal.Realtime;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class RealtimeSceneHelper : SceneHelper
{
    [Header("REALTIME")]
    [SerializeField]
    protected string m_RoomName;
    [SerializeField]
    private string m_RealtimePlayerPrefab;
    [SerializeField]
    private RawImage m_ConnectionStatus;
    [SerializeField]
    List<string> m_InteractablePrefabNames;
    [SerializeField]
    private Color ROOM_NOT_CONNECTED = Color.red;
    [SerializeField]
    private Color ROOM_CONNECTING = Color.yellow;
    [SerializeField]
    private Color ROOM_CONNECTED = Color.green;

    private Realtime m_Realtime;

    public override void Start()
    {
        m_Realtime = GetComponent<Realtime>();
        if (!m_Realtime)
        {
            Debug.Log($"{gameObject.name} does not have Realtime component attached");
            return;
        }

        m_Realtime.didConnectToRoom += InitializePlayer;
        m_Realtime.didDisconnectFromRoom += RealtimeDidDisconnectFromRoom;
        m_ConnectionStatus.material.color = ROOM_NOT_CONNECTED;
    }

    //Realtime Event when Connecting to a Room
    private void InitializePlayer(Realtime realtime)
    {
        m_ConnectionStatus.material.color = ROOM_CONNECTING;
        int id = m_Realtime.clientID;

        if (!_spawnTransform)
        {
            Debug.Log("spawnTransform not set");
            Debug.Log("Setting spawnTransform.....");
            //if not then get from one of the default positions
            _spawnTransform = m_LocalPlayer.transform;//either its current position
            //R: if you put gameobjects with tag "spawn" in the scene with names 1, 2, ..., the players are going to be spawned at those specific positions
            foreach (GameObject g in GameObject.FindGameObjectsWithTag("spawn"))
            {
                if (g.name.Equals(id.ToString()))
                {
                    _spawnTransform = g.transform; break;
                }
            }
            Debug.Log(".....spawnTransform set");
        }

        base.InitializePlayer();

        // R: To show the other person where I am (this prefab is just to share my head and hands and render them for others) Check the prefab and see how to adapt to HL2
        GameObject newPlayer = Realtime.Instantiate(m_RealtimePlayerPrefab, _spawnTransform.position, _spawnTransform.rotation, new Realtime.InstantiateOptions
        {
            ownedByClient = true,
            preventOwnershipTakeover = true,
            destroyWhenOwnerLeaves = true,
            destroyWhenLastClientLeaves = true,
            useInstance = m_Realtime,
        });
        if (newPlayer.TryGetComponent<RealtimeView>(out RealtimeView r))
            r.RequestOwnershipOfSelfAndChildren();

        if (id == 0) AllRequestOwnerShip();
        m_ConnectionStatus.material.color = ROOM_CONNECTED;

        GameObject o = Realtime.Instantiate("OriginRT", Vector3.zero, Quaternion.identity, new Realtime.InstantiateOptions
        {
            ownedByClient = true,
            preventOwnershipTakeover = true,
            destroyWhenOwnerLeaves = true,
            destroyWhenLastClientLeaves = true,
            useInstance = m_Realtime,
        });
        o.GetComponentInChildren<TMP_Text>().text = realtime.room.clientID.ToString();
        PlayerAvatarSetUp();
    }

    private void RealtimeDidDisconnectFromRoom(Realtime realtime)
    {
        LobbyController lCont = FindObjectOfType<LobbyController>();

        //take us back the lobby when we disconnect
        if(lCont) lCont.LoadScene(0);
    }

    private void RequestOwnerShip(GameObject o)
    {
        if (o.TryGetComponent<RealtimeView>(out RealtimeView rtView))
        {
            Debug.Log(rtView.transform.name);
            rtView.RequestOwnershipOfSelfAndChildren();
        }

        /*        if (o.TryGetComponent<RealtimeTransform>(out RealtimeTransform rtTransform))
            rtTransform.RequestOwnership();*/

        /*for (int c = 0; c < o.transform.childCount; c++)
            RequestOwnerShip(o.transform.GetChild(c).gameObject);*/

        return;
    }

    private void AllRequestOwnerShip()
    {
        var rViews = FindObjectsOfType<RealtimeView>();//GetComponents<RealtimeView>();//FindObjectsByType<RealtimeView>(FindObjectsSortMode.InstanceID);
        //var rTransforms = FindObjectsOfType<RealtimeTransform>();//GetComponents<RealtimeTransform>();//FindObjectsByType<RealtimeTransform>(FindObjectsSortMode.InstanceID);

        foreach (RealtimeView v in rViews)
        {
            //Debug.Log(v.transform.name);
            if(!v.isOwnedRemotelySelf && !v.isOwnedLocallySelf)
                v.RequestOwnershipOfSelfAndChildren();
        }
/*        foreach (RealtimeTransform t in rTransforms)
        {
            t.RequestOwnership();
        }*/
    }
    public override void JoinRoom(Transform transform)
    {
        SetPlayerOffset(transform);
        m_Realtime.Connect(m_RoomName);
    }

    public override void JoinRoom(MyTransform transform)
    {
        SetPlayerOffset(transform);
        m_Realtime.Connect(m_RoomName);
    }

    public override void JoinRoom(Vector3 pos, Quaternion rot)
    {
        SetPlayerOffset(pos, rot);
        m_Realtime.Connect(m_RoomName);
    }

    public override void PlayerAvatarSetUp()
    {
        AvatarInfoPub aPub = FindAnyObjectByType<AvatarInfoPub>();

        if (aPub) aPub.SetPlayerID(m_Realtime.clientID);
        m_DataManager.CreatePlayerFile(m_Realtime.clientID);
    }

    private void OnDisable()
    {
        m_Realtime.didConnectToRoom -= InitializePlayer;

        m_Realtime.didDisconnectFromRoom -= RealtimeDidDisconnectFromRoom;
    }
}
