using TMPro;
using UnityEngine;
using UnityEngine.UI;
using MuseuVerse.Interaction;
using MuseuVerse.Player;
using MuseuVerse.Progress;

namespace MuseuVerse.UI
{
    /// <summary>
    /// Tela final (certificado de visita). Aberta pelo ProgressManager via UnityEvent
    /// quando todas as pecas sao visitadas — sem referencia de codigo entre os dois.
    /// Mostra o nome do visitante (GameState), a contagem de pecas e oferece o claim
    /// do NFT por uma URL configurada no inspector. Pausa player e raycaster enquanto
    /// aberta, igual aos demais modais.
    /// Roda depois do FirstPersonController (execution order) para que o ESC que fecha
    /// a tela nao seja reprocessado pelo controle do jogador no mesmo frame.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class TelaFinal : MonoBehaviour
    {
        [Header("Referencias de gameplay")]
        [SerializeField, Tooltip("Controle do Player. Se vazio, busca via FindObjectOfType.")]
        private FirstPersonController playerController;

        [SerializeField, Tooltip("Raycaster do Player. Se vazio, busca via FindObjectOfType.")]
        private InteractionRaycaster raycaster;

        [Header("Elementos da tela")]
        [SerializeField, Tooltip("Raiz visual da tela, ativada/desativada ao abrir/fechar")]
        private GameObject painelRaiz;

        [SerializeField, Tooltip("Texto que recebe o nome do visitante vindo do GameState")]
        private TMP_Text textoNome;

        [SerializeField, Tooltip("Texto da contagem de pecas, ex: '4/4 pecas visitadas'")]
        private TMP_Text textoContagem;

        [Header("Claim NFT")]
        [SerializeField, Tooltip("Botao que abre o link de claim do NFT")]
        private Button botaoClaim;

        [SerializeField, Tooltip("URL de claim do NFT. Preenchida quando o dev de blockchain entregar o link.")]
        private string urlClaim = string.Empty;

        [Header("Botoes")]
        [SerializeField] private Button botaoFechar;

        [Header("Configuracao")]
        [SerializeField, Tooltip("Total de pecas do acervo, usado no texto de contagem")]
        private int totalPecas = 4;

        private bool estaAberta;

        public bool EstaAberta => estaAberta;

        private void Awake()
        {
            if (playerController == null)
            {
                playerController = FindObjectOfType<FirstPersonController>();
            }
            if (raycaster == null)
            {
                raycaster = FindObjectOfType<InteractionRaycaster>();
            }
        }

        private void OnEnable()
        {
            if (botaoClaim != null)
            {
                botaoClaim.onClick.AddListener(AoClicarClaim);
            }
            if (botaoFechar != null)
            {
                botaoFechar.onClick.AddListener(Fechar);
            }
        }

        private void OnDisable()
        {
            if (botaoClaim != null)
            {
                botaoClaim.onClick.RemoveListener(AoClicarClaim);
            }
            if (botaoFechar != null)
            {
                botaoFechar.onClick.RemoveListener(Fechar);
            }
        }

        private void Start()
        {
            if (painelRaiz != null)
            {
                painelRaiz.SetActive(false);
            }
        }

        private void Update()
        {
            if (!estaAberta)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Fechar();
            }
        }

        /// <summary>
        /// Abre o certificado, preenche nome e contagem e pausa o gameplay.
        /// Metodo publico para o ProgressManager chamar via UnityEvent no inspector.
        /// </summary>
        public void Abrir()
        {
            estaAberta = true;

            if (painelRaiz != null)
            {
                painelRaiz.SetActive(true);
            }

            if (textoNome != null)
            {
                string nome = GameState.Instance.PlayerName;
                if (string.IsNullOrWhiteSpace(nome))
                {
                    nome = "Visitante";
                }
                textoNome.text = nome;
            }

            if (textoContagem != null)
            {
                textoContagem.text = $"{totalPecas}/{totalPecas} peças visitadas";
            }

            // SetInputEnabled(false) tambem libera o cursor para clicar na UI.
            if (playerController != null)
            {
                playerController.SetInputEnabled(false);
            }
            if (raycaster != null)
            {
                raycaster.SetInputEnabled(false);
            }
        }

        /// <summary>
        /// Fecha o certificado e devolve o controle ao jogador.
        /// </summary>
        public void Fechar()
        {
            estaAberta = false;

            if (painelRaiz != null)
            {
                painelRaiz.SetActive(false);
            }

            // SetInputEnabled(true) volta a travar o cursor para o modo de jogo.
            if (playerController != null)
            {
                playerController.SetInputEnabled(true);
            }
            if (raycaster != null)
            {
                raycaster.SetInputEnabled(true);
            }
        }

        private void AoClicarClaim()
        {
            if (string.IsNullOrWhiteSpace(urlClaim))
            {
                Debug.LogWarning("[TelaFinal] URL de claim do NFT ainda nao configurada no inspector.", this);
                return;
            }

            Application.OpenURL(urlClaim);
        }
    }
}
