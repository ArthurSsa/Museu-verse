import { useState, useEffect, useCallback } from "react";
import { ethers } from "ethers";

// ─── Config ────────────────────────────────────────────────────────────────────
// URL do backend (Railway). Pode sobrescrever no build com VITE_BACKEND_URL.
const BACKEND_URL =
  (import.meta.env.VITE_BACKEND_URL as string | undefined) ||
  "https://museu-verse-production.up.railway.app";

const CONTRACT_ADDRESS = "0x794958920a4e39f2349d653526e8ee9c48b9592c";

// Precisa ser idêntica à mensagem que o backend espera no /api/auth/login
const LOGIN_MESSAGE = "Login MuseuVerse";

// IDs das peças — precisam bater com VALID_PIECES do backend
const PIECES = ["piece1", "piece2", "piece3", "piece4"];

const ABI = [
  "function mintNFT(address to, string visitorName, uint256 expiration, bytes signature)",
];

const SEPOLIA_CHAIN_ID = "0xaa36a7"; // 11155111

const SEPOLIA_PARAMS = {
  chainId: SEPOLIA_CHAIN_ID,
  chainName: "Sepolia",
  rpcUrls: ["https://ethereum-sepolia-rpc.publicnode.com"],
  nativeCurrency: { name: "SepoliaETH", symbol: "ETH", decimals: 18 },
  blockExplorerUrls: ["https://sepolia.etherscan.io"],
};
// ───────────────────────────────────────────────────────────────────────────────

type StatusKind = "idle" | "ok" | "warn";
type ResultKind = "success" | "error" | null;

declare global {
  interface Window {
    ethereum?: ethers.Eip1193Provider & {
      request: (args: { method: string; params?: unknown[] }) => Promise<unknown>;
      on?: (event: string, handler: (...args: unknown[]) => void) => void;
    };
  }
}

function short(addr: string) {
  return addr.slice(0, 6) + "…" + addr.slice(-4);
}

// Chamada genérica ao backend
async function api<T>(
  path: string,
  method: string,
  token: string | null,
  body?: unknown
): Promise<T> {
  let res: Response;
  try {
    res = await fetch(BACKEND_URL + path, {
      method,
      headers: {
        "Content-Type": "application/json",
        ...(token ? { Authorization: "Bearer " + token } : {}),
      },
      body: body ? JSON.stringify(body) : undefined,
    });
  } catch {
    throw new Error(
      "Não foi possível falar com o servidor. Confira se o backend está no ar e com CORS liberado."
    );
  }
  let data: { error?: string } = {};
  try {
    data = await res.json();
  } catch {
    /* resposta sem corpo JSON */
  }
  if (!res.ok) throw new Error(data?.error || `Erro ${res.status} em ${path}`);
  return data as T;
}

// ─── Sub-components ────────────────────────────────────────────────────────────

function Spinner({ light = false }: { light?: boolean }) {
  return (
    <span
      style={{
        display: "inline-block",
        width: 13,
        height: 13,
        border: light
          ? "2px solid rgba(207,229,202,.3)"
          : "2px solid rgba(28,24,19,.3)",
        borderTopColor: light ? "#cfe5ca" : "#1c1813",
        borderRadius: "50%",
        animation: "spin .7s linear infinite",
        verticalAlign: "-2px",
        marginRight: 7,
      }}
    />
  );
}

function Certificate({ name }: { name: string }) {
  return (
    <div className="cert">
      <div className="seal">
        <span>MV</span>
      </div>
      <div className="cert-label">Certificado de Visita</div>
      <div className="cert-name">MuseuVerse</div>
      <div className="cert-for">
        — em nome de <b>{name || "visitante"}</b> —
      </div>
      <div className="cert-meta">NFT · ERC-721 · ETHEREUM SEPOLIA</div>
    </div>
  );
}

// ─── Main component ────────────────────────────────────────────────────────────

