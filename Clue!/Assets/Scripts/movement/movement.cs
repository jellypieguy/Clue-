using UnityEngine;
using UnityEngine.InputSystem;

// why am i having to do lowercase class?? who tf keeps doing lowercase stuff in unity??????
public class movement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 20f;
    [SerializeField] private float smoothTime = 0.2f;
    [SerializeField] private float dragSensitivity = 1.0f;

    [Header("Boundaries")]
    [SerializeField] private Vector2 minBounds = new(-20f, -20f);
    [SerializeField] private Vector2 maxBounds = new(20f, 20f);

    private Camera cam;
    private Vector3 targetPosition;
    private Vector3 currentVelocity = Vector3.zero;
    private bool isInitialized = false;

    private void Awake() => cam = GetComponent<Camera>();

    private void Start() => targetPosition = transform.position;

    private void Update()
    {
        if (!isInitialized)
        {
            targetPosition = transform.position;
            isInitialized = true;
        }

        HandleKeyboardInput();
        HandleMouseDrag();

        targetPosition.x = Mathf.Clamp(targetPosition.x, minBounds.x, maxBounds.x);
        targetPosition.y = Mathf.Clamp(targetPosition.y, minBounds.y, maxBounds.y);

        transform.position = Vector3.SmoothDamp(
            transform.position, 
            new Vector3(targetPosition.x, targetPosition.y, transform.position.z), 
            ref currentVelocity, 
            smoothTime
        );
    }

    private void HandleKeyboardInput()
    {
        if (Keyboard.current == null) return;

        var input = Vector2.zero;
        var keys = Keyboard.current;

        // WASD + arrow keys 
        if (keys.wKey.isPressed || keys.upArrowKey.isPressed) input.y += 1;
        if (keys.sKey.isPressed || keys.downArrowKey.isPressed) input.y -= 1;
        if (keys.aKey.isPressed || keys.leftArrowKey.isPressed) input.x -= 1;
        if (keys.dKey.isPressed || keys.rightArrowKey.isPressed) input.x += 1;

        if (input != Vector2.zero)
            targetPosition += moveSpeed * Time.deltaTime * new Vector3(input.x, input.y, 0).normalized;
    }

    private void HandleMouseDrag()
    {
        if (Mouse.current == null || cam == null) return;

        // r_mouseclick drag to look 
        if (Mouse.current.rightButton.isPressed)
        {
            var dragDelta = Mouse.current.delta.ReadValue();
            if (dragDelta != Vector2.zero)
            {
                float screenFactor = cam.orthographicSize * 2f / Screen.height;
                targetPosition -= new Vector3(dragDelta.x, dragDelta.y, 0) * screenFactor * dragSensitivity;
            }
        }
    }
}