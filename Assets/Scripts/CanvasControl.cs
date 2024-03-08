using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class CanvasControl : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private TextMeshProUGUI m_Range;
    [Header("UI INTERACTABILITY")]
    [SerializeField] private CallibrateRoom _room;
    [SerializeField] private CallibrateRoom.Mode defaultRoomMode;
    [SerializeField] private CallibrateRoom.Mode pointerRoomMode;

    private float _range = 0;

    private void Start()
    {
        UpdateRange();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _room.SetMode(pointerRoomMode);//set to standby mode
    }

    void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
    {
        _room.SetMode(defaultRoomMode);//set to default mode
    }

    public void OnIncreaseRange()
    {
        _range += 0.5f;
        UpdateRange();
    }

    public void OnDecreaseRange()
    {
        _range = (_range == 0) ? 0 : (_range - 0.5f);
        UpdateRange();
    }

    private void UpdateRange()
    {
        m_Range.text = _range.ToString();
    }
}
