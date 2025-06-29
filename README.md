
# Arena Brasil: Batalha de Lendas ✨🇧🇷

O Battle Royale mobile que celebra a cultura brasileira, combinando gameplay imersivo com lendas do nosso folclore!

## 📋 Visão Geral

"Arena Brasil: Batalha de Lendas" é concebido como um jogo Battle Royale móvel de alto potencial, inspirado no sucesso de "Free Fire" em mercados emergentes, com foco particular no Brasil. O objetivo é desenvolver um título não apenas envolvente, mas também altamente lucrativo, capitalizando a vasta base de jogadores móveis brasileiros.

## 🏗️ Arquitetura do Sistema

### Componentes Principais

1. **Cliente de Jogo (Unity C#)**
   - Motor: Unity Engine 6.0 LTS
   - Networking: Unity Netcode for Entities (NFE)
   - Otimização: DOTS/ECS para alta performance

2. **Servidores de Jogo Dedicados (DGS)**
   - Unity Headless Build em C#
   - Hospedagem: AWS GameLift
   - Simulação autoritária de gameplay

3. **Backend Central**
   - Firebase Authentication & Firestore
   - PlayFab para economia virtual
   - Cloud Functions para lógica de negócio

## 🚀 Tecnologias Utilizadas

- **Motor de Jogo**: Unity Engine 6.0 LTS
- **Linguagem**: C#
- **Networking**: Unity Netcode for Entities (NFE)
- **Backend**: Firebase + PlayFab
- **Autenticação**: Firebase Authentication
- **Banco de Dados**: Firebase Firestore
- **Hospedagem de Servidores**: AWS GameLift
- **Analytics**: Google Analytics for Firebase

## 🎮 Heróis Lendas

- **Saci**: Teletransporte + Invisibilidade temporária
- **Curupira**: Velocidade aumentada + Rastros confusos
- **Iara (Mãe d'Água)**: Cura + Atração de inimigos
- **Boitatá**: Área de dano de fogo
- **Mula-sem-Cabeça**: Dash com dano

## 📁 Estrutura do Projeto

```
Assets/
├── 00_Core/           # Sistemas fundamentais
├── 01_Art/            # Assets visuais
├── 02_Audio/          # Música e efeitos sonoros
├── 03_Gameplay/       # Lógica central do jogo
├── 04_UI/             # Interface do usuário
├── 05_Networking/     # Scripts de rede
├── 06_AI/             # Sistemas de IA
├── 07_ThirdParty/     # SDKs externos
├── 08_Scenes/         # Cenas do Unity
├── 09_Resources/      # Recursos carregáveis
└── 10_Settings/       # Configurações globais
```

## 🔐 Estratégias de Segurança

- **Validação Server-Authoritative**: Todas as ações críticas validadas no servidor
- **Anti-Cheat Multicamadas**: Detecção client-side + validação server-side
- **Comunicação Segura**: TLS/SSL para todas as comunicações
- **Autenticação Robusta**: Firebase Auth com múltiplos provedores

## 💰 Modelo de Monetização

- **Compras In-App**: Skins, personagens, passes de batalha
- **Anúncios Recompensados**: Validação server-side
- **Sistema de Moedas Virtuais**: Economia balanceada
- **Conteúdo Cosmético**: Foco em itens visuais, não pay-to-win

## 🌟 Diferenciais Culturais

- **Folclore Brasileiro**: Personagens baseados em lendas nacionais
- **Mapas Temáticos**: Favelas, Amazônia, metrópoles brasileiras
- **Dublagem Autêntica**: Sotaques regionais brasileiros
- **Música Dinâmica**: Instrumentos e temas folclóricos

## 🚀 Como Executar

1. Clone este repositório
2. Abra no Unity 6.0 LTS ou superior
3. Configure os SDKs do Firebase e PlayFab
4. Execute o projeto

## 📊 Métricas de Sucesso

- **Retenção D1/D7/D30**: Monitoramento via Firebase Analytics
- **ARPU**: Receita por usuário através de IAPs e ads
- **Engagement**: Tempo de sessão e frequência de jogo
- **Performance**: FPS e estabilidade em dispositivos diversos

## 🤝 Contribuição

Contribuições são bem-vindas! Por favor, leia nosso guia de contribuição antes de enviar pull requests.

## 📄 Licença

Este projeto está licenciado sob a MIT License - veja o arquivo LICENSE para detalhes.

## 📞 Contato

Paulo Silas de Campos Filho - techleadevelopers@gmail.com

Link do Projeto: [Arena Brasil Repository](https://github.com/techleadevelopers/arena-brasil-batalha-de-lendas)
