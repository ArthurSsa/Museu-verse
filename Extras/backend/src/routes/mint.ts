import {Router, Request, Response} from 'express';
import jwt from 'jsonwebtoken';
import {verifyMessage} from 'viem';
import {signMintRequest} from '../services/signer';

const router = Router();

const visitedPieces = new Map<string, Set<string>>();

const VALID_PIECES = new Set(["piece1", "piece2", "piece3", "piece4"]);
const REQUIRED_PIECES = 4;

function authMiddleware(req: Request, res: Response, next: Function) {
    const authHeader = req.headers.authorization;

    if(!authHeader || !authHeader.startsWith('Bearer ')) {
        return res.status(401).json({error: 'token nao fornecido'});
    }
    const token = authHeader.split(' ')[1];

    try {
        const payload = jwt.verify(token, process.env.JWT_SECRET!) as {walletAddress: string};
        (req as any).walletAddress = payload.walletAddress;
        next();
    } catch {
        return res.status(401).json({error: 'token invalido ou expirado'});
    }
}

router.post('/auth/login', async (req: Request, res: Response) => {
    const  {walletAddress, message, signature} = req.body;

    if(!walletAddress || !message || !signature) {
        return res.status(400).json({error: 'dados incompletos'});
    }
    try {     
        const isValid = await verifyMessage({
            address: walletAddress as `0x${string}`,
            message,
            signature: signature as `0x${string}`
        });

        if(!isValid) {
            return res.status(401).json({error: 'assinatura invalida'});
        }

        const token = jwt.sign({walletAddress: walletAddress.toLowerCase()}, process.env.JWT_SECRET!, {expiresIn: '1h'});
        return res.json({token});
    } catch (error) {
        return res.status(500).json({error: 'erro ao verificar assinatura'});
    }
});

router.post('/visit', authMiddleware, (req: Request, res: Response) => {
    const walletAddress = (req as any).walletAddress as string;
    const {pieceId} = req.body;

    if(!pieceId || !VALID_PIECES.has(pieceId)) {
        return res.status(400).json({error: 'peça inválida', validPieces: Array.from(VALID_PIECES)});
    }

    if(!visitedPieces.has(walletAddress)) {
        visitedPieces.set(walletAddress, new Set());
    }

    visitedPieces.get(walletAddress)!.add(pieceId);

    const visited = Array.from(visitedPieces.get(walletAddress)!);
    const remaining = REQUIRED_PIECES - visited.length;
    return res.json({visited, totalVisited: visited.length, remaining: Math.max(0, remaining),canMint: visited.length >= REQUIRED_PIECES});
});

router.post('/mint', authMiddleware, async (req: Request, res: Response) => {
    const walletAddress = (req as any).walletAddress as string;
    const {visitorName} = req.body;

    if(!visitorName || visitorName.trim().length === 0) {
        return res.status(400).json({error: 'nome do visitante é obrigatório'});
    }

    if(visitorName.length >= 35) {
        return res.status(403).json({error: 'visitante deve ter menos que 35 caracteres'});
    }

    const visited = visitedPieces.get(walletAddress);

    if(!visited || visited.size < REQUIRED_PIECES) {
        return res.status(403).json({error: 'visitante não visitou peças suficientes', visited: visited ? Array.from(visited) : [],totalVisited: visited?.size??  0, required: REQUIRED_PIECES});
    }

    try {
        const expiration = BigInt(Math.floor(Date.now() / 1000) + 3600); // 1 hora
        
        const signature = await signMintRequest(walletAddress as `0x${string}`, visitorName.trim(), expiration);

        visitedPieces.delete(walletAddress);

        return res.json({signature, expiration: expiration.toString(), visitorName: visitorName.trim(), walletAddress});
    } catch (err) {
        console.error('Erro ao gerar assinatura de mintagem:', err);
        return res.status(500).json({error: 'erro ao gerar assinatura de mintagem'});
    }
});

export default router;