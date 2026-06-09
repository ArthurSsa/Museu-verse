using System;
using TMPro;
using UnityEngine;
using MuseuVerse.Interaction;

namespace MuseuVerse.Progress
{
    /// <summary>
    /// Iluminacao guiada do museu: a sala comeca escura e um spot acende sobre a
    /// primeira peca a visitar. Ao fechar o painel da peca iluminada (evento
    /// OnArtifactVisited), acende o spot da proxima etapa SEM apagar os anteriores
    /// (os spots acumulam e a sala vai clareando). Apos a ultima, mantem todos os
    /// spots acesos e acende a luz geral da sala.
    /// Roda em paralelo ao ProgressManager/TelaFinal, sem depender deles.
    /// </summary>
    public class SequenciaLuz : MonoBehaviour
    {
        /// <summary>Uma etapa da sequencia: a peca e o spot que a ilumina.</summary>
        [Serializable]
        public class EtapaLuz
        {
            [Tooltip("Artifact da peca desta etapa")]
            public Artifact peca;

            [Tooltip("Spot Light que ilumina esta peca")]
            public Light spot;

            [TextArea, Tooltip("Objetivo exibido na HUD quando esta etapa fica ativa")]
            public string instrucao;
        }

        [Header("Sequencia (ordenada)")]
        [SerializeField, Tooltip("Etapas na ordem em que devem acender. Ordem esperada: Sha-Amun, Adandozan, Bendego, Luzia.")]
        private EtapaLuz[] etapas;

        [Header("Luz final")]
        [SerializeField, Tooltip("Light que ilumina a sala inteira, acesa quando a ultima peca for visitada")]
        private Light luzSala;

        [Header("Objetivo (HUD)")]
        [SerializeField, Tooltip("TMP_Text no topo da HUD que mostra o objetivo atual")]
        private TMP_Text textoObjetivo;

        [SerializeField, TextArea, Tooltip("Mensagem exibida no objetivo quando todas as pecas forem visitadas")]
        private string mensagemConclusao;

        private int indiceAtual;

        private void OnEnable()
        {
            if (etapas == null)
            {
                return;
            }
            foreach (EtapaLuz etapa in etapas)
            {
                if (etapa != null && etapa.peca != null)
                {
                    etapa.peca.OnArtifactVisited += AoVisitarPeca;
                }
            }
        }

        private void OnDisable()
        {
            if (etapas == null)
            {
                return;
            }
            foreach (EtapaLuz etapa in etapas)
            {
                if (etapa != null && etapa.peca != null)
                {
                    etapa.peca.OnArtifactVisited -= AoVisitarPeca;
                }
            }
        }

        private void Start()
        {
            // Estado inicial: tudo apagado.
            if (etapas != null)
            {
                foreach (EtapaLuz etapa in etapas)
                {
                    if (etapa != null && etapa.spot != null)
                    {
                        etapa.spot.enabled = false;
                    }
                }
            }
            if (luzSala != null)
            {
                luzSala.enabled = false;
            }

            indiceAtual = 0;
            AtivarEtapaAtual();
        }

        private void AoVisitarPeca(Artifact artifact)
        {
            if (etapas == null || indiceAtual >= etapas.Length)
            {
                return; // sequencia ja concluida
            }

            EtapaLuz etapaAtual = etapas[indiceAtual];

            // So avanca pela peca da etapa atual; visitas fora de ordem sao ignoradas
            // aqui (e tratadas pelo skip em AtivarEtapaAtual quando a sequencia chegar nelas).
            if (etapaAtual == null || etapaAtual.peca != artifact)
            {
                return;
            }

            // o spot da etapa visitada PERMANECE aceso (os spots acumulam);
            // apenas avancamos para acender o proximo.
            indiceAtual++;
            AtivarEtapaAtual();
        }

        /// <summary>
        /// Acende o spot da etapa atual. Pula etapas cuja peca ja foi visitada
        /// (visita fora de ordem) para a sequencia nunca emperrar. Se nao houver
        /// mais etapas, acende a luz da sala.
        /// </summary>
        private void AtivarEtapaAtual()
        {
            // pula etapas invalidas ou ja visitadas
            while (indiceAtual < etapas.Length && EtapaJaConcluida(etapas[indiceAtual]))
            {
                EtapaLuz pulada = etapas[indiceAtual];
                if (pulada != null && pulada.spot != null)
                {
                    pulada.spot.enabled = true; // ja visitada: mantem acesa (spots acumulam)
                }
                indiceAtual++;
            }

            if (indiceAtual >= etapas.Length)
            {
                // todas concluidas: luz geral da sala e mensagem de conclusao
                if (luzSala != null)
                {
                    luzSala.enabled = true;
                }
                DefinirObjetivo(mensagemConclusao);
                return;
            }

            EtapaLuz proxima = etapas[indiceAtual];
            if (proxima != null)
            {
                if (proxima.spot != null)
                {
                    proxima.spot.enabled = true;
                }
                DefinirObjetivo(proxima.instrucao);
            }
        }

        private void DefinirObjetivo(string texto)
        {
            if (textoObjetivo != null)
            {
                textoObjetivo.text = texto;
            }
        }

        /// <summary>Etapa nula, sem peca, ou cuja peca ja foi visitada.</summary>
        private bool EtapaJaConcluida(EtapaLuz etapa)
        {
            return etapa == null || etapa.peca == null || etapa.peca.FoiVisitada;
        }
    }
}