export default function MintPage() {
  // Wallet state
  const [account, setAccount] = useState<string | null>(null);
  const [onSepolia, setOnSepolia] = useState(false);

  // Form / flow state
  const [name, setName] = useState("");
  const [statusText, setStatusText] = useState("Carteira não conectada");
  const [statusKind, setStatusKind] = useState<StatusKind>("idle");
  const [resultHtml, setResultHtml] = useState("");
  const [resultKind, setResultKind] = useState<ResultKind>(null);
  const [claiming, setClaiming] = useState(false);
  const [claimStep, setClaimStep] = useState("Resgatando…");

  const setStatus = useCallback((text: string, kind: StatusKind = "idle") => {
    setStatusText(text);
    setStatusKind(kind);
  }, []);

  const showResult = useCallback((html: string, kind: ResultKind) => {
    setResultHtml(html);
    setResultKind(kind);
  }, []);

  const detectNetwork = useCallback(
    async (currentAccount: string | null) => {
      if (!window.ethereum) return;
      const chainId = (await window.ethereum.request({
        method: "eth_chainId",
      })) as string;
      const isSepolia = chainId.toLowerCase() === SEPOLIA_CHAIN_ID.toLowerCase();
      setOnSepolia(isSepolia);

      if (currentAccount) {
        if (isSepolia) {
          setStatus(
            `Conectado: <span class="addr">${short(currentAccount)}</span> · Sepolia`,
            "ok"
          );
        } else {
          setStatus("Conectado, mas na rede errada — troque para Sepolia", "warn");
        }
      }
    },
    [setStatus]
  );

  // Listeners do MetaMask
  useEffect(() => {
    if (!window.ethereum?.on) return;

    const handleAccounts = (accounts: unknown) => {
      const accts = accounts as string[];
      const addr = accts[0] ?? null;
      setAccount(addr);
      if (!addr) setStatus("Carteira não conectada");
      detectNetwork(addr);
    };

    const handleChain = () => detectNetwork(account);

    window.ethereum.on("accountsChanged", handleAccounts);
    window.ethereum.on("chainChanged", handleChain);
  }, [account, detectNetwork, setStatus]);

  async function connect() {
    if (!window.ethereum) {
      showResult(
        'Carteira não encontrada. Instale o <a href="https://metamask.io/" target="_blank" rel="noopener">MetaMask</a> e recarregue a página.',
        "error"
      );
      return;
    }
    try {
      const accts = (await window.ethereum.request({
        method: "eth_requestAccounts",
      })) as string[];
      const addr = accts[0];
      setAccount(addr);
      setResultKind(null);
      setResultHtml("");
      await detectNetwork(addr);
    } catch {
      showResult("Conexão recusada na carteira.", "error");
    }
  }

  async function switchToSepolia() {
    try {
      await window.ethereum!.request({
        method: "wallet_switchEthereumChain",
        params: [{ chainId: SEPOLIA_CHAIN_ID }],
      });
    } catch (err: unknown) {
      const e = err as { code?: number };
      if (e.code === 4902) {
        try {
          await window.ethereum!.request({
            method: "wallet_addEthereumChain",
            params: [SEPOLIA_PARAMS],
          });
        } catch {
          showResult("Não foi possível adicionar a rede Sepolia.", "error");
          return;
        }
      } else {
        showResult("Não foi possível trocar de rede.", "error");
        return;
      }
    }
    await detectNetwork(account);
  }

  async function claim() {
    const visitorName = name.trim();
    if (!visitorName) {
      showResult("Digite seu nome para o certificado.", "error");
      return;
    }
    if (visitorName.length >= 35) {
      showResult("O nome deve ter menos de 35 caracteres.", "error");
      return;
    }

    setClaiming(true);
    setResultKind(null);
    setResultHtml("");

    try {
      const provider = new ethers.BrowserProvider(window.ethereum!);
      const signer = await provider.getSigner();

      // 1) Login — assina a mensagem e recebe o token JWT
      setClaimStep("Faça login: assine na carteira…");
      const loginSig = await signer.signMessage(LOGIN_MESSAGE);
      const { token } = await api<{ token: string }>(
        "/api/auth/login",
        "POST",
        null,
        { walletAddress: account, message: LOGIN_MESSAGE, signature: loginSig }
      );

      // 2) Visitas — registra as peças do museu no backend
      setClaimStep("Registrando sua visita…");
      for (const pieceId of PIECES) {
        await api("/api/visit", "POST", token, { pieceId });
      }

      // 3) Comprovante — backend valida a visita e assina o voucher (EIP-712)
      setClaimStep("Gerando seu comprovante…");
      const mint = await api<{
        signature: string;
        expiration: string;
        visitorName: string;
      }>("/api/mint", "POST", token, { visitorName });

      // 4) Mint on-chain — usa exatamente o que o backend assinou
      setClaimStep("Confirme a transação na carteira…");
      const contract = new ethers.Contract(CONTRACT_ADDRESS, ABI, signer);
      const tx = await contract.mintNFT(
        account,
        mint.visitorName,
        BigInt(mint.expiration),
        mint.signature
      );

      showResult(
        `<span class="spinner-light"></span>Transação enviada, aguardando confirmação…`,
        "success"
      );

      const receipt = await tx.wait();
      const url = `${SEPOLIA_PARAMS.blockExplorerUrls[0]}/tx/${receipt.hash}`;
      showResult(
        `✓ Certificado resgatado!<br/>Transação: <a href="${url}" target="_blank" rel="noopener">ver no Etherscan ↗</a>`,
        "success"
      );
    } catch (err: unknown) {
      const e = err as { shortMessage?: string; reason?: string; message?: string };
      const msg = e.shortMessage ?? e.reason ?? e.message ?? "Erro ao resgatar.";
      showResult("Não foi possível resgatar: " + msg, "error");
    } finally {
      setClaiming(false);
    }
  }

  const canClaim = Boolean(account && onSepolia && !claiming);

  return (
    <>
      <style>{css}</style>

      <div className="wrap">
        <div className="eyebrow">MuseuVerse · Web3</div>
        <h1>Resgatar Certificado</h1>
        <p className="sub">Você concluiu a visita. Reivindique seu certificado on-chain.</p>

        <Certificate name={name} />

        <div className="panel">
          <div className="field">
            <label htmlFor="nameInput">Seu nome (vai no certificado · obrigatório)</label>
            <input
              id="nameInput"
              type="text"
              placeholder="Ex.: Maria"
              maxLength={34}
              autoComplete="off"
              value={name}
              onChange={(e) => setName(e.target.value)}
            />
          </div>

          <div className={`status ${statusKind}`}>
            <span className="dot" />
            <span dangerouslySetInnerHTML={{ __html: statusText }} />
          </div>

          {!account && (
            <button className="btn-outline" onClick={connect}>
              Conectar carteira
            </button>
          )}

          {account && !onSepolia && (
            <button className="btn-outline" onClick={switchToSepolia}>
              Trocar para Sepolia
            </button>
          )}

          <button className="btn-gold" disabled={!canClaim} onClick={claim}>
            {claiming ? (
              <>
                <Spinner /> {claimStep}
              </>
            ) : (
              "Resgatar certificado"
            )}
          </button>

          {resultKind && (
            <div
              className={`result ${resultKind}`}
              dangerouslySetInnerHTML={{ __html: resultHtml }}
            />
          )}
        </div>

        <div className="help">
          Precisa do{" "}
          <a href="https://metamask.io/" target="_blank" rel="noopener">
            MetaMask
          </a>{" "}
          e de um pouco de <b>SepoliaETH de teste</b> (pegue num{" "}
          <a href="https://sepolia-faucet.pk910.de/" target="_blank" rel="noopener">
            faucet
          </a>
          ) pra pagar o gas.
        </div>

        <footer>
          <a
            href="https://arthurssa.itch.io/museuverse"
            target="_blank"
            rel="noopener"
          >
            ← Voltar ao museu
          </a>
        </footer>
      </div>
    </>
  );
}

