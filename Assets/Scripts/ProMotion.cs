using UnityEngine;
using UnityEngine.InputSystem;

public class ProMotion : MonoBehaviour
{
    //Required Assets
    [Header("Required Assets")]
    [SerializeField] private Transform _cam;
    [SerializeField] private Rigidbody _body;
    // Parameters
    [Header("Parameters")]
    [SerializeField] private float _moveSpeed = 20;
    [SerializeField] private float _boostSpeed = 40;
    [SerializeField] private float _airSpeed = 10;
    [SerializeField] private float _jumpSpeed = 10;
    [SerializeField] private float _sensitivityX = 3f;
    [SerializeField] private float _sensitivityY = 3f;
    [SerializeField] private float _halfHeight = 1;
    [SerializeField] private float _halfWidth = 0.65f;
    [SerializeField] private float _rayLength = 0.1f;
    [SerializeField] private float _tiltAngle = 5;
    [SerializeField] private float _tiltSpeed = 5;
    [SerializeField] private float _rotationSpeed = 90.0f;
    [SerializeField] private float _boostTime = 0.25f;
    // Variables
    private Player—ontrol.ProMotionActions _actions;
    private bool _downTouch = false;
    private bool DownTouch
    {
        get
        {
            return _downTouch;
        }
        set
        {
            if (!value && _downTouch)
            {
                if (!_body.useGravity)
                {
                    _body.transform.rotation = _cam.rotation;
                    _cam.localRotation = Quaternion.Euler(0, 0, 0);
                }
            }
            _downTouch = value;
        }
    }
    private bool _rightTouch = false;
    private bool _leftTouch = false;
    private Vector2 _moveInput;
    private RaycastHit _downHit;
    private RaycastHit _sideHit;
    private float _neededTilt = 0;
    private Vector3 _moveVector;
    private float _curTilt = 0;
    private float _rotationX = 0;
    private float _rotationY = 0;
    private bool _rotateBodyOnGround = false;
    private float _rotateBodyOnGroundTimer = 0;
    private bool _boost = false;
    private float _boostTimer = 0;

    public void Initialize(Player—ontrol playerControl)
    {
        _actions = playerControl.ProMotion;
        _actions.Jump.started += Jump;
        _actions.SwitchGravity.started += SwitchGravity;
        _actions.StartBoost.started += StartBoost;
    }

    private void Update()
    {
        Move();
    }

