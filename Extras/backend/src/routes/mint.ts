import { Router, Request, Response } from 'express';
import { randomUUID } from 'crypto';
import { signMintRequest } from '../services/signer';
import { apiKeyMiddleware } from '../middleware/apiKey';

const router = Router();

// ─── Estado em memória ────────────────────────────────────────────────────────
// visitToken → { visitorName, expiresAt }
// Tokens expiram em 2h para não acumular memória.
const pendingVisits = new Map<string, { visitorName: string; expiresAt: number }>();

function limparExpirados() {
    const agora = Date.now();
    for (const [token, entry] of pendingVisits) {
        if (entry.expiresAt < agora) pendingVisits.delete(token);
    }
}

// ─── POST /api/visit-complete ─────────────────────────────────────────────────
// Chamado pelo Unity quando o jogador conclui as 4 peças.
// Autenticado por UNITY_API_KEY no header x-api-key.
// Retorna um visitToken opaco que o frontend vai usar para solicitar a assinatura.
router.post('/visit-complete', apiKeyMiddleware, (req: Request, res: Response) => {
    const { visitorName } = req.body;

    if (!visitorName || typeof visitorName !== 'string' || visitorName.trim().length === 0) {
        return res.status(400).json({ error: 'visitorName é obrigatório' });
    }

    const nome = visitorName.trim();

    if (nome.length >= 35) {
        return res.status(400).json({ error: 'visitorName deve ter menos de 35 caracteres' });
    }

    limparExpirados();

    const token = randomUUID();
    // Token válido por 2 horas — tempo suficiente para o usuário conectar a carteira
    pendingVisits.set(token, { visitorName: nome, expiresAt: Date.now() + 2 * 60 * 60 * 1000 });

    return res.json({ visitToken: token });
});

// ─── POST /api/mint ───────────────────────────────────────────────────────────
// Chamado pelo frontend após o usuário conectar a carteira.
// Valida o visitToken, assina o voucher EIP-712 e apaga o token (uso único).
router.post('/mint', async (req: Request, res: Response) => {
    const { visitToken, walletAddress, visitorName } = req.body;

    if (!visitToken || !walletAddress || !visitorName) {
        return res.status(400).json({ error: 'visitToken, walletAddress e visitorName são obrigatórios' });
    }

    // Validar formato do endereço Ethereum
    if (!/^0x[0-9a-fA-F]{40}$/.test(walletAddress)) {
        return res.status(400).json({ error: 'walletAddress inválido' });
    }

    const entry = pendingVisits.get(visitToken);

    if (!entry) {
        return res.status(403).json({ error: 'visitToken inválido ou expirado' });
    }

    if (entry.expiresAt < Date.now()) {
        pendingVisits.delete(visitToken);
        return res.status(403).json({ error: 'visitToken expirado' });
    }

    // visitorName do body deve bater com o que foi registrado pelo Unity
    if (visitorName.trim() !== entry.visitorName) {
        return res.status(403).json({ error: 'visitorName não corresponde ao token' });
    }

    try {
        const expiration = BigInt(Math.floor(Date.now() / 1000) + 3600); // 1 hora para assinar

        const signature = await signMintRequest(
            walletAddress.toLowerCase() as `0x${string}`,
            entry.visitorName,
            expiration
        );

        // Token de uso único — apaga após assinar
        pendingVisits.delete(visitToken);

        return res.json({
            signature,
            expiration: expiration.toString(),
            visitorName: entry.visitorName,
            walletAddress: walletAddress.toLowerCase(),
        });
    } catch (err) {
        console.error('[mint] Erro ao assinar:', err);
        return res.status(500).json({ error: 'erro ao gerar assinatura' });
    }
});

export default router;