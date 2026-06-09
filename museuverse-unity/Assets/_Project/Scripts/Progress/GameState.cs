using UnityEngine;

namespace MuseuVerse.Progress
{
    /// <summary>
    /// Estado global da sessao, persistente entre cenas (DontDestroyOnLoad).
    /// Guarda os dados do visitante usados na tela final e na futura emissao do
    /// certificado NFT. Estruturado para receber novos campos (email, carteira)
    /// sem que quem ja consome PlayerName precise mudar.
    /// </summary>
    public class GameState : MonoBehaviour
    {
        private static GameState instancia;

        /// <summary>
        /// Acesso global. Reaproveita a instancia existente na cena ou cria uma
        /// sob demanda, garantindo que sempre haja um GameState valido mesmo que
        /// nenhum objeto tenha sido posto na cena manualmente.
        /// </summary>
        public static GameState Instance
        {
            get
            {
                if (instancia == null)
                {
                    instancia = FindObjectOfType<GameState>();
                    if (instancia == null)
                    {
                        var go = new GameObject("GameState");
                        instancia = go.AddComponent<GameState>();
                    }
                }
                return instancia;
            }
        }

        [Header("Dados do visitante")]
        [SerializeField, Tooltip("Nome digitado na tela de boas-vindas. Usado no certificado NFT.")]
        private string playerName = string.Empty;

        // Campos futuros do certificado NFT. Quando a captura existir, basta
        // descomentar o campo e a propriedade correspondente: nenhum consumidor
        // de PlayerName precisa ser alterado.
        // [SerializeField] private string email = string.Empty;
        // [SerializeField] private string walletAddress = string.Empty;

        /// <summary>Nome do visitante. Vazio ate a confirmacao na tela de boas-vindas.</summary>
        public string PlayerName
        {
            get => playerName;
            set => playerName = value;
        }

        // public string Email { get => email; set => email = value; }
        // public string WalletAddress { get => walletAddress; set => walletAddress = value; }

        private void Awake()
        {
            // Garante instancia unica que sobrevive a troca de cena.
            if (instancia != null && instancia != this)
            {
                Destroy(gameObject);
                return;
            }

            instancia = this;
            DontDestroyOnLoad(gameObject);
        }
    }
}