    private void Move()
    {
        CheckTouches();
        MoveCam();
        MoveBody();
        RotateBodyOnGround();
    }
    private void MoveCam()
    {
        Vector2 rotateInput = _actions.Camera.ReadValue<Vector2>();
        float dTilt = (_neededTilt - _curTilt) * _tiltSpeed * Time.deltaTime;
        _curTilt = _curTilt + dTilt;
        float dX = -rotateInput.y * _sensitivityX;
        float dY = rotateInput.x * _sensitivityY;
        _rotationY += dY;
        if (_body.useGravity)
        {
            _rotationX = Mathf.Clamp(_rotationX + dX, -90, 90);
            _body.transform.Rotate(0, dY, 0);
            _cam.localRotation = Quaternion.Euler(_rotationX, 0, _curTilt);
        }
        else
        {
            if (DownTouch)
            {
                Quaternion savedRotation = _cam.rotation;
                _body.transform.rotation = Quaternion.LookRotation(Vector3.Cross(_body.transform.right, _downHit.normal), _downHit.normal);
                _cam.rotation = savedRotation;

                _rotationX = FormatRotationAxis(_cam.localRotation.eulerAngles.x);
                _rotationX = Mathf.Clamp(_rotationX + dX, -90, 90);
                _body.transform.Rotate(0, dY, 0);
                float curZRotation = FormatRotationAxis(_cam.localRotation.eulerAngles.z);
                float curYRotation = FormatRotationAxis(_cam.localRotation.eulerAngles.y);
                _cam.localRotation = Quaternion.Euler(
                    _rotationX,
                    Lerp(curYRotation, 0, _tiltSpeed * Time.deltaTime),
                    Lerp(curZRotation, _curTilt, _tiltSpeed * Time.deltaTime));
            }
            else
            {
                float dZ = _actions.ZRotation.ReadValue<float>() * _rotationSpeed * Time.deltaTime;
                _body.transform.Rotate(dX, dY, dZ);
                _cam.localRotation = Quaternion.Euler(0, 0, _curTilt);
            }
        }
    }
    private float FormatRotationAxis(float axis)
    {
        return axis > 260 ? axis - 360 : axis;
    }
    private void MoveBody()
    {
        _moveInput = _actions.Movement.ReadValue<Vector2>();
        if (_rightTouch || _leftTouch) WallRun();
        else StandardMotion();
        Boost();
    }
    private void CheckTouches()
    {
        DownTouch = Physics.Raycast(_body.transform.position - _body.transform.up * _halfHeight / 2, -_body.transform.up, out _downHit, _rayLength + _halfHeight / 2);
        if (!DownTouch)
        {
            DownTouch = Physics.Raycast(_body.transform.position - _body.transform.up * _halfHeight / 2, -_body.transform.up + _body.transform.forward, out _downHit, _rayLength + _halfHeight / 2);
            if (!DownTouch)
            {
                DownTouch = Physics.Raycast(_body.transform.position - _body.transform.up * _halfHeight / 2, -_body.transform.up + _body.transform.right, out _downHit, _rayLength + _halfHeight / 2);
                if (!DownTouch)
                {
                    DownTouch = Physics.Raycast(_body.transform.position - _body.transform.up * _halfHeight / 2, -_body.transform.up - _body.transform.forward, out _downHit, _rayLength + _halfHeight / 2);
                    if (!DownTouch)
                    {
                        DownTouch = Physics.Raycast(_body.transform.position - _body.transform.up * _halfHeight / 2, -_body.transform.up - _body.transform.right, out _downHit, _rayLength + _halfHeight / 2);
                    }
                }
            }
        }

        _rightTouch = Physics.Raycast(_body.transform.position, _body.transform.right + _body.transform.forward, out _sideHit, _rayLength + _halfWidth);
        if (!_rightTouch)
        {
            _rightTouch = Physics.Raycast(_body.transform.position, _body.transform.right, out _sideHit, _rayLength + _halfWidth);
            if (!_rightTouch)
            {
                _rightTouch = Physics.Raycast(_body.transform.position, _body.transform.right - _body.transform.forward, out _sideHit, _rayLength + _halfWidth);
                if (!_rightTouch)
                {
                    _leftTouch = Physics.Raycast(_body.transform.position, -_body.transform.right + _body.transform.forward, out _sideHit, _rayLength + _halfWidth);
                    if (!_leftTouch)
                    {
                        _leftTouch = Physics.Raycast(_body.transform.position, -_body.transform.right, out _sideHit, _rayLength + _halfWidth);
                        if (!_leftTouch)
                        {
                            _leftTouch = Physics.Raycast(_body.transform.position, -_body.transform.right - _body.transform.forward, out _sideHit, _rayLength + _halfWidth);
                        }
                    }
                }
            }
        }
        if (_rightTouch) _leftTouch = false;
    }
    private void WallRun()
    {
        _neededTilt = (_rightTouch ? 1 : -1) * _tiltAngle;
        _moveVector = Mathf.Sign(_neededTilt) * Vector3.Cross(_body.transform.up, _sideHit.normal);
        Vector3 normalVelocityComponent = Vector3.Dot(_sideHit.normal, _body.velocity) * _sideHit.normal;
        normalVelocityComponent += Mathf.Max(Vector3.Dot(_sideHit.normal, _body.transform.right * _moveInput.x), 0) * _sideHit.normal;
        _body.velocity = _moveVector * Mathf.Max(_moveInput.y, 0) * _moveSpeed + normalVelocityComponent;
    }
    private void StandardMotion()
    {
        _neededTilt = -_tiltAngle * _moveInput.x;
        _moveVector = _moveInput.x * _body.transform.right + _moveInput.y * _body.transform.forward;
        if (DownTouch)
        {
            Vector3 upVelocityComponent = Vector3.Dot(_body.transform.up, _body.velocity) * _body.transform.up;
            _body.velocity = _moveVector * _moveSpeed + upVelocityComponent;
        }
        else _body.velocity += _moveVector * _airSpeed * Time.deltaTime;
    }
    private float Lerp(float a, float b, float dt)
    {
        return a + (b - a) * dt;
    }
    private void RotateBodyOnGround()
    {
        if (_rotateBodyOnGround)
        {
            if (_rotateBodyOnGroundTimer < 1f)
            {
                _body.transform.rotation = Quaternion.Slerp(_body.transform.rotation, Quaternion.Euler(0, _rotationY, 0), _rotateBodyOnGroundTimer);
                _rotateBodyOnGroundTimer += Time.deltaTime;
            }
            else
            {
                _rotateBodyOnGround = false;
                _rotateBodyOnGroundTimer = 0;
            }
        }
    }

    private void Jump(InputAction.CallbackContext context)
    {
        if (_rightTouch || _leftTouch)
        {
            _body.velocity += (_sideHit.normal + _body.transform.up) * _jumpSpeed;
        }
        else
        {
            if (DownTouch) _body.velocity = _body.transform.up * _jumpSpeed;
        }
    }

    private void SwitchGravity(InputAction.CallbackContext context)
    {
        _body.useGravity = !_body.useGravity;
        if (_body.useGravity)
        {
            _rotateBodyOnGround = true;
        }
    }

    private void StartBoost(InputAction.CallbackContext context)
    {
        _boost = true;
    }
    private void Boost()
    {
        if (_boost)
        {
            _moveVector = _moveVector == Vector3.zero ? _body.transform.forward : _moveVector.normalized;
            if (_boostTimer < _boostTime)
            {
                _body.velocity = _moveVector * _boostSpeed;
                _boostTimer += Time.deltaTime;
            }
            else
            {
                _body.velocity = _moveVector * _airSpeed;
                _boost = false;
                _boostTimer = 0;
            }
        }
    }
}
