import { createWalletClient, http } from "viem";
import { privateKeyToAccount } from "viem/accounts";
import { hardhat } from "viem/chains";

// Conta index 2 — o "visitor" nos testes
const VISITOR_PRIVATE_KEY = "0x5de4111afa1a4b94908f83103eb1f1706367c2e68ca870fc3fb9a804cdab365a";

const account = privateKeyToAccount(VISITOR_PRIVATE_KEY);

const client = createWalletClient({
  account,
  chain: hardhat,
  transport: http(),
});

const message = "Login MuseuVerse";

async function main() {
  const signature = await client.signMessage({ message });

  console.log("\n📋 Use esses valores no Postman:\n");
  console.log(`walletAddress: ${account.address}`);
  console.log(`message: ${message}`);
  console.log(`signature: ${signature}`);
  console.log("\nBody JSON:");
  console.log(JSON.stringify({ walletAddress: account.address, message, signature }, null, 2));
}

main();