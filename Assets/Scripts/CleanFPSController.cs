using UnityEngine;

/// <summary>
/// Controlador de movimiento en primera persona para Player utilizando CharacterController e Input clásico.
/// Implementa movimiento WASD, rotación con ratón y gravedad estándar.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class CleanFPSController : MonoBehaviour
{
    [Header("Configuración de Movimiento")]
    [Tooltip("Velocidad de traslación del jugador (unidades/segundo).")]
    [SerializeField] private float moveSpeed = 5f;

    [Tooltip("Aceleración de gravedad hacia abajo.")]
    [SerializeField] private float gravity = -9.81f;

    [Header("Configuración de Mirada (Ratón)")]
    [Tooltip("Sensibilidad de rotación del ratón.")]
    [SerializeField] private float mouseSensitivity = 2f;

    [Tooltip("Límite inferior de inclinación de la cámara (grados).")]
    [SerializeField] private float minPitch = -85f;

    [Tooltip("Límite superior de inclinación de la cámara (grados).")]
    [SerializeField] private float maxPitch = 85f;

    [Header("Referencias")]
    [Tooltip("Transform de la cámara en primera persona (hijo de Player).")]
    [SerializeField] private Transform playerCamera;

    private CharacterController characterController;
    private float verticalVelocity = 0f;
    private float cameraPitch = 0f;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

        // Si la cámara no está asignada en el Inspector, buscarla en los hijos o usar Camera.main
        if (playerCamera == null)
        {
            Camera cam = GetComponentInChildren<Camera>();
            if (cam != null)
            {
                playerCamera = cam.transform;
            }
            else if (Camera.main != null)
            {
                playerCamera = Camera.main.transform;
            }
        }
    }

    private void Start()
    {
        // Bloqueo y ocultamiento del cursor al iniciar
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Inicializar el ángulo de pitch según la rotación local actual de la cámara
        if (playerCamera != null)
        {
            Vector3 camEuler = playerCamera.localEulerAngles;
            cameraPitch = camEuler.x > 180f ? camEuler.x - 360f : camEuler.x;
        }
    }

    private void Update()
    {
        HandleMouseLook();
        HandleMovement();
        HandleCursorToggle();
    }

    /// <summary>
    /// Gestiona la rotación horizontal (yaw) del cuerpo y vertical (pitch) de la cámara.
    /// </summary>
    private void HandleMouseLook()
    {
        if (Cursor.lockState != CursorLockMode.Locked) return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Rotación horizontal del jugador sobre el eje Y
        transform.Rotate(Vector3.up * mouseX);

        // Rotación vertical de la cámara (pitch) limitada
        if (playerCamera != null)
        {
            cameraPitch -= mouseY;
            cameraPitch = Mathf.Clamp(cameraPitch, minPitch, maxPitch);
            playerCamera.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
        }
    }

    /// <summary>
    /// Gestiona el desplazamiento WASD y la aplicación de gravedad para apoyar al jugador sobre la superficie.
    /// </summary>
    private void HandleMovement()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        // Dirección en espacio local del jugador (adelante/atrás y lados)
        Vector3 moveDirection = (transform.right * horizontal + transform.forward * vertical);
        if (moveDirection.sqrMagnitude > 1f)
        {
            moveDirection.Normalize();
        }

        Vector3 velocity = moveDirection * moveSpeed;

        // Gravedad estándar y detección de suelo
        if (characterController.isGrounded && verticalVelocity < 0f)
        {
            // Pequeña fuerza hacia abajo para mantener el contacto firme con el suelo
            verticalVelocity = -2f;
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }

        velocity.y = verticalVelocity;

        // Desplazar mediante el CharacterController
        characterController.Move(velocity * Time.deltaTime);
    }

    /// <summary>
    /// Permite liberar el cursor con la tecla Escape y volver a bloquearlo haciendo clic en la ventana.
    /// </summary>
    private void HandleCursorToggle()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
