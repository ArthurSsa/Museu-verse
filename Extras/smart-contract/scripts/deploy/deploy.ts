import "dotenv/config";
import {network} from "hardhat";
import { privateKeyToAccount } from "viem/accounts";


async function main() {

    console.log("RPC URL:", process.env.SEPOLIA_RPC_URL);
    console.log("PRIVATE KEY existe:", !!process.env.SEPOLIA_PRIVATE_KEY);

    if (!process.env.SEPOLIA_PRIVATE_KEY) {
        console.error("Error: SEPOLIA_PRIVATE_KEY faltando no arquivo .env");
        process.exit(1);
    }

    const conn = await network.create("sepolia");
    const viem = conn.viem;

    const admin = privateKeyToAccount(process.env.SEPOLIA_PRIVATE_KEY as `0x${string}`);

    const authorizedSigner = privateKeyToAccount(
    (process.env.SIGNER_PRIVATE_KEY ?? process.env.SEPOLIA_PRIVATE_KEY) as `0x${string}`);
 
    console.log("Deploying NFTmuseu na Sepolia...");
    console.log("Admin:  ", admin.address);
    console.log("AuthorizedSigner: ", authorizedSigner.address);

    const contract = await viem.deployContract("NFTmuseu", [
    admin.address,
    authorizedSigner.address,
    ]);

    console.log("\nContrato deployado na Sepolia!");
    console.log(`   CONTRACT_ADDRESS=${contract.address}`);
    console.log("\nVerifique em:");
    console.log(`   https://sepolia.etherscan.io/address/${contract.address}`);
}

main().catch((error) => {
    console.error(error);
    process.exit(1);
});