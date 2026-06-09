using UnityEngine;

namespace MuseuVerse.Player
{
    /// <summary>
    /// Controle de personagem em primeira pessoa.
    /// Movimento WASD via CharacterController, mouse look com clamp vertical,
    /// corrida com LeftShift e travamento do cursor com ESC.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class FirstPersonController : MonoBehaviour
    {
        [Header("Referencias")]
        [SerializeField, Tooltip("Transform da camera filha que responde ao mouse look. Se vazio, busca a primeira Camera filha automaticamente.")]
        private Transform cameraTransform;

        [Header("Movimento")]
        [SerializeField, Tooltip("Velocidade ao caminhar (m/s)")]
        private float velocidadeCaminhada = 2.5f;

        [SerializeField, Tooltip("Velocidade ao correr com LeftShift (m/s)")]
        private float velocidadeCorrida = 4.5f;

        [SerializeField, Tooltip("Gravidade aplicada por segundo. Valor negativo.")]
        private float gravidade = -19.62f;

        [SerializeField, Tooltip("Altura aproximada do pulo em metros")]
        private float alturaDoPulo = 1.0f;

        [Header("Camera")]
        [SerializeField, Tooltip("Sensibilidade horizontal do mouse")]
        private float sensibilidadeMouseX = 1.5f;

        [SerializeField, Tooltip("Sensibilidade vertical do mouse")]
        private float sensibilidadeMouseY = 1.5f;

        [SerializeField, Range(0f, 90f), Tooltip("Limite de rotacao vertical da camera, em graus")]
        private float limiteRotacaoVertical = 85.0f;

        private CharacterController characterController;
        private float rotacaoVertical;
        private Vector3 velocidadeVertical;
        private bool inputHabilitado = true;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();

            if (cameraTransform == null)
            {
                var cameraFilha = GetComponentInChildren<Camera>();
                if (cameraFilha == null)
                {
                    Debug.LogError("[FirstPersonController] cameraTransform nao atribuido e nao foi encontrada Camera filha.", this);
                    enabled = false;
                    return;
                }
                cameraTransform = cameraFilha.transform;
            }
        }

        private void Start()
        {
            TravarCursor(true);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape) && inputHabilitado)
            {
                TravarCursor(Cursor.lockState != CursorLockMode.Locked);
            }

            if (!inputHabilitado)
            {
                return;
            }

            AtualizarRotacao();
            AtualizarMovimento();
        }

        private void AtualizarRotacao()
        {
            // sem mouse look enquanto o cursor estiver destravado
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                return;
            }

            float mouseX = Input.GetAxis("Mouse X") * sensibilidadeMouseX;
            float mouseY = Input.GetAxis("Mouse Y") * sensibilidadeMouseY;

            // yaw aplicado no jogador inteiro para que transform.forward/right giram junto
            transform.Rotate(Vector3.up * mouseX);

            // pitch aplicado apenas na camera filha, com clamp para nao virar de cabeca pra baixo
            rotacaoVertical -= mouseY;
            rotacaoVertical = Mathf.Clamp(rotacaoVertical, -limiteRotacaoVertical, limiteRotacaoVertical);
            cameraTransform.localRotation = Quaternion.Euler(rotacaoVertical, 0f, 0f);
        }

        private void AtualizarMovimento()
        {
            float inputHorizontal = Input.GetAxis("Horizontal");
            float inputVertical = Input.GetAxis("Vertical");

            Vector3 direcao = transform.right * inputHorizontal + transform.forward * inputVertical;

            bool correndo = Input.GetKey(KeyCode.LeftShift);
            float velocidadeAtual = correndo ? velocidadeCorrida : velocidadeCaminhada;

            Vector3 movimentoHorizontal = direcao * velocidadeAtual;

            // mantem o player colado no chao com leve forca pra baixo enquanto esta apoiado
            if (characterController.isGrounded && velocidadeVertical.y < 0f)
            {
                velocidadeVertical.y = -2f;
            }

            // pulo: so dispara se estiver no chao
            if (Input.GetKeyDown(KeyCode.Space) && characterController.isGrounded)
            {
                // formula cinematica: v = sqrt(2 * |g| * h) para atingir altura desejada
                velocidadeVertical.y = Mathf.Sqrt(alturaDoPulo * -2f * gravidade);
            }

            // gravidade integrada por frame
            velocidadeVertical.y += gravidade * Time.deltaTime;

            Vector3 movimentoTotal = movimentoHorizontal + velocidadeVertical;
            characterController.Move(movimentoTotal * Time.deltaTime);
        }

        private void TravarCursor(bool travar)
        {
            Cursor.lockState = travar ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !travar;
        }

        /// <summary>
        /// Habilita ou desabilita o input de movimento e camera.
        /// Chamado por paineis de UI que precisam pausar o jogador.
        /// </summary>
        /// <param name="habilitado">true para retomar controle e travar cursor, false para pausar e liberar cursor.</param>
        public void SetInputEnabled(bool habilitado)
        {
            inputHabilitado = habilitado;
            TravarCursor(habilitado);
        }
    }
}
