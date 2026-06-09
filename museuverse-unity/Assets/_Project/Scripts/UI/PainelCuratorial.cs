using TMPro;
using UnityEngine;
using UnityEngine.UI;
using MuseuVerse.Interaction;
using MuseuVerse.Player;

namespace MuseuVerse.UI
{
    /// <summary>
    /// Painel curatorial modal. Exibe titulo, descricao longa e controle da narracao
    /// da peca selecionada. Pausa o jogador e o raycaster enquanto aberto e marca a
    /// peca como visitada ao fechar.
    /// Roda depois do FirstPersonController (execution order) para que o ESC que fecha
    /// o painel nao seja reprocessado pelo controle do jogador no mesmo frame.
    /// </summary>
    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(AudioSource))]
    public class PainelCuratorial : MonoBehaviour
    {
        [Header("Referencias de gameplay")]
        [SerializeField, Tooltip("Raycaster do Player. Se vazio, busca via FindObjectOfType.")]
        private InteractionRaycaster raycaster;

        [SerializeField, Tooltip("Controle do Player. Se vazio, busca via FindObjectOfType.")]
        private FirstPersonController playerController;

        [Header("Elementos do painel")]
        [SerializeField, Tooltip("Raiz visual do painel, ativada/desativada ao abrir/fechar")]
        private GameObject painelRaiz;

        [SerializeField] private TMP_Text textoTitulo;
        [SerializeField] private TMP_Text textoDescricao;

        [Header("Controle de audio")]
        [SerializeField] private Button botaoPlayPause;
        [SerializeField, Tooltip("Texto filho do botao play/pause")]
        private TMP_Text textoBotaoPlayPause;

        [SerializeField, Tooltip("Image com Image Type = Filled / Fill Method = Horizontal")]
        private Image barraProgresso;

        [Header("Botoes")]
        [SerializeField] private Button botaoFechar;

        private AudioSource audioSource;
        private Artifact artifactAtual;
        private bool estaAberto;

        public bool EstaAberto => estaAberto;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;

            if (raycaster == null)
            {
                raycaster = FindObjectOfType<InteractionRaycaster>();
            }
            if (playerController == null)
            {
                playerController = FindObjectOfType<FirstPersonController>();
            }
        }

        private void OnEnable()
        {
            if (raycaster != null)
            {
                raycaster.OnInteract += Abrir;
            }
            if (botaoFechar != null)
            {
                botaoFechar.onClick.AddListener(Fechar);
            }
            if (botaoPlayPause != null)
            {
                botaoPlayPause.onClick.AddListener(AlternarPlayPause);
            }
        }

        private void OnDisable()
        {
            if (raycaster != null)
            {
                raycaster.OnInteract -= Abrir;
            }
            if (botaoFechar != null)
            {
                botaoFechar.onClick.RemoveListener(Fechar);
            }
            if (botaoPlayPause != null)
            {
                botaoPlayPause.onClick.RemoveListener(AlternarPlayPause);
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
            if (!estaAberto)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Fechar();
                return;
            }

            AtualizarBarraProgresso();
            AtualizarTextoBotao();
        }

        /// <summary>
        /// Abre o painel preenchido com os dados da peca e inicia a narracao.
        /// </summary>
        public void Abrir(Artifact artifact)
        {
            if (artifact == null)
            {
                return;
            }

            artifactAtual = artifact;
            estaAberto = true;

            if (painelRaiz != null)
            {
                painelRaiz.SetActive(true);
            }
            if (textoTitulo != null)
            {
                textoTitulo.text = artifact.Title;
            }
            if (textoDescricao != null)
            {
                textoDescricao.text = artifact.LongDescription;
            }

            // pausa o gameplay e libera o cursor (SetInputEnabled cuida do cursor)
            if (playerController != null)
            {
                playerController.SetInputEnabled(false);
            }
            if (raycaster != null)
            {
                raycaster.SetInputEnabled(false);
            }

            if (artifact.Narration != null)
            {
                audioSource.clip = artifact.Narration;
                audioSource.time = 0f;
                audioSource.Play();
            }
            else
            {
                audioSource.clip = null;
            }

            AtualizarTextoBotao();
            AtualizarBarraProgresso();
        }

        /// <summary>
        /// Fecha o painel, para a narracao, retoma o gameplay e marca a peca como visitada.
        /// </summary>
        public void Fechar()
        {
            estaAberto = false;
            audioSource.Stop();

            if (painelRaiz != null)
            {
                painelRaiz.SetActive(false);
            }

            if (playerController != null)
            {
                playerController.SetInputEnabled(true);
            }
            if (raycaster != null)
            {
                raycaster.SetInputEnabled(true);
            }

            if (artifactAtual != null)
            {
                artifactAtual.MarcarComoVisitada();
            }
            artifactAtual = null;
        }

        private void AlternarPlayPause()
        {
            if (audioSource.clip == null)
            {
                return;
            }

            if (audioSource.isPlaying)
            {
                audioSource.Pause();
            }
            else
            {
                // Play retoma a partir de audioSource.time (resume) ou do inicio se terminou
                audioSource.Play();
            }

            AtualizarTextoBotao();
        }

        private void AtualizarTextoBotao()
        {
            if (textoBotaoPlayPause == null)
            {
                return;
            }

            bool tocando = audioSource.clip != null && audioSource.isPlaying;
            textoBotaoPlayPause.text = tocando ? "Pausar" : "Tocar";
        }

        private void AtualizarBarraProgresso()
        {
            if (barraProgresso == null)
            {
                return;
            }

            if (audioSource.clip != null && audioSource.clip.length > 0f)
            {
                barraProgresso.fillAmount = Mathf.Clamp01(audioSource.time / audioSource.clip.length);
            }
            else
            {
                barraProgresso.fillAmount = 0f;
            }
        }
    }
}
