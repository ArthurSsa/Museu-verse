using TMPro;
using UnityEngine;
using UnityEngine.UI;
using MuseuVerse.Interaction;
using MuseuVerse.Player;
using MuseuVerse.Progress;

namespace MuseuVerse.UI
{
    /// <summary>
    /// Tela de boas-vindas exibida no inicio da experiencia, dentro da propria
    /// cena do museu (sem troca de cena). Captura o nome do visitante, salva no
    /// GameState para o certificado NFT, e so libera o controle do jogador apos
    /// a confirmacao.
    /// Roda depois do FirstPersonController (execution order) para que o
    /// SetInputEnabled(false) que libera o cursor aconteca DEPOIS do Start do
    /// player, que trava o cursor. Caso contrario o cursor ficaria preso.
    /// </summary>
    [DefaultExecutionOrder(50)]
    public class TelaBoasVindas : MonoBehaviour
    {
        [Header("Referencias de gameplay")]
        [SerializeField, Tooltip("Controle do Player. Se vazio, busca via FindObjectOfType.")]
        private FirstPersonController playerController;

        [SerializeField, Tooltip("Raycaster do Player. Se vazio, busca via FindObjectOfType.")]
        private InteractionRaycaster raycaster;

        [Header("Elementos da tela")]
        [SerializeField, Tooltip("Raiz visual da tela, ativada no Start e desativada ao iniciar a visita")]
        private GameObject painelRaiz;

        [SerializeField, Tooltip("Campo de texto onde o visitante digita o nome")]
        private TMP_InputField inputNome;

        [SerializeField, Tooltip("Botao de confirmacao. Fica desabilitado enquanto o nome estiver vazio.")]
        private Button botaoIniciar;

        private bool estaAberta;

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
            if (inputNome != null)
            {
                inputNome.onValueChanged.AddListener(AoMudarTexto);
            }
            if (botaoIniciar != null)
            {
                botaoIniciar.onClick.AddListener(IniciarVisita);
            }
        }

        private void OnDisable()
        {
            if (inputNome != null)
            {
                inputNome.onValueChanged.RemoveListener(AoMudarTexto);
            }
            if (botaoIniciar != null)
            {
                botaoIniciar.onClick.RemoveListener(IniciarVisita);
            }
        }

        private void Start()
        {
            Abrir();
        }

        private void Update()
        {
            if (!estaAberta)
            {
                return;
            }

            // Enter confirma, desde que o nome seja valido (botao habilitado).
            bool pressionouEnter = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
            if (pressionouEnter && botaoIniciar != null && botaoIniciar.interactable)
            {
                IniciarVisita();
            }
        }

        /// <summary>
        /// Mostra a tela, congela o jogador e o raycaster e foca o campo de nome.
        /// </summary>
        private void Abrir()
        {
            estaAberta = true;

            if (painelRaiz != null)
            {
                painelRaiz.SetActive(true);
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

            AtualizarBotao(inputNome != null ? inputNome.text : string.Empty);

            if (inputNome != null)
            {
                inputNome.ActivateInputField();
            }
        }

        /// <summary>
        /// Salva o nome no GameState, fecha a tela e devolve o controle ao jogador.
        /// Ignora a chamada se o nome estiver vazio ou so com espacos.
        /// </summary>
        private void IniciarVisita()
        {
            if (inputNome == null)
            {
                return;
            }

            string nome = inputNome.text.Trim();
            if (string.IsNullOrEmpty(nome))
            {
                return;
            }

            GameState.Instance.PlayerName = nome;

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

        private void AoMudarTexto(string texto)
        {
            AtualizarBotao(texto);
        }

        /// <summary>
        /// Habilita o botao apenas quando o nome tem algum caractere visivel.
        /// IsNullOrWhiteSpace cobre o caso de so espacos.
        /// </summary>
        private void AtualizarBotao(string texto)
        {
            if (botaoIniciar != null)
            {
                botaoIniciar.interactable = !string.IsNullOrWhiteSpace(texto);
            }
        }
    }
}
