using System;
using UnityEngine;

namespace MuseuVerse.Interaction
{
    /// <summary>
    /// Lanca raycast da camera a cada frame em busca de Artifacts proximos.
    /// Dispara eventos quando o alvo do raycast muda e quando o jogador
    /// pressiona a tecla de interacao mirando uma peca valida.
    /// </summary>
    public class InteractionRaycaster : MonoBehaviour
    {
        [Header("Referencias")]
        [SerializeField, Tooltip("Camera de origem do raycast. Se vazio, usa Camera.main no Awake.")]
        private Camera cameraReferencia;

        [Header("Configuracao")]
        [SerializeField, Tooltip("Distancia maxima do raycast em metros")]
        private float distanciaMaxima = 3f;

        [SerializeField, Tooltip("Mascara de layers consideradas pelo raycast. Inclua paredes para que elas bloqueiem o alcance.")]
        private LayerMask layerMaskRaycast = ~0;

        [SerializeField, Tooltip("Tecla de interacao")]
        private KeyCode teclaInteracao = KeyCode.E;

        [Header("Debug")]
        [SerializeField, Tooltip("Imprime no console quando o alvo muda e quando E eh pressionado. Util na Fase 4, desligar quando a UI estiver pronta.")]
        private bool logDebug = true;

        private Artifact alvoAtual;
        private bool inputHabilitado = true;

        /// <summary>Alvo atualmente mirado pelo raycast, ou null se nada.</summary>
        public Artifact AlvoAtual => alvoAtual;

        /// <summary>
        /// Disparado quando o alvo do raycast muda. Recebe o novo alvo ou null
        /// quando nada esta sendo mirado. HUD vai assinar para mostrar/esconder
        /// o prompt "Pressione E para interagir" (Fase 5).
        /// </summary>
        public event Action<Artifact> OnTargetChanged;

        /// <summary>
        /// Disparado quando o jogador pressiona a tecla de interacao mirando
        /// um Artifact valido. O painel curatorial (Fase 5) vai assinar.
        /// </summary>
        public event Action<Artifact> OnInteract;

        private void Awake()
        {
            if (cameraReferencia == null)
            {
                cameraReferencia = Camera.main;
                if (cameraReferencia == null)
                {
                    Debug.LogError("[InteractionRaycaster] cameraReferencia nao atribuida e Camera.main nao encontrada.", this);
                    enabled = false;
                }
            }
        }

        private void Update()
        {
            if (!inputHabilitado)
            {
                if (alvoAtual != null)
                {
                    DefinirAlvo(null);
                }
                return;
            }

            AtualizarAlvo();

            if (alvoAtual != null && Input.GetKeyDown(teclaInteracao))
            {
                if (logDebug)
                {
                    Debug.Log($"[InteractionRaycaster] Interagindo com: {alvoAtual.Title} (id={alvoAtual.ArtifactId})");
                }
                OnInteract?.Invoke(alvoAtual);
            }
        }

        private void AtualizarAlvo()
        {
            Ray raio = new Ray(cameraReferencia.transform.position, cameraReferencia.transform.forward);
            Artifact alvoDetectado = null;

            if (Physics.Raycast(raio, out RaycastHit hit, distanciaMaxima, layerMaskRaycast))
            {
                // GetComponentInParent permite que o collider esteja em filho do GameObject do Artifact
                alvoDetectado = hit.collider.GetComponentInParent<Artifact>();
            }

            if (alvoDetectado != alvoAtual)
            {
                DefinirAlvo(alvoDetectado);
            }
        }

        private void DefinirAlvo(Artifact novoAlvo)
        {
            alvoAtual = novoAlvo;

            if (logDebug)
            {
                if (novoAlvo != null)
                {
                    Debug.Log($"[InteractionRaycaster] Mirando em: {novoAlvo.Title}");
                }
                else
                {
                    Debug.Log("[InteractionRaycaster] Mirando em: nada");
                }
            }

            OnTargetChanged?.Invoke(novoAlvo);
        }

        /// <summary>
        /// Habilita ou desabilita o raycast e a tecla de interacao.
        /// Chamado por paineis de UI que precisam pausar a interacao.
        /// Quando desabilitado, limpa o alvo atual.
        /// </summary>
        /// <param name="habilitado">true para retomar, false para pausar.</param>
        public void SetInputEnabled(bool habilitado)
        {
            inputHabilitado = habilitado;
        }
    }
}
