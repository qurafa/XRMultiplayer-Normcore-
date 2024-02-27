using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class CanvasControl : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private CallibrateRoom _room;
    [SerializeField] private CallibrateRoom.Mode defaultRoomMode;
    [SerializeField] private CallibrateRoom.Mode pointerRoomMode;
    

    public void OnPointerEnter(PointerEventData eventData)
    {
        _room.SetMode(pointerRoomMode);//set to standby mode
    }

    void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
    {
        _room.SetMode(defaultRoomMode);//set to default mode
    }
}
