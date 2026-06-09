import {createWalletClient, http} from 'viem';
import { privateKeyToAccount } from 'viem/accounts';
import {sepolia} from 'viem/chains';

const domain = {
    name: 'NFTmuseu',
    version: '1',
    chainId: 11155111,
    verifyingContract: process.env.VERIFYING_CONTRACT_ADDRESS as `0x${string}`,
} as const;

const types = {
    MintRequest: [
        { name: 'to', type: 'address' },
        { name: 'visitorName', type: 'string' },
        { name: 'expiration', type: 'uint256' },
    ],
} as const;

export async function signMintRequest(to: `0x${string}`, visitorName: string, expiration: bigint): Promise<`0x${string}`> {

    const account = privateKeyToAccount(process.env.SIGNER_PRIVATE_KEY as `0x${string}`);

    const client = createWalletClient({
    account,
    chain: sepolia,
    transport: http(),
    });
    const signature = await client.signTypedData({
        domain, types, primaryType: 'MintRequest', message: { to, visitorName, expiration },
    });
    return signature;
}