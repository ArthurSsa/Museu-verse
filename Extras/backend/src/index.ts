import 'dotenv/config';
import express from 'express';
import mintRouter from './routes/mint';

const requiredEnvVars = ['SIGNER_PRIVATE_KEY', 'VERIFYING_CONTRACT_ADDRESS', 'UNITY_API_KEY'];

for (const envVar of requiredEnvVars) {
    if (!process.env[envVar]) {
        console.error(`Erro: variável de ambiente ausente — ${envVar}`);
        process.exit(1);
    }
}

const app = express();
const PORT = process.env.PORT || 3000;

app.use(express.json());

// CORS simples — permite o frontend do Netlify chamar o backend
app.use((req, res, next) => {
    const origin = req.headers.origin ?? '';
    const allowed = (process.env.ALLOWED_ORIGIN ?? 'https://museu-verse.netlify.app').split(',');
    if (allowed.includes(origin) || process.env.NODE_ENV === 'development') {
        res.setHeader('Access-Control-Allow-Origin', origin || '*');
        res.setHeader('Access-Control-Allow-Headers', 'Content-Type, x-api-key');
        res.setHeader('Access-Control-Allow-Methods', 'POST, OPTIONS');
    }
    if (req.method === 'OPTIONS') return res.sendStatus(204);
    next();
});

app.use('/api', mintRouter);

app.get('/health', (_, res) => res.json({ status: 'ok' }));

app.listen(PORT, () => {
    console.log(`Servidor rodando na porta ${PORT}`);
    console.log('Rotas:');
    console.log('  POST /api/visit-complete  ← Unity chama com x-api-key');
    console.log('  POST /api/mint            ← Frontend chama após conectar carteira');
});