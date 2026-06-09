using System;
using UnityEngine;

namespace MuseuVerse.Interaction
{
    /// <summary>
    /// Representa uma peca interagivel do acervo. Carrega os metadados que o painel
    /// curatorial vai exibir (titulo, descricoes, narracao) e dispara evento ao ser
    /// visitada pela primeira vez.
    /// </summary>
    public class Artifact : MonoBehaviour
    {
        [Header("Identificacao")]
        [SerializeField, Tooltip("Identificador unico da peca em snake/kebab case. Ex: luzia, bendego, adandozan, shaamun.")]
        private string artifactId;

        [SerializeField, Tooltip("Titulo exibido no HUD e no painel curatorial")]
        private string title;

        [Header("Descricoes")]
        [SerializeField, Tooltip("Descricao curta exibida no HUD ao mirar a peca")]
        private string shortDescription;

        [SerializeField, TextArea(4, 12), Tooltip("Descricao longa exibida no painel curatorial")]
        private string longDescription;

        [Header("Midia")]
        [SerializeField, Tooltip("Narracao em PT-BR de 30 a 45 segundos")]
        private AudioClip narration;

        [SerializeField, Tooltip("Thumbnail opcional usada na UI")]
        private Sprite thumbnail;

        private bool foiVisitada;

        public string ArtifactId => artifactId;
        public string Title => title;
        public string ShortDescription => shortDescription;
        public string LongDescription => longDescription;
        public AudioClip Narration => narration;
        public Sprite Thumbnail => thumbnail;
        public bool FoiVisitada => foiVisitada;

        /// <summary>
        /// Disparado uma unica vez quando a peca eh marcada como visitada.
        /// O ProgressManager (Fase 6) vai assinar este evento.
        /// </summary>
        public event Action<Artifact> OnArtifactVisited;

        /// <summary>
        /// Marca a peca como visitada e dispara OnArtifactVisited.
        /// Chamadas subsequentes sao ignoradas pelo guard de foiVisitada.
        /// </summary>
        public void MarcarComoVisitada()
        {
            if (foiVisitada)
            {
                return;
            }
            foiVisitada = true;
            OnArtifactVisited?.Invoke(this);
        }
    }
}
