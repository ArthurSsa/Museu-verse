# MuseuVerse

Museu virtual imersivo em 3D que preserva digitalmente o acervo perdido do **Museu Nacional do Rio de Janeiro** (incêndio de 2018). O visitante percorre, em primeira pessoa e direto no navegador, quatro peças emblemáticas que se perderam — e, ao final, pode resgatar um certificado de visita como NFT.

🎮 **Jogue no navegador:** https://arthurssa.itch.io/museuverse

> Projeto do desafio **ExpoVerse** — Hackathon Web3 RESTIC 29.

## Sobre o projeto

Em setembro de 2018, um incêndio destruiu cerca de 85% do acervo do Museu Nacional do Rio de Janeiro. O MuseuVerse recria, em um ambiente 3D navegável, quatro peças perdidas — preservando sua memória e tornando-a acessível a qualquer pessoa, em qualquer lugar.

As quatro peças:
- **Sarcófago de Sha-Amun-em-su** — sarcófago egípcio lacrado há ~2.800 anos.
- **Trono de Adandozan** — trono do Reino do Daomé, presente diplomático ao Brasil no séc. XIX.
- **Meteorito de Bendegó** — maior meteorito achado no Brasil; metálico, resistiu ao fogo.
- **Luzia** — o fóssil humano mais antigo das Américas (~11.500 anos).

## Objetivo

Transformar a preservação de um patrimônio perdido em uma experiência imersiva e participativa: um museu da memória, navegável no navegador, com um certificado de visita registrado on-chain.

## A experiência

O visitante digita o nome e entra no museu. O ambiente começa escuro e a iluminação guia o percurso, peça por peça. Ao se aproximar de uma peça e pressionar **E**, abre-se um painel curatorial com o texto histórico e uma narração em áudio. Um contador acompanha o progresso; visitadas as quatro peças, o certificado de visita é emitido, com a opção de resgate como NFT.

## Tecnologias

- **Unity 2022.3.62f3 LTS** (Built-in Render Pipeline) → build **WebGL**
- Navegação em primeira pessoa + interação por **raycast**
- Narração de áudio gerada com **ElevenLabs**
- **Blockchain:** contrato **ERC-721** (Solidity / Hardhat) na rede **Ethereum Sepolia** (testnet)
- **Backend (Node / Express):** valida a visita e assina cada resgate com **assinatura EIP-712** (signer autorizado)
- **Frontend de resgate:** página web que conecta a carteira e chama o contrato
- **Hospedagem:** itch.io (experiência Unity) · Netlify (página de resgate) · Railway (backend)

## Estrutura

```
museuverse-unity/   — projeto Unity: a experiência imersiva principal (cenas, scripts, modelos e texturas dentro de Assets/)
Extras/             — módulos complementares do projeto:
    backend/        — serviço autorizado que assina os comprovantes de mint (EIP-712) do certificado (hospedado no Railway)
    frontend/       — interface web de resgate do certificado NFT (hospedada no Netlify)
    smart-contract/ — contrato do certificado NFT (Solidity / Hardhat)
    docs/           — vídeo-pitch, slides e materiais
```

Além da experiência principal desenvolvida em **Unity** (`museuverse-unity/`), o repositório reúne em **`Extras/`** todos os módulos complementares do projeto: **backend**, **frontend**, **smart-contract** e **documentação**.

> O build WebGL (pasta `Builds/`) e o modelo 3D do saguão (`.fbx`, acima de 100 MB) não são versionados no repositório — a versão jogável roda no itch.io.

## Como executar

**Jogar (recomendado):** acesse **https://arthurssa.itch.io/museuverse** em qualquer navegador.

**Rodar localmente (Unity):**
1. Abra a pasta `museuverse-unity/` no **Unity 2022.3.62f3 LTS** (com suporte a build WebGL).
2. Abra a cena `Assets/_Project/Scenes/02_Museum`.
3. Pressione **Play** (WASD + mouse para andar e olhar; **E** para interagir com as peças).

## Web3 — Certificado NFT

Ao concluir a visita, o jogador recebe um certificado resgatável como NFT na **Ethereum Sepolia** (testnet), via contrato **ERC-721**. O resgate acontece em uma **página web** (fora da aplicação Unity): o visitante conecta a carteira, o **backend valida a visita e assina um voucher EIP-712**, e a página chama o `mintNFT` do contrato com essa assinatura — garantindo que só quem concluiu a visita consiga mintar o certificado.

- **Endereço do contrato:** `0x794958920a4e39f2349d653526e8ee9c48b9592c` ([ver na Sepolia Etherscan](https://sepolia.etherscan.io/address/0x794958920a4e39f2349d653526e8ee9c48b9592c))
- **Link de resgate:** https://museu-verse.netlify.app/

## Requisitos mínimos

- ✅ **Ambiente navegável** — museu 3D explorável em primeira pessoa
- ✅ **Interação básica** — a tecla **E** abre os painéis curatoriais das peças
- ✅ **Fluxo funcional** — boas-vindas → exploração → certificado (4/4)
- ✅ **README funcional** — este documento
- ✅ **Vídeo-pitch** — [_(adicionar link do YouTube)_](https://youtu.be/RwitvMrYLrI) [Gameplay](https://youtu.be/UR2DNKXBv6Y)

## Equipe

- **Arthur Santos Sampaio** — Desenvolvedor Unity
- **Ricardo Augusto Belo da Silva** — Modelador 3D (Blender)
- **Anthony Davi de Sousa Araujo** — Desenvolvedor Fullstack (Blockchain / UI/UX)
- **Guilherme Pessoa Marinho** — Desenvolvedor Blockchain / Smart Contracts

## Uso de Inteligência Artificial

Conforme as regras do hackathon, declaramos o uso de ferramentas de IA no projeto:
- **Claude (Anthropic)** — apoio na escrita e revisão de código (C#), na configuração do projeto Unity e na redação da documentação.
- **ElevenLabs** — geração das narrações em áudio das peças.
