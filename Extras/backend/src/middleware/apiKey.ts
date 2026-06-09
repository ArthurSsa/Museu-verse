import { Request, Response, NextFunction } from 'express';

export function apiKeyMiddleware(req: Request, res: Response, next: NextFunction) {
    const key = req.headers['x-api-key'];
    
    if (!key || key !== process.env.UNITY_API_KEY) {
        return res.status(401).json({ error: 'API key inválida' });
    }
    next();
}