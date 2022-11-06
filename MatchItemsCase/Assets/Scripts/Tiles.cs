using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Tiles : MonoBehaviour, IPointerDownHandler
{
    public event Action<Vector2Int> DidGetClicked;

    [SerializeField] private Rigidbody2D _Rigidbody;
    public Rigidbody2D Rigidbody => _Rigidbody;
    [SerializeField] private TilesColor _Color;
    public TilesColor Color => _Color;
    [SerializeField] private Image _DefaultIcon;
    [SerializeField] private Image _IconA;
    [SerializeField] private Image _IconB;
    [SerializeField] private Image _IconC;
    [SerializeField] private TileGroupType _matchType;
    public Vector2Int GridPos { get; set; }



    public void SetMatchGroupType(TileGroupType matchType)
    {
        if (_matchType == matchType) return;

        _matchType = matchType;

        _DefaultIcon.enabled = false;
        _IconA.enabled = false;
        _IconB.enabled = false;
        _IconC.enabled = false;

        switch (matchType)
        {
            case TileGroupType.Default:
                _DefaultIcon.enabled = true;
                break;

            case TileGroupType.A:
                _IconA.enabled = true;
                break;

            case TileGroupType.B:
                _IconB.enabled = true;
                break;

            case TileGroupType.C:
                _IconC.enabled = true;
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(matchType), matchType, null);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        DidGetClicked?.Invoke(GridPos);
    }
}


