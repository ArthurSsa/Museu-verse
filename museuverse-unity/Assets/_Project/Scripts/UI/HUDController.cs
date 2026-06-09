using TMPro;
using UnityEngine;
using MuseuVerse.Interaction;

namespace MuseuVerse.UI
{
    /// <summary>
    /// Controla a HUD persistente: crosshair, prompt de interacao e contador de pecas.
    /// Assina o InteractionRaycaster para mostrar/esconder o prompt conforme o alvo mirado.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        [Header("Referencias")]
        [SerializeField, Tooltip("Raycaster do Player. Se vazio, busca via FindObjectOfType no Awake.")]
        private InteractionRaycaster raycaster;

        [Header("Elementos da HUD")]
        [SerializeField, Tooltip("Ponto central da mira (permanece sempre visivel)")]
        private GameObject crosshair;

        [SerializeField, Tooltip("Grupo do prompt de interacao, ativado apenas quando mira uma peca")]
        private GameObject grupoPrompt;

        [SerializeField, Tooltip("Texto que mostra o nome da peca mirada")]
        private TMP_Text textoNomePeca;

        [SerializeField, Tooltip("Texto do contador de pecas visitadas")]
        private TMP_Text textoContador;

        [Header("Configuracao")]
        [SerializeField, Tooltip("Total de pecas do acervo")]
        private int totalPecas = 4;

        private void Awake()
        {
            if (raycaster == null)
            {
                raycaster = FindObjectOfType<InteractionRaycaster>();
                if (raycaster == null)
                {
                    Debug.LogError("[HUDController] InteractionRaycaster nao encontrado na cena.", this);
                }
            }
        }

        private void OnEnable()
        {
            if (raycaster != null)
            {
                raycaster.OnTargetChanged += AoMudarAlvo;
            }
        }

        private void OnDisable()
        {
            if (raycaster != null)
            {
                raycaster.OnTargetChanged -= AoMudarAlvo;
            }
        }

        private void Start()
        {
            if (grupoPrompt != null)
            {
                grupoPrompt.SetActive(false);
            }
            AtualizarContador(0, totalPecas);
        }

        private void AoMudarAlvo(Artifact alvo)
        {
            bool temAlvo = alvo != null;

            if (grupoPrompt != null)
            {
                grupoPrompt.SetActive(temAlvo);
            }

            if (temAlvo && textoNomePeca != null)
            {
                textoNomePeca.text = alvo.Title;
            }
        }

        /// <summary>
        /// Atualiza o texto do contador de pecas visitadas.
        /// Sera chamado pelo ProgressManager na Fase 6.
        /// </summary>
        /// <param name="visitadas">Quantidade de pecas ja visitadas.</param>
        /// <param name="total">Total de pecas do acervo.</param>
        public void AtualizarContador(int visitadas, int total)
        {
            if (textoContador != null)
            {
                textoContador.text = $"Peças visitadas: {visitadas}/{total}";
            }
        }
    }
}
