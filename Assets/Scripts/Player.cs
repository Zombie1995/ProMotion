using UnityEngine;

public class Player : MonoBehaviour
{
    // Required Assets
    [Header("Required Assets")]
    [SerializeField] private ProMotion _proMotion;
    // Variables
    private Player—ontrol _playerControl;

    private void Awake()
    {
        InitializeControl();
        EnableControl();
    }

    private void InitializeControl()
    {
        Cursor.visible = false;
        _playerControl = new();
        _proMotion.Initialize(_playerControl);
    }

    private void EnableControl()
    {
        _proMotion.enabled = true;
        _playerControl.ProMotion.Enable();
    }
}
