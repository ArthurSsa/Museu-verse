import "dotenv/config";
import express from "express";
import mintRouter from "./routes/mint";

const requiredEnvVars = ["SIGNER_PRIVATE_KEY", "CONTRACT_ADDRESS", "JWT_SECRET"];

for (const envVar of requiredEnvVars) {
    if (!process.env[envVar]) {
        console.error(`Error: variavel de ambiente ausente ${envVar}`);
        process.exit(1);
    }
}
const app = express();
const PORT = process.env.PORT || 3000;

app.use(express.json());

app.use("/api", mintRouter);

app.get("/health", (_, res) => {
    res.json({ status: 'ok' });
});

app.listen(PORT, () => {
    console.log(`Servidor rodando na porta ${PORT}`);
    console.log(`Rotas disponíveis:`);
    console.log(`POST /api/auth/login`);
    console.log(`POST /api/visit`);
    console.log(`POST /api/mint`);
});