// ─── Styles ────────────────────────────────────────────────────────────────────

const css = `
  @import url('https://fonts.googleapis.com/css2?family=Playfair+Display:wght@600;700&family=Inter:wght@400;500;600&display=swap');

  :root {
    --bg: #1c1813; --bg2: #2a241b; --card: #241f18;
    --gold: #c9a35a; --goldb: #d9b86a; --cream: #f0eadd; --muted: #b3a892;
    --ok: #7bb274; --err: #d2715a; --line: #3a3226;
  }

  *, *::before, *::after { box-sizing: border-box; }

  body {
    margin: 0; padding: 0;
    font-family: 'Inter', system-ui, sans-serif;
    color: var(--cream);
    background:
      radial-gradient(1200px 600px at 50% -10%, #2a241b 0%, rgba(42,36,27,0) 60%),
      radial-gradient(900px 500px at 50% 120%, #241f18 0%, rgba(36,31,24,0) 60%),
      var(--bg);
    min-height: 100vh;
    display: flex;
    align-items: center;
    justify-content: center;
    padding: 32px 18px;
  }

  .wrap { width: 100%; max-width: 540px; text-align: center; }

  .eyebrow {
    font-size: 12px; letter-spacing: 3px; color: var(--gold);
    font-weight: 600; text-transform: uppercase; margin-bottom: 6px;
  }

  h1 {
    font-family: 'Playfair Display', Georgia, serif;
    font-size: 30px; margin: 0 0 4px; color: var(--cream); font-weight: 700;
  }

  .sub { color: var(--muted); font-size: 15px; margin: 0 0 26px; }

  .cert {
    background: linear-gradient(180deg, #2a241b 0%, #241f18 100%);
    border: 1.5px solid var(--gold); border-radius: 14px;
    padding: 30px 26px 26px;
    box-shadow: 0 18px 50px rgba(0,0,0,.45), inset 0 0 0 1px rgba(201,163,90,.12);
  }

  .seal {
    width: 64px; height: 64px; margin: 0 auto 14px; border-radius: 50%;
    border: 2px solid var(--gold); display: flex; align-items: center;
    justify-content: center; box-shadow: 0 0 0 4px rgba(201,163,90,.12);
  }
  .seal span {
    font-family: 'Playfair Display', serif; font-weight: 700;
    font-size: 22px; color: var(--gold);
  }

  .cert-label { font-size: 12px; letter-spacing: 2px; color: var(--cream); text-transform: uppercase; margin-bottom: 4px; }
  .cert-name { font-family: 'Playfair Display', serif; font-size: 30px; color: var(--gold); margin: 2px 0 6px; }
  .cert-for { font-family: 'Playfair Display', serif; font-style: italic; font-size: 15px; color: var(--muted); margin-bottom: 14px; }
  .cert-for b { color: var(--cream); font-style: normal; }
  .cert-meta { font-size: 11px; letter-spacing: 1px; color: var(--gold); padding-top: 12px; border-top: 1px solid var(--line); }

  .panel { margin-top: 22px; text-align: left; }

  .field { margin-bottom: 14px; }
  .field label { display: block; font-size: 12px; color: var(--muted); margin-bottom: 6px; }
  .field input {
    width: 100%; padding: 11px 13px; border-radius: 9px; border: 1px solid var(--line);
    background: #1f1a14; color: var(--cream); font-size: 14px; font-family: inherit;
  }
  .field input:focus { outline: none; border-color: var(--gold); }

  .status {
    font-size: 13px; color: var(--muted); margin: 4px 0 16px;
    min-height: 20px; text-align: center; display: flex;
    align-items: center; justify-content: center; gap: 7px;
  }
  .dot {
    display: inline-block; width: 8px; height: 8px;
    border-radius: 50%; background: var(--muted); flex-shrink: 0;
  }
  .status.ok .dot  { background: var(--ok); }
  .status.warn .dot { background: var(--err); }
  .addr { color: var(--cream); font-family: ui-monospace, Menlo, monospace; }

  button {
    width: 100%; padding: 14px 16px; border-radius: 10px; border: none;
    cursor: pointer; font-family: inherit; font-size: 15px; font-weight: 600;
    margin-bottom: 11px; transition: background .15s, opacity .15s;
    display: flex; align-items: center; justify-content: center;
  }
  .btn-gold { background: var(--gold); color: #1c1813; }
  .btn-gold:hover:not(:disabled) { background: var(--goldb); }
  .btn-outline { background: transparent; color: var(--gold); border: 1.5px solid var(--gold); }
  .btn-outline:hover { background: rgba(201,163,90,.1); }
  button:disabled { opacity: .4; cursor: not-allowed; }

  .result {
    margin-top: 6px; padding: 13px 14px; border-radius: 10px;
    font-size: 13.5px; line-height: 1.5;
  }
  .result.success { background: rgba(123,178,116,.12); border: 1px solid rgba(123,178,116,.4); color: #cfe5ca; }
  .result.error   { background: rgba(210,113,90,.12);  border: 1px solid rgba(210,113,90,.4);  color: #eccabf; }
  .result a { color: var(--goldb); font-weight: 600; }

  .spinner-light {
    display: inline-block; width: 13px; height: 13px;
    border: 2px solid rgba(207,229,202,.3); border-top-color: #cfe5ca;
    border-radius: 50%; animation: spin .7s linear infinite;
    vertical-align: -2px; margin-right: 7px;
  }

  .help { margin-top: 22px; font-size: 12.5px; color: var(--muted); line-height: 1.6; text-align: center; }
  .help a { color: var(--gold); }

  footer { margin-top: 26px; font-size: 12px; color: var(--muted); }
  footer a { color: var(--gold); text-decoration: none; }

  @keyframes spin { to { transform: rotate(360deg); } }
`;