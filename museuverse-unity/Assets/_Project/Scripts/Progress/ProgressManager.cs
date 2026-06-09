using System;
using UnityEngine;
using UnityEngine.Events;
using MuseuVerse.Interaction;
using MuseuVerse.UI;

namespace MuseuVerse.Progress
{
    /// <summary>
    /// Acompanha o progresso da visita: descobre as pecas do acervo na cena,
    /// assina o evento de visita de cada uma, atualiza o contador do HUD e
    /// dispara a conclusao quando todas forem visitadas.
    /// Nao conhece a tela final diretamente: expoe a conclusao por evento C#
    /// (OnTodasVisitadas) e por UnityEvent (inspector), para a TelaFinal assinar.
    /// </summary>
    public class ProgressManager : MonoBehaviour
    {
        [Header("Referencias")]
        [SerializeField, Tooltip("HUD a atualizar. Se vazio, busca via FindObjectOfType no Awake.")]
        private HUDController hud;

        [Header("Conclusao")]
        [SerializeField, Tooltip("Disparado uma unica vez quando todas as pecas forem visitadas. A TelaFinal vai assinar aqui.")]
        private UnityEvent onTodasVisitadasInspector;

        private Artifact[] pecas;
        private bool concluido;

        /// <summary>Total de pecas do acervo encontradas na cena.</summary>
        public int Total => pecas != null ? pecas.Length : 0;

        /// <summary>Quantidade de pecas ja visitadas no momento.</summary>
        public int Visitadas => ContarVisitadas();

        /// <summary>True apos a conclusao ter sido disparada.</summary>
        public bool Concluido => concluido;

        /// <summary>
        /// Disparado uma unica vez quando todas as pecas forem visitadas.
        /// Equivalente em codigo ao UnityEvent do inspector.
        /// </summary>
        public event Action OnTodasVisitadas;

        private void Awake()
        {
            if (hud == null)
            {
                hud = FindObjectOfType<HUDController>();
                if (hud == null)
                {
                    Debug.LogError("[ProgressManager] HUDController nao encontrado na cena.", this);
                }
            }

            // Todas as pecas ja existem na cena no Awake; descoberta unica.
            pecas = FindObjectsOfType<Artifact>();
            if (pecas.Length == 0)
            {
                Debug.LogWarning("[ProgressManager] Nenhum Artifact encontrado na cena.", this);
            }
        }

        private void OnEnable()
        {
            if (pecas == null)
            {
                return;
            }
            foreach (Artifact peca in pecas)
            {
                if (peca != null)
                {
                    peca.OnArtifactVisited += AoVisitarPeca;
                }
            }
        }

        private void OnDisable()
        {
            if (pecas == null)
            {
                return;
            }
            foreach (Artifact peca in pecas)
            {
                if (peca != null)
                {
                    peca.OnArtifactVisited -= AoVisitarPeca;
                }
            }
        }

        private void Start()
        {
            // Estado inicial do contador (cobre o caso raro de algo ja visitado).
            AtualizarProgresso();
        }

        private void AoVisitarPeca(Artifact artifact)
        {
            AtualizarProgresso();
        }

        /// <summary>
        /// Recalcula o progresso a partir do estado real das pecas (idempotente,
        /// nao acumula contagem), atualiza o HUD e dispara a conclusao se for o caso.
        /// </summary>
        private void AtualizarProgresso()
        {
            int visitadas = ContarVisitadas();
            int total = Total;

            if (hud != null)
            {
                hud.AtualizarContador(visitadas, total);
            }

            if (!concluido && total > 0 && visitadas >= total)
            {
                concluido = true;
                DispararConclusao();
            }
        }

        private int ContarVisitadas()
        {
            if (pecas == null)
            {
                return 0;
            }

            int contador = 0;
            foreach (Artifact peca in pecas)
            {
                if (peca != null && peca.FoiVisitada)
                {
                    contador++;
                }
            }
            return contador;
        }

        private void DispararConclusao()
        {
            Debug.Log("[ProgressManager] Todas as pecas visitadas. Disparando conclusao.", this);
            onTodasVisitadasInspector?.Invoke();
            OnTodasVisitadas?.Invoke();
        }
    }
}
