// SPDX-License-Identifier: MIT
pragma solidity ^0.8.24;

import "@openzeppelin/contracts/token/ERC721/ERC721.sol";
import "@openzeppelin/contracts/token/ERC721/extensions/ERC721URIStorage.sol";
import "@openzeppelin/contracts/token/ERC721/extensions/ERC721Enumerable.sol";
import "@openzeppelin/contracts/access/AccessControl.sol";
import "@openzeppelin/contracts/utils/ReentrancyGuard.sol";
import "@openzeppelin/contracts/utils/Base64.sol";
import "@openzeppelin/contracts/utils/Strings.sol";
import "@openzeppelin/contracts/utils/cryptography/ECDSA.sol";
import "@openzeppelin/contracts/utils/cryptography/EIP712.sol";

contract NFTmuseu is ERC721, ERC721URIStorage, ERC721Enumerable, AccessControl, ReentrancyGuard, EIP712 {
    using ECDSA for bytes32;
    uint256 private _tokenIdCounter;
    mapping(bytes32 => bool) private _usedSignatures;
    address private _authorizedSigner;
    bytes32 private constant MINT_TYPEHASH = keccak256("MintRequest(address to,string visitorName,uint256 expiration)");

    event NFTMinted(address indexed to, uint256 indexed tokenId, string tokenURI);

    constructor(address initialAdmin, address authorizedSigner) ERC721("Museu Nacional", "MNRIO") EIP712("NFTmuseu", "1") {
        _grantRole(DEFAULT_ADMIN_ROLE, initialAdmin);
        _authorizedSigner = authorizedSigner; // Definir o endereço do backend autorizado aqui
    }

    function mintNFT(address to, string memory visitorName, uint256 expiration, bytes memory signature) external nonReentrant {
        require(expiration > block.timestamp, "NFTmuseu: assinatura expirada");
        require(to == msg.sender, "NFTmuseu: endereco invalido");
        require(bytes(visitorName).length > 0 && bytes(visitorName).length < 35, "NFTmuseu: nome do visitante nao pode ser vazio e deve ter menos de 35 caracteres");
        bytes32 structHash = keccak256(abi.encode( MINT_TYPEHASH, to, keccak256(bytes(visitorName)), expiration
        ));
        require(!_usedSignatures[structHash], "NFTmuseu: assinatura ja utilizada");
        bytes32 digest = _hashTypedDataV4(structHash);
        address signer = ECDSA.recover(digest,signature);
        require(signer == _authorizedSigner, "NFTmuseu: assinatura invalida");
        require(balanceOf(to) == 0, "NFTmuseu: usuario ja possui um NFT");
        uint256 tokenId = _tokenIdCounter++;
        _safeMint(to, tokenId);
        _usedSignatures[structHash] = true;
        string memory uri = _buildURI(visitorName, to);
        _setTokenURI(tokenId, uri);
        emit NFTMinted(to, tokenId, uri);
        
    }

    function burn(uint256 tokenId) external {
        require(
            ownerOf(tokenId) == msg.sender || hasRole(DEFAULT_ADMIN_ROLE, msg.sender),
            "NFTmuseu: sem permissao para queimar"
        );
        _burn(tokenId);
    }

    function _update(address to, uint256 tokenId, address auth) internal override(ERC721, ERC721Enumerable) returns(address) {
        return super._update(to, tokenId, auth);
    }
    
    function _increaseBalance(address account, uint128 value) internal override(ERC721, ERC721Enumerable) {
        super._increaseBalance(account, value);
    }

    function tokenURI(uint256 tokenId) public view override(ERC721, ERC721URIStorage) returns (string memory) {
        return super.tokenURI(tokenId);
    }

    function supportsInterface(bytes4 interfaceId) public view override(ERC721,ERC721URIStorage, ERC721Enumerable, AccessControl) returns (bool) {
        return super.supportsInterface(interfaceId);
    }

    function _buildURI(string memory visitorName, address addr) internal pure returns (string memory) {
        return string(abi.encodePacked(
            "data:application/json;base64,",
            Base64.encode(bytes(
                abi.encodePacked(
                    '{"address": "', Strings.toHexString(uint256(uint160(addr)), 20),
                    '","name": "', visitorName,
                    '","description": "NFT representando a visita ao Museu Nacional do Rio de Janeiro criado no unity para o projeto MuseuVerse."}'
                )
            ))
        ));
    }
}