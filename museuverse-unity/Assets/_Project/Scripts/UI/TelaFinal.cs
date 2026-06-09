using System;
using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using MuseuVerse.Interaction;
using MuseuVerse.Player;
using MuseuVerse.Progress;

namespace MuseuVerse.UI
{
    /// <summary>
    /// Tela final do certificado de visita.
    /// Quando o jogador clica em "Resgatar NFT", chama o backend para obter um
    /// visitToken e depois abre o site de mint com visitToken + visitorName na URL.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class TelaFinal : MonoBehaviour
    {
        // ── Referências de gameplay ──────────────────────────────────────────
        [Header("Referencias de gameplay")]
        [SerializeField] private FirstPersonController playerController;
        [SerializeField] private InteractionRaycaster  raycaster;

        // ── Elementos da tela ────────────────────────────────────────────────
        [Header("Elementos da tela")]
        [SerializeField] private GameObject painelRaiz;
        [SerializeField] private TMP_Text   textoNome;
        [SerializeField] private TMP_Text   textoContagem;

        // ── Botões ───────────────────────────────────────────────────────────
        [Header("Botoes")]
        [SerializeField] private Button    botaoClaim;
        [SerializeField] private Button    botaoFechar;
        [SerializeField] private TMP_Text  textoBotaoClaim; // label do botão de claim

        // ── Feedback ao jogador ──────────────────────────────────────────────
        [Header("Feedback")]
        [SerializeField, Tooltip("Texto de status exibido enquanto chama o backend.")]
        private TMP_Text textoStatus;

        // ── Configuração Web3 ────────────────────────────────────────────────
        [Header("Configuracao Web3")]
        [SerializeField, Tooltip("URL base do backend. Ex: https://seu-backend.railway.app")]
        private string backendUrl = "https://SEU-BACKEND.com";

        [SerializeField, Tooltip("URL base do site de mint. Ex: https://museu-verse.netlify.app")]
        private string mintSiteUrl = "https://museu-verse.netlify.app";

        [SerializeField,
         Tooltip("UNITY_API_KEY configurada no backend (.env). Mantenha em segredo — não commite a chave real.")]
        private string unityApiKey = "museuverse_unity_apikey_bemsecreto";

        [Header("Configuracao")]
        [SerializeField] private int totalPecas = 4;

        // ── Estado interno ───────────────────────────────────────────────────
        private bool estaAberta;
        private bool chamandoBackend;

        public bool EstaAberta => estaAberta;

        // ── Unity lifecycle ──────────────────────────────────────────────────

        private void Awake()
        {
            playerController ??= FindObjectOfType<FirstPersonController>();
            raycaster        ??= FindObjectOfType<InteractionRaycaster>();
        }

        private void OnEnable()
        {
            botaoClaim?.onClick.AddListener(AoClicarClaim);
            botaoFechar?.onClick.AddListener(Fechar);
        }

        private void OnDisable()
        {
            botaoClaim?.onClick.RemoveListener(AoClicarClaim);
            botaoFechar?.onClick.RemoveListener(Fechar);
        }

        private void Start()
        {
            painelRaiz?.SetActive(false);
        }

        private void Update()
        {
            if (!estaAberta) return;
            if (Input.GetKeyDown(KeyCode.Escape)) Fechar();
        }

        // ── API pública ──────────────────────────────────────────────────────

        /// <summary>
        /// Abre o certificado. Chamado pelo ProgressManager via UnityEvent no inspector.
        /// </summary>
        public void Abrir()
        {
            estaAberta = true;
            painelRaiz?.SetActive(true);

            string nome = GameState.Instance.PlayerName;
            if (string.IsNullOrWhiteSpace(nome)) nome = "Visitante";

            if (textoNome     != null) textoNome.text     = nome;
            if (textoContagem != null) textoContagem.text = $"{totalPecas}/{totalPecas} peças visitadas";

            SetStatus(string.Empty);
            SetBotaoClaimInterativo(true);

            playerController?.SetInputEnabled(false);
            raycaster?.SetInputEnabled(false);
        }

        /// <summary>
        /// Fecha o certificado e devolve o controle ao jogador.
        /// </summary>
        public void Fechar()
        {
            estaAberta = false;
            painelRaiz?.SetActive(false);
            playerController?.SetInputEnabled(true);
            raycaster?.SetInputEnabled(true);
        }

        // ── Claim ────────────────────────────────────────────────────────────

        private void AoClicarClaim()
        {
            if (chamandoBackend) return;
            StartCoroutine(RotinaClaim());
        }

        private IEnumerator RotinaClaim()
        {
            chamandoBackend = true;
            SetBotaoClaimInterativo(false);
            SetStatus("Gerando certificado…");

            string nome = GameState.Instance.PlayerName;
            if (string.IsNullOrWhiteSpace(nome)) nome = "Visitante";

            // ── 1. Chamar o backend para obter o visitToken ──────────────────
            string endpoint = backendUrl.TrimEnd('/') + "/api/visit-complete";
            string corpo    = $"{{\"visitorName\":\"{EscapeJson(nome)}\"}}";

            using var req = new UnityWebRequest(endpoint, "POST");
            req.uploadHandler   = new UploadHandlerRaw(Encoding.UTF8.GetBytes(corpo));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("x-api-key", unityApiKey);

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[TelaFinal] Erro ao chamar backend: {req.error} | {req.downloadHandler.text}");
                SetStatus("Erro ao conectar ao servidor. Tente novamente.");
                SetBotaoClaimInterativo(true);
                chamandoBackend = false;
                yield break;
            }

            // ── 2. Parsear resposta { "visitToken": "..." } ──────────────────
            string visitToken = ExtrairCampoJson(req.downloadHandler.text, "visitToken");
            if (string.IsNullOrEmpty(visitToken))
            {
                Debug.LogError($"[TelaFinal] visitToken ausente na resposta: {req.downloadHandler.text}");
                SetStatus("Resposta inesperada do servidor. Tente novamente.");
                SetBotaoClaimInterativo(true);
                chamandoBackend = false;
                yield break;
            }

            // ── 3. Montar URL e abrir o site de mint ────────────────────────
            string url = mintSiteUrl.TrimEnd('/')
                + "?visitToken=" + Uri.EscapeDataString(visitToken)
                + "&visitorName=" + Uri.EscapeDataString(nome);

            Debug.Log($"[TelaFinal] Abrindo mint: {url}");
            Application.OpenURL(url);

            SetStatus("Site de resgate aberto! Continue lá.");
            // Não reabilita o botão — token é de uso único.
            chamandoBackend = false;
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private void SetBotaoClaimInterativo(bool interativo)
        {
            if (botaoClaim == null) return;
            botaoClaim.interactable = interativo;
            if (textoBotaoClaim != null)
                textoBotaoClaim.text = interativo ? "Resgatar NFT" : "Aguarde…";
        }

        private void SetStatus(string msg)
        {
            if (textoStatus != null) textoStatus.text = msg;
        }

        /// <summary>
        /// Mini-parser JSON sem dependências externas.
        /// Extrai o valor de string de um campo de nível raiz: { "campo": "valor" }
        /// </summary>
        private static string ExtrairCampoJson(string json, string campo)
        {
            string chave = $"\"{campo}\"";
            int idx = json.IndexOf(chave, StringComparison.Ordinal);
            if (idx < 0) return null;

            int colon = json.IndexOf(':', idx + chave.Length);
            if (colon < 0) return null;

            int quote1 = json.IndexOf('"', colon + 1);
            if (quote1 < 0) return null;

            int quote2 = json.IndexOf('"', quote1 + 1);
            if (quote2 < 0) return null;

            return json.Substring(quote1 + 1, quote2 - quote1 - 1);
        }

        /// <summary>Escapa aspas e barras para inserir em JSON manual.</summary>
        private static string EscapeJson(string s) =>
            s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}