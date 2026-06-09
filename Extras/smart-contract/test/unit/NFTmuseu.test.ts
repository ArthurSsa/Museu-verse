import { describe, it, beforeEach } from "node:test";
import  assert from "node:assert";
import {network} from "hardhat";

async function signMintRequest( walletClient: any, contractAddress: '0x${string}', to: '0x${string}', visitorName: string, expiration: bigint ): Promise<'0x${string}'> {
    return await walletClient.signTypedData({
    domain: {
        name: "NFTmuseu",
        version: "1",
        chainId: 31337,
        verifyingContract: contractAddress,
    },
    types: {
        MintRequest: [
            {name: "to", type: "address"},
            {name: "visitorName", type: "string"},
            {name: "expiration", type: "uint256"},
        ]
    },
    primaryType: "MintRequest",
    message: { to, visitorName, expiration },
  }); 
}

describe("NFTmuseu", async function () {
    let viem: any;
    let contractAddress: '0x${string}';
    let admin: any;
    let authorizedSigner: any;
    let visitor: any;
    let attacker: any;

    beforeEach(async function () {

        const conn = await network.create();
        viem = conn.viem;

        const [w0, w1, w2, w3] = await viem.getWalletClients();
        admin = w0;
        authorizedSigner = w1;
        visitor = w2;
        attacker = w3;

        const contract = await viem.deployContract("NFTmuseu", [
            admin.account.address,
            authorizedSigner.account.address
        ]);
        contractAddress = contract.address;
    });

    describe("deploy", async function () {
        it("Deve inicializar com os parametros corretos", async function () {
            const contract = await viem.getContractAt("NFTmuseu", contractAddress);
            const name = await contract.read.name();
            const symbol = await contract.read.symbol();
            assert.strictEqual(name, "Museu Nacional");
            assert.strictEqual(symbol, "MNRIO");
        });
    });

    describe("mintNFT", async function () {
        it("Deve mintar com assinatura valida", async function () {
            const expiration = BigInt(Math.floor(Date.now() / 1000) + 3600);
            const signature = await signMintRequest(authorizedSigner, contractAddress, visitor.account.address, "Joao testando", expiration);

            const contract = await viem.getContractAt("NFTmuseu", contractAddress, {
                client: {wallet: visitor}
            });

            await contract.write.mintNFT([
                visitor.account.address,
                "Joao testando",
                expiration,
                signature
            ]);

            const balance = await contract.read.balanceOf([visitor.account.address]);
            assert.strictEqual(balance, 1n);
        });

        it("deve rejeitar assinatura invalida", async function () {
            const expiration = BigInt(Math.floor(Date.now() / 1000) - 3600);
            const signature = await signMintRequest(authorizedSigner, contractAddress, visitor.account.address, "Joao testando", expiration);

            const contract = await viem.getContractAt("NFTmuseu", contractAddress, {
                client: {wallet: visitor}
            });

            await assert.rejects(
                contract.write.mintNFT([
                    visitor.account.address,
                    "Joao testando",
                    expiration,
                    signature
                ])
            );
    });

    it("deve rejeitar assinatura de signer nao autorizado", async function () {
        const expiration = BigInt(Math.floor(Date.now() / 1000) + 3600);
        const signature = await signMintRequest(attacker, contractAddress, visitor.account.address, "Joao testando", expiration);

        const contract = await viem.getContractAt("NFTmuseu", contractAddress, {
            client: {wallet: visitor}
        });

        await assert.rejects(
            contract.write.mintNFT([
                visitor.account.address,
                "Joao testando",
                expiration,
                signature
            ])
        );
    });
});

    
    it("deve rejeitar assinatura reutilizada", async function () {
        const expiration = BigInt(Math.floor(Date.now() / 1000) + 3600);
        const signature = await signMintRequest(authorizedSigner, contractAddress, visitor.account.address, "Joao testando", expiration);

        const contract = await viem.getContractAt("NFTmuseu", contractAddress, {
            client: {wallet: visitor}
        });

        await contract.write.mintNFT([
            visitor.account.address,
            "Joao testando",
            expiration,
            signature
        ]);

        await assert.rejects(
            contract.write.mintNFT([
                visitor.account.address,
                "Joao testando",
                expiration,
                signature
            ])
        );
        });

        it("deve rejeitar mint duplo no mesmo endereco", async function () {
            const expiration = BigInt(Math.floor(Date.now() / 1000) + 3600);

            const signature1 = await signMintRequest(authorizedSigner, contractAddress, visitor.account.address, "Joao testando", expiration);

            const signature2 = await signMintRequest(authorizedSigner, contractAddress, visitor.account.address, "Joao testando", expiration + 1n);

            const contract = await viem.getContractAt("NFTmuseu", contractAddress, {
                client: {wallet: visitor}
            });

            await contract.write.mintNFT([
                visitor.account.address,
                "Joao testando",
                expiration,
                signature1
            ]);

            await assert.rejects(
                contract.write.mintNFT([
                    visitor.account.address,
                    "Joao testando",
                    expiration + 1n,
                    signature2
                ])
            );
});

    describe("burn", async function () {
        it("deve permitir que o dono queime seu token", async function () {
            const expiration = BigInt(Math.floor(Date.now() / 1000) + 3600);
            const signature = await signMintRequest(authorizedSigner, contractAddress, visitor.account.address, "Joao testando", expiration);

            const contract = await viem.getContractAt("NFTmuseu", contractAddress, {
                client: {wallet: visitor}
            });

            await contract.write.mintNFT([
                visitor.account.address,
                "Joao testando",
                expiration,
                signature
            ]);

            await contract.write.burn([0n]);
            const balance = await contract.read.balanceOf([visitor.account.address]);
            assert.strictEqual(balance, 0n);
        });
        it("admin deve conseguir queimar token", async function () {
            const expiration = BigInt(Math.floor(Date.now() / 1000) + 3600);
            const signature = await signMintRequest(authorizedSigner, contractAddress, visitor.account.address, "Joao testando", expiration);

            const visitorContract = await viem.getContractAt("NFTmuseu", contractAddress, {
                client: {wallet: visitor}
            });

            await visitorContract.write.mintNFT([
                visitor.account.address,
                "Joao testando",
                expiration,
                signature
            ]);

            const adminContract = await viem.getContractAt("NFTmuseu", contractAddress, {
                client: {wallet: admin}
            });
            await adminContract.write.burn([0n]);
            const balance = await visitorContract.read.balanceOf([visitor.account.address]);
            assert.strictEqual(balance, 0n);
        });
        it("deve impedir que terceiros queimem token", async function () {
            const expiration = BigInt(Math.floor(Date.now() / 1000) + 3600);
            const signature = await signMintRequest(authorizedSigner, contractAddress, visitor.account.address, "Joao testando", expiration);
            const visitorContract = await viem.getContractAt("NFTmuseu", contractAddress, {
                client: {wallet: visitor}
            });
            await visitorContract.write.mintNFT([
                visitor.account.address,
                "Joao testando",
                expiration,
                signature
            ]);
            const attackerContract = await viem.getContractAt("NFTmuseu", contractAddress, {
                client: {wallet: attacker}
            });

            await assert.rejects(
                attackerContract.write.burn([0n])
            );
        });
    });
});