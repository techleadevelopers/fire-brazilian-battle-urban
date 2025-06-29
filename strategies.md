Arena Brasil: Batalha de Lendas - Documentação do Estado Atual e Plano de Implementação Final
Este documento detalha o estado atual de desenvolvimento do projeto "Arena Brasil: Batalha de Lendas", com base nos arquivos C# fornecidos e no README.md. Ele abrange a arquitetura geral, o status de implementação de cada componente lógico, as funcionalidades já existentes e aquelas que ainda precisam ser desenvolvidas ou aprimoradas para a versão final do jogo.

I. Sumário Executivo
"Arena Brasil: Batalha de Lendas" é um Battle Royale mobile de alto potencial, inspirado no sucesso de "Free Fire" e focado no mercado brasileiro. O objetivo principal é criar um título envolvente e lucrativo, acessível em dispositivos de baixo custo, com atualizações contínuas de conteúdo e forte engajamento da comunidade. A autenticidade cultural, integrando folclore, locais e dublagem brasileiros, é um diferencial competitivo chave.

A arquitetura do jogo utiliza Unity (C#) para o cliente e servidores dedicados, com um backend híbrido combinando Firebase para serviços essenciais (autenticação, perfil) e PlayFab para economia e LiveOps. AWS GameLift gerencia os servidores de jogo dedicados para escalabilidade e baixa latência. Estratégias de segurança multicamadas, com autoridade do servidor e anti-cheat avançado, protegem a integridade do jogo.

II. Relatório do Estado Atual
O projeto "Arena Brasil: Batalha de Lendas" possui uma estrutura de código bem definida, com muitos dos sistemas centrais já esboçados e algumas funcionalidades básicas implementadas. A visão arquitetônica delineada no README.md está sendo seguida, com a separação clara entre Cliente, Servidores de Jogo Dedicados (DGS) e Backend Central.

A. Arquitetura Geral do Sistema

Cliente de Jogo (Game Client): O desenvolvimento está em Unity (C#), com classes para gerenciamento de UI, input, player, armas, áudio, otimização e sistemas de metajogo (gacha, battle pass, clãs, etc.). A maioria das interações do jogador e a lógica visual estão sendo estabelecidas.
Servidores de Jogo Dedicados (DGS): Implementados como builds headless do Unity em C#, com classes para gerenciamento de rede, combate autoritário, zona segura, loot, geração de mapas e clima. A autoridade do servidor para lógica crítica de gameplay é uma prioridade.
Backend Central (Central Backend Services): Um modelo híbrido está em uso, com FirebaseBackendService para autenticação e dados de perfil, e EconomyManager e MatchmakingService apontando para funcionalidades de economia e matchmaking via PlayFab. LiveOpsManager gerencia eventos e desafios.
B. Componentes e Estado Atual de Implementação (por arquivo C#)

A seguir, uma análise detalhada do estado de cada arquivo C# fornecido:

AchievementSystem.cs
Propósito: Gerencia as conquistas do jogo, seu progresso e recompensas.
Estado Atual:
Singleton Instance e DontDestroyOnLoad implementados.
Lista de conquistas (availableAchievements) e dicionário (achievementsDictionary) para acesso rápido.
Conquistas temáticas brasileiras pré-definidas (primeira_vitoria, saci_master, lenda_brasileira, matador_favela, explorador_amazonia, sobrevivente_sertao, capoeirista).
Método TrackProgress (Server-side) para atualizar o progresso das conquistas e verificar se foram desbloqueadas.
Método UnlockAchievement (Server-side) para registrar conquistas desbloqueadas, invocar evento e recompensar o jogador via EconomyManager.
RPC AchievementUnlockedClientRpc para notificar clientes sobre conquistas desbloqueadas.
Método ShowAchievementNotification (Client-side) para exibir notificação no Debug.Log.
Métodos GetPlayerAchievements e GetAchievementProgress para consulta.
Funcionalidades Faltantes para a Versão Final:
Integração real de TrackProgress com eventos de gameplay (ex: OnKill, OnMatchEnd, OnHeroUsed). Atualmente, o progress é passado manualmente.
Persistência dos dados de progresso e conquistas desbloqueadas dos jogadores (provavelmente via Firebase Firestore ou PlayFab).
UI de notificação de conquista (ShowAchievementNotification) precisa ser implementada no UIManager.
Mecanismo para redefinir o progresso de conquistas entre temporadas, se aplicável.
Validação de targetValue para conquistas que dependem de múltiplas condições (ex: lenda_brasileira que exige jogar com todos os heróis folclóricos).
AntiCheatSystem.cs
Propósito: Implementa medidas anti-cheat no lado do cliente e do servidor.
Estado Atual:
Singleton Instance e DontDestroyOnLoad implementados.
Configurações para detecção de speed hacks, teleport hacks, aimbot, wall hacks e injeção de processos.
RegisterPlayer para inicializar dados de segurança por jogador.
UpdatePlayerPosition para verificar speed/teleport hacks com base na posição e velocidade.
CheckAimbotSuspicion e CheckWallHackSuspicion para análise de padrões de comportamento.
CheckForSuspiciousProcesses para detectar programas de cheat conhecidos (lista de processos suspeitos hardcoded).
PerformSecurityChecks para verificações de integridade de memória, manipulação de tempo e attachment de debugger.
ReportViolation e ReportSecurityThreat para registrar violações e tomar ações (kick).
Métodos auxiliares para cálculo de média de precisão/tempo de reação e hash de versão.
Funcionalidades Faltantes para a Versão Final:
Integração real com o NetworkManager para KickPlayer.
Implementação robusta de detecção de aimbot/wallhack (atualmente baseada em heurísticas simples).
Lista de processos suspeitos e palavras banidas precisa ser dinâmica e atualizável (via backend).
Integração com soluções anti-cheat de terceiros (EasyAntiCheat, BattlEye) conforme mencionado no README.md.
Mecanismo para persistir e analisar relatórios de violação no backend para banimentos.
UI de mensagem de kick (ShowKickMessage) precisa ser implementada no UIManager.
ArenaBrasilGameManager.cs
Propósito: Gerencia o estado geral da partida (lobby, início, em progresso, fim).
Estado Atual:
Singleton Instance e DontDestroyOnLoad implementados.
OnNetworkSpawn para inicialização da partida no servidor.
Configurações para início automático, delay, mínimo de jogadores.
Estados da partida (MatchState) e transição de estados (ChangeMatchState).
StartMatchSequence e corrotina StartCountdown para o início da partida.
Inicialização de sistemas de jogo (InitializeGameSystems) como SafeZone, LootSystem e CombatSystem.
Música brasileira e frases motivacionais.
Assinatura de eventos do CombatSystem (OnPlayerEliminated, OnPlayersAliveChanged).
EndMatch para finalizar a partida, encontrar o vencedor e processar resultados.
ProcessMatchResults para calcular recompensas e concedê-las via EconomyManager.
RPCs para anúncios gerais (AnnounceToAllClientsRpc) e mudança de estado.
Update para controle do timer da partida.
Funcionalidades Faltantes para a Versão Final:
GetCurrentMatchId() e GetLocalPlayerName() no ReplaySystem (e potencialmente aqui) precisam de uma implementação real que se conecte ao sistema de ID de partida e dados do jogador.
CalculatePlacement é um placeholder, precisa de lógica real baseada na ordem de eliminação.
Integração com MapManager para carregar o mapa aleatório.
Lógica para lidar com jogadores que se conectam/desconectam durante a partida.
UI de resultados da partida (ShowMatchResults) precisa ser implementada no UIManager.
AudioManager.cs
Propósito: Gerencia todos os sons e músicas do jogo.
Estado Atual:
Singleton Instance e DontDestroyOnLoad implementados.
Múltiplos AudioSource para música, SFX, voz e ambiente.
Arrays de AudioClip para música de menu, combate (baixa/alta intensidade), vitória.
Arrays de AudioClip para linhas de voz de heróis brasileiros (Saci, Curupira, IaraMae, Boitata, MataCavalos).
SFX de combate e sons ambientes (selva, cidade, favela).
Controles de volume mestre, música, SFX e voz.
Métodos para tocar música de menu, combate, vitória, linhas de voz de heróis, sons de arma/impacto/recarregamento e sons ambientes.
UpdateVolumes para aplicar configurações de volume.
InitializeAudio para configurar dicionário de vozes e volumes.
Funcionalidades Faltantes para a Versão Final:
Integração EnableStreamingMode() para usar música sem direitos autorais durante o streaming (mencionado no StreamingSystem).
Sistema de mixagem de áudio mais avançado (Audio Mixer Groups) para controle granular de volume e efeitos.
Lógica para transições de música mais suaves (fade-in/out).
Integração com o sistema de mapa para tocar sons ambientes específicos do tema do mapa.
Implementação de PlayHeroVoiceLine para tipos específicos de falas (habilidade, morte, vitória).
BattlePassSystem.cs
Propósito: Gerencia o sistema de Passe de Batalha, progressão e recompensas.
Estado Atual:
Singleton Instance e DontDestroyOnLoad implementados.
Configurações de maxTier, xpPerTier, premiumCost.
Criação de temporada atual (currentSeason) com recompensas gratuitas e premium hardcoded.
Métodos CreateFreeRewards e CreatePremiumRewards com lógica básica de distribuição de recompensas (moedas, XP, personagens, skins, armas, cosméticos).
AddBattlePassXP (Server-side) para adicionar XP e verificar desbloqueio de tiers.
UnlockTier para auto-reivindicar recompensas gratuitas.
PurchasePremiumPass (Server-side) para ativar o passe premium e reivindicar recompensas premium retroativamente.
ClaimReward para conceder recompensas via EconomyManager ou adicionar ao inventário (placeholder).
RPCs BattlePassProgressClientRpc, PremiumPassPurchasedClientRpc, RewardClaimedClientRpc para notificar o cliente.
Métodos GetPlayerData, GetPlayerTier, HasPremiumPass para consulta.
Funcionalidades Faltantes para a Versão Final:
Persistência dos dados do jogador no passe de batalha (PlayerBattlePassData) via backend (Firebase/PlayFab).
Integração real com o sistema de inventário para AddToPlayerInventory.
Integração com o EconomyManager para deduzir o custo da compra do passe premium.
Recompensas hardcoded precisam ser configuráveis via backend (PlayFab Catalog/Economy).
UI de progressão do passe de batalha e reivindicação de recompensas precisa ser implementada no UIManager.
Lógica para lidar com o fim da temporada e início de uma nova (reset de progresso, distribuição de recompensas de fim de temporada).
BrazilianMapGenerator.cs
Propósito: Gera mapas temáticos brasileiros proceduralmente.
Estado Atual:
Singleton Instance e DontDestroyOnLoad implementados.
OnNetworkSpawn para iniciar a geração do mapa no servidor.
Configurações de assets para diferentes temas (favela, amazônia, metrópole, praia, cerrado).
Elementos culturais (arte de rua, bandeiras, campos de futebol, palcos de música).
GenerateMap para iniciar o processo de geração baseado no tema atual.
Métodos GenerateFavelaMap, GenerateAmazonMap, GenerateCityMap, GenerateBeachMap, GenerateCerradoMap com lógica básica de spawn de objetos e cores.
AddCulturalElements para adicionar grafites, bandeiras e campos de futebol.
GenerateStreetNames (placeholder).
Métodos auxiliares para posições e rotações aleatórias, e cores brasileiras.
ClearExistingMap para limpar objetos gerados.
RPC NotifyMapGeneratedClientRpc para notificar clientes.
ChangeMapTheme para alternar temas de mapa.
Funcionalidades Faltantes para a Versão Final:
A lógica de geração é muito básica; precisa de algoritmos mais complexos para criar mapas jogáveis e variados (geração de terreno, caminhos, edifícios funcionais).
AddFavelaElements, AddAmazonElements, AddCityElements, AddBeachElements, AddCerradoElements são placeholders.
GenerateStreetNames precisa de implementação real e integração com o mapa.
O lootBoxPrefab não está sendo usado diretamente aqui para spawnar loot, isso é feito pelo LootSystem.
Integração visual no UIManager para mostrar o mapa gerado.
O MapManager já possui uma lista de BrazilianMapData com cenas pré-definidas. O BrazilianMapGenerator parece ser para geração procedural, o que pode ser uma alternativa ou um complemento. É preciso definir se os mapas serão pré-construídos ou gerados proceduralmente.
ChatSystem.cs
Propósito: Gerencia a comunicação de chat entre os jogadores.
Estado Atual:
Singleton Instance e DontDestroyOnLoad implementados.
Configurações para comprimento máximo de mensagem, histórico, cooldown e filtro de profanidade.
Recursos de chat temáticos brasileiros (saudações, boa sorte).
SendMessage (Server-side) para processar mensagens, aplicar cooldown, filtrar profanidade e adicionar ao histórico.
FilterProfanity com lista hardcoded de palavras banidas.
DetectMessageType para classificar mensagens (saudação, encorajamento, etc.).
BroadcastMessage para rotear mensagens para todos, equipe ou clã.
SendQuickMessage para mensagens rápidas pré-definidas.
MutePlayer (Server-side) para silenciar jogadores.
RPCs para mensagens recebidas (MessageReceivedClientRpc), rejeitadas (MessageRejectedClientRpc) e jogadores mutados (PlayerMutedClientRpc).
Métodos auxiliares para obter nome do jogador e membros da equipe/clã (placeholders).
DisplayMessage para exibir no Debug.Log.
Funcionalidades Faltantes para a Versão Final:
Lista de palavras banidas precisa ser carregada dinamicamente (de um arquivo ou backend).
Integração real com o sistema de equipe/esquadrão para GetTeamMembers.
Integração real com o sistema de dados do jogador para GetPlayerName.
Implementação de chat privado.
UI de chat (DisplayMessage) precisa ser implementada no UIManager.
Persistência do histórico de chat para fins de moderação/auditoria.
ClanSystem.cs
Propósito: Gerencia o sistema de clãs do jogo.
Estado Atual:
Singleton Instance e DontDestroyOnLoad implementados.
Configurações para máximo de membros, custo de criação, comprimento de nome.
Clãs padrão temáticos brasileiros (Guerreiros da Amazônia, Cangaceiros do Sertão, Leões de Copacabana).
CreateClan (Server-side) para criar um clã, com validações de nome/tag/custo e adição do líder.
JoinClan (Server-side) para adicionar um jogador a um clã, com validações de lotação e pertencimento.
LeaveClan (Server-side) para remover um jogador, com lógica de promoção de novo líder ou dissolução do clã.
PromoteMember (Server-side) para mudar o papel de um membro.
AddClanExperience para subir de nível o clã e conceder recompensas de nível.
NotifyClanMembers para enviar mensagens a todos os membros do clã.
RPCs para sucesso/falha de criação/entrada/saída e promoção de membro.
Métodos GetTopClans e SearchClans para consulta.
Funcionalidades Faltantes para a Versão Final:
Persistência dos dados dos clãs e membros via backend (Firebase/PlayFab).
Integração real com o EconomyManager para o custo de criação do clã e recompensas.
GetPlayerName é um placeholder.
Sistema de convites/aplicações para clãs.
UI de gerenciamento de clãs, lista de membros, chat do clã, etc.
Mecanismo para emblemas de clãs (atualmente apenas string emblem).
CombatSystem.cs
Propósito: Gerencia a lógica de combate, dano e eliminações.
Estado Atual:
Singleton Instance e DontDestroyOnLoad implementados.
OnNetworkSpawn para inicializar contagem de jogadores vivos no servidor.
Configurações de camadas de combate, alcance, multiplicadores de dano (cabeça, corpo, membro).
ProcessDamageServerRpc (Server-side) para detecção autoritária de acertos via Raycast.
CalculateDamage para aplicar multiplicadores de dano.
ApplyDamage para reduzir a saúde do jogador e verificar eliminação.
EliminatePlayer para registrar eliminação, reduzir contagem de jogadores vivos, conceder kill ao atacante e verificar fim da partida.
RPCs NotifyHitClientRpc para feedback visual de acerto e AnnounceEliminationClientRpc para anunciar eliminações.
TrackCombatSession para registrar dados de combate (dano total, hits).
GetPlayerById e GetPlayerNameById para obter dados do jogador (placeholder para nome).
EndMatch para finalizar a partida e anunciar o vencedor.
RPC AnnounceWinnerClientRpc para anunciar o vencedor.
Métodos para mostrar indicadores de dano, hit marker, feed de eliminação e UI de jogadores vivos (placeholders).
Funcionalidades Faltantes para a Versão Final:
Integração com o PlayerController para TakeDamage, AddKill, AddDamage.
Integração com o UIManager para ShowDamageIndicator, ShowHitMarker, UpdateEliminationFeed, UpdatePlayersAliveUI, ShowVictoryScreen, ShowDefeatScreen.
O GetPlayerNameById é um placeholder, precisa de integração com o sistema de perfil do jogador.
Implementação de CombatData para rastrear estatísticas de combate por jogador.
Lógica para lidar com diferentes tipos de dano (fogo, veneno, etc.).
Integração com o AntiCheatSystem para reportar anomalias de dano.
EconomyManager.cs
Propósito: Gerencia a economia virtual do jogo (moedas, gemas, XP, loja, recompensas).
Estado Atual:
Singleton Instance e DontDestroyOnLoad implementados.
Configurações de moedas iniciais, XP/moedas por partida, bônus de vitória/kill.
Lista de itens da loja (shopItems) com itens temáticos brasileiros hardcoded.
InitializeShopItems para configurar itens da loja.
LoadPlayerCurrencies e SavePlayerCurrencies para carregar/salvar dados de moeda e XP usando PlayFab (via GetPlayerStatisticsRequest, UpdatePlayerStatisticsRequest).
PurchaseItem para comprar itens da loja usando PlayFab (PurchaseItemRequest).
GrantMatchRewards para conceder XP e moedas após a partida, incluindo bônus de vitória/kill/tempo de sobrevivência e verificação de level up.
CalculateLevel e GetXPRequiredForNextLevel para progressão de nível.
AddCurrency para adicionar moedas/gemas.
HasEnoughCurrency para verificar saldo.
Eventos para atualização de moeda/XP, compra de item, falha de compra, concessão de recompensa.
Método auxiliar ExecutePlayFabRequest para chamadas assíncronas ao PlayFab.
Funcionalidades Faltantes para a Versão Final:
A integração com PlayFab para GetPlayerData e SpendCoins (usados no ClanSystem) e AddCoins, AddExperience, GrantItem (usados no LiveOpsManager, RankingSystem, BattlePassSystem) precisa ser consistente. Atualmente, AddCoins e AddCurrency (que chama SavePlayerCurrencies) são os métodos públicos para adicionar moedas. É necessário garantir que todos os sistemas utilizem os métodos corretos para interagir com a economia.
O GetPlayerData(leaderId) e SpendCoins(team.captainId, fee) no ClanSystem não existem no EconomyManager fornecido.
O AddCoins(playerId, coinReward) e AddExperience(playerId, xpReward) no RankingSystem não existem no EconomyManager fornecido.
O AddCoins(playerId, reward.quantity) e AddPremiumCoins(playerId, reward.quantity) no BattlePassSystem não existem no EconomyManager fornecido.
O AddCoins(challenge.reward.coins), AddXP(challenge.reward.xp) e GrantItem(challenge.reward.itemId) no LiveOpsManager não existem no EconomyManager fornecido.
O AddCoins(prizePerPlayer) e GrantItem(reward) no EsportsManager não existem no EconomyManager fornecido.
Tratamento de erros e feedback para o usuário em caso de falha na compra ou na concessão de recompensas.
UI da loja e inventário precisa ser implementada no UIManager.
Gerenciamento de inventário de itens (além de moedas).
Integração com o sistema de anúncios recompensados.
O PlayerProfile do FirebaseBackendService também tem campos de moeda/XP. É preciso definir qual sistema é a fonte autoritária desses dados (PlayFab ou Firestore). A descrição do README.md sugere PlayFab para economia, o que é consistente com a implementação aqui.
EsportsManager.cs
Propósito: Gerencia torneios, equipes e o sistema de e-sports.
Estado Atual:
Singleton Instance e DontDestroyOnLoad implementados.
Configurações de torneios ativos, equipes registradas, prêmios.
Dados de campeonatos brasileiros (lbaf, regionalChampionships) hardcoded.
InitializeEsportsSystem para criar torneios sazonais e configurar streaming/ranking.
CreateWeeklyTournament, CreateMonthlyChampionship, CreateSpecialEventTournaments (Carnaval, Independência) com dados hardcoded.
RegisterTeam para registrar equipes em torneios, com validações de período, lotação, elegibilidade e taxa de inscrição.
CheckTeamEligibility e ProcessEntryFee (placeholder para economia).
SetupRankingSystem (invoca UpdateRankings repetidamente).
CalculateTeamRating (lógica básica de ELO).
InitializeStreamingIntegration, SetupTwitchIntegration, SetupYouTubeIntegration (placeholders).
StartTournamentServerRpc (Server-side) para iniciar torneios e gerar chaves.
GenerateTournamentBracket com métodos para chaves de eliminação, suíço e liga (placeholders para os dois últimos).
CompleteMatch para registrar resultados de partidas.
AdvanceTournament, IsTournamentComplete, CompleteTournament para progressão do torneio e distribuição de prêmios.
DistributePrizes (placeholder para economia e concessão de itens).
Eventos para início/fim de torneio, registro de equipe, partida completa, campeão coroado.
Funcionalidades Faltantes para a Versão Final:
Persistência dos dados de torneios, equipes e jogadores via backend.
ProcessEntryFee e DistributePrizes precisam de integração real com o EconomyManager e o sistema de inventário.
SetupTwitchIntegration e SetupYouTubeIntegration precisam de implementação real com as APIs de streaming.
GenerateSwissBracket e GenerateLeagueBracket são placeholders.
UI de torneios, registro de equipes, chaves, placares, etc.
Sistema de convite/gerenciamento de equipe mais robusto.
Lógica para lidar com desqualificações, desistências.
Integração com o sistema de matchmaking para criar partidas de torneio.
FirebaseBackendService.cs
Propósito: Fornece integração com os serviços Firebase (Autenticação, Firestore, Functions).
Estado Atual:
Singleton Instance e DontDestroyOnLoad implementados.
initializeOnStart para inicializar Firebase no início.
InitializeFirebase para verificar dependências e configurar FirebaseAuth, FirebaseFirestore, FirebaseFunctions.
SignInWithEmailAndPassword, CreateUserWithEmailAndPassword, SignOut para autenticação.
CreatePlayerProfile para criar um perfil de jogador inicial no Firestore.
GetPlayerProfile para buscar perfil do jogador.
UpdatePlayerStats para atualizar estatísticas de jogador (XP, moedas, partidas, vitórias, kills, nível) usando transações Firestore.
PurchaseItem (placeholder) para chamar uma Cloud Function de compra.
GetLeaderboard para buscar dados de leaderboard do Firestore.
CalculateLevel para determinar o nível do jogador.
OnAuthStateChanged para lidar com mudanças de estado de autenticação.
Eventos para inicialização do Firebase, login/logout de usuário.
Funcionalidades Faltantes para a Versão Final:
A classe PlayerProfile hardcoded aqui pode precisar ser sincronizada ou consolidada com os dados de jogador gerenciados pelo PlayFab (EconomyManager, MatchmakingService). O README.md sugere Firestore para perfil/inventário e PlayFab para economia/LiveOps, o que implica que PlayerProfile no Firestore seria a fonte primária de dados de perfil, enquanto o PlayFab gerencia moedas e itens virtuais.
A implementação de PurchaseItem é um placeholder que chama uma Cloud Function genérica. A lógica real da Cloud Function precisa ser desenvolvida.
Integração com o GameManager para LoadPlayerData.
Tratamento de erros mais robusto e feedback para o usuário.
Configuração de regras de segurança no Firestore para proteger os dados do jogador.
GachaSystem.cs
Propósito: Gerencia o sistema de Gacha (caixas de loot).
Estado Atual:
Singleton Instance e DontDestroyOnLoad implementados.
Configurações de caixas de gacha (gachaBoxes) e todos os itens (allItems).
Taxas de raridade (lendário, épico, raro, comum).
Sistema de "pity" garantido para itens lendários e épicos.
InitializeGachaSystem para criar caixas e configurar itens brasileiros hardcoded.
CreateGachaBoxes e SetupBrazilianItems para definir caixas e itens.
OpenGachaBoxServerRpc (Server-side) para abrir caixas, verificar custo, inicializar dados do jogador, rolar itens, atualizar contadores de pity e deduzir moeda via EconomyManager.
RollGachaItem para determinar o item com base nas taxas e pity.
GetRandomItemByRarity para selecionar um item aleatório de uma raridade.
UpdatePityCounters para atualizar o progresso do pity.
CanAffordGacha e DeductGachaCurrency para integração com EconomyManager.
RPCs NotifyLegendaryDropClientRpc (para notificação global) e GachaResultClientRpc (para resultados do jogador).
Métodos ShowLegendaryAnimation e ShowGachaResults (placeholders).
Métodos GetPlayerGachaData e GetAvailableBoxes para consulta.
Funcionalidades Faltantes para a Versão Final:
Persistência dos dados de gacha do jogador (PlayerGachaData) via backend (Firebase/PlayFab).
Integração real com o EconomyManager para deduzir o custo.
Integração com o sistema de inventário para adicionar itens obtidos.
Itens e caixas hardcoded precisam ser configuráveis via backend (PlayFab Catalog/Economy).
UI de abertura de caixa, animação de lendário e exibição de resultados precisa ser implementada no UIManager.
Lógica para o specialItems na GachaBox.
GameFlowManager.cs
Propósito: Gerencia o fluxo de estados do jogo (menu, lobby, matchmaking, partida, resultados).
Estado Atual:
Singleton Instance e DontDestroyOnLoad implementados.
Estados de jogo (GameState).
ChangeGameState para transição entre estados, invocando eventos e chamando métodos EnterState/ExitState.
EnterMainMenu, EnterLobby, EnterMatchmaking, EnterMatch, EnterResults para lógica específica de cada estado.
Integração com UIManager para mostrar telas.
Integração com AudioManager para tocar música.
Integração com NetworkManager.Singleton para desconectar.
Integração com FirebaseBackendService para login.
Integração com MatchmakingService para iniciar/cancelar matchmaking.
Métodos públicos para iniciar matchmaking, lidar com match encontrado/finalizado, sair para o menu principal e sair do jogo.
Funcionalidades Faltantes para a Versão Final:
O arquivo GameFlowManager.cs aparece duas vezes com implementações ligeiramente diferentes (uma mais simples de carregamento de cena, outra mais complexa com estados de matchmaking e integração com outros sistemas). A versão mais completa é a que será considerada para as funcionalidades faltantes.
Integração com UIManager para ShowScreen para todas as telas.
Lógica para lidar com o login do jogador no lobby (atualmente apenas Debug.Log("Player needs to sign in")).
Lógica de carregamento de cena real para cada estado (atualmente, o GameManager.cs original tinha isso, mas esta versão do GameFlowManager foca mais na lógica de estado). É preciso definir qual GameFlowManager é o principal. Considerando a segunda versão, ela não tem o carregamento de cena explícito, o que é um ponto a ser resolvido.
Invoke(nameof(ReturnToLobby), 10f) no EnterResults para auto-retorno ao lobby.
GameManager.cs
Propósito: Gerenciador principal do jogo, responsável pela inicialização de sistemas e controle do ciclo de vida.
Estado Atual:
Singleton Instance e DontDestroyOnLoad implementados.
Configurações de jogo (frame rate, duração da partida, max jogadores).
Inicialização de sistemas core (GameFlowManager, FirebaseBackendService, NetworkManagerClient).
SubscribeToEvents para eventos do backend e fluxo de jogo.
StartGameInitialization para iniciar o processo de inicialização (Firebase primeiro).
OnFirebaseInitialized, OnUserSignedIn, OnUserSignedOut para lidar com eventos do Firebase.
LoadPlayerData (assíncrono) para carregar perfil do jogador.
OnGameStateChanged para reagir a mudanças de estado do jogo (iniciar/finalizar partida).
StartMatch, EndMatch, UpdateMatchTimer para gerenciar o ciclo da partida.
QuitGame, RestartGame para controle da aplicação.
FormatTime, SetTargetFrameRate como utilitários.
Eventos para inicialização do jogo, início/fim de partida, atualização de tempo.
Funcionalidades Faltantes para a Versão Final:
O GameManager parece ser a versão mais antiga do gerenciador de jogo, enquanto ArenaBrasilGameManager.cs é a versão mais recente e específica para o Battle Royale. É crucial consolidar ou definir qual é o gerenciador principal. Se ArenaBrasilGameManager for o principal, muitas das lógicas aqui (match duration, start/end match) seriam movidas para lá.
LoadPlayerData é um placeholder que apenas loga o perfil. A lógica real de carregamento de dados do jogador precisa ser implementada.
Integração com o UIManager para exibir telas de loading/main menu/gameplay.
A NetworkManagerClient está sendo instanciada aqui, mas também é um singleton próprio. É preciso garantir que a inicialização seja feita uma única vez e de forma consistente.
GachaSystem.cs
Propósito: Gerencia o sistema de Gacha (caixas de loot).
Estado Atual: (Já detalhado acima, mas reitero as principais lacunas)
Funcionalidades Faltantes para a Versão Final:
Persistência dos dados de gacha do jogador (PlayerGachaData) via backend (Firebase/PlayFab).
Integração real com o EconomyManager para deduzir o custo.
Integração com o sistema de inventário para adicionar itens obtidos.
Itens e caixas hardcoded precisam ser configuráveis via backend (PlayFab Catalog/Economy).
UI de abertura de caixa, animação de lendário e exibição de resultados precisa ser implementada no UIManager.
Lógica para o specialItems na GachaBox.
HeroLenda.cs
Propósito: ScriptableObject que define os dados e habilidades dos heróis lendários.
Estado Atual:
ScriptableObject para criar ativos de heróis.
Campos para informações do herói (nome, tipo, descrição, stats, cooldowns, áudio/visual).
CanUseAbility para verificar se a habilidade está pronta.
UseAbility com um switch para chamar métodos de habilidade específicos de cada herói (Saci, Curupira, Iara, Boitata, MataCavalos).
Implementações básicas das habilidades (teletransporte/invisibilidade para Saci, velocidade para Curupira, área de dano para Boitata, dash para MataCavalos).
Corrotinas para ApplyInvisibility e ApplySpeedBoost.
CreateFireArea para efeito visual.
PlayAbilityEffects para tocar sons de habilidade e voz.
Funcionalidades Faltantes para a Versão Final:
As habilidades são implementações muito básicas e precisam ser expandidas para efeitos de gameplay reais (ex: player.Heal(30f) para Iara é um comentário, Atrair inimigos próximos é um placeholder).
Integração com sistemas de status (invisibilidade, buff de velocidade, debuffs).
Integração com o CombatSystem para dano de área.
Efeitos visuais (abilityEffect) precisam ser criados e configurados.
PlayerController precisa de uma referência mais robusta para HeroLenda e a lógica de aplicação de stats do herói.
InfluencerSystem.cs
Propósito: Gerencia parcerias com influenciadores e o programa de criadores.
Estado Atual:
Singleton Instance e DontDestroyOnLoad implementados.
Listas de influenciadores parceiros (partneredInfluencers) e colaborações de celebridades (activeCelebCollabs).
Configurações do programa de criadores (creatorProgram).
InitializeInfluencerSystem para configurar influenciadores e colaborações hardcoded.
SetupBrazilianInfluencers e SetupCelebrityCollaborations com dados de Nobru, LOUD, Gaules, Anitta, Neymar Jr.
InitializeCreatorProgram com requisitos e bônus hardcoded.
UseCreatorCodeServerRpc (Server-side) para aplicar benefícios de código de criador, rastrear métricas e invocar eventos.
ApplyCreatorCodeBenefits para conceder moedas/gemas via EconomyManager e conteúdo exclusivo (placeholder).
GrantExclusiveContent (placeholder para inventário).
TrackCreatorCodeUsage para registrar uso e calcular comissão.
CalculateInfluencerCommission (cálculo simples).
RPC CreatorCodeUsedClientRpc para notificação ao cliente.
ShowCreatorCodeReward (placeholder para UI).
StartInfluencerEvent para criar e anunciar eventos de influenciadores.
RPC StartInfluencerEventClientRpc para notificação ao cliente.
ShowInfluencerEventNotification (placeholder para UI).
ValidateCreatorCode e GetTopInfluencers para consulta.
Funcionalidades Faltantes para a Versão Final:
Persistência dos dados de métricas de influenciadores (influencerMetrics) via backend.
Integração real com o EconomyManager para conceder moedas/gemas e o sistema de inventário para GrantExclusiveContent.
Dados de influenciadores e celebridades precisam ser configuráveis via backend.
UI para entrada de código de criador e exibição de recompensas/notificações de eventos.
Lógica para o exclusiveContent (como skins, emotes, mapas) ser realmente concedido e acessível.
O EconomyManager.Instance.AddCurrency não aceita playerId como parâmetro, precisa ser ajustado ou o EconomyManager precisa gerenciar o saldo por jogador.
LiveOpsManager.cs
Propósito: Gerencia eventos ao vivo e desafios diários/semanais.
Estado Atual:
Singleton Instance e DontDestroyOnLoad implementados.
Listas de eventos ativos, desafios diários/semanais.
Dados de eventos sazonais brasileiros hardcoded (Carnaval das Lendas, Festa Junina Arena, Independência das Lendas).
InitializeLiveOps para carregar eventos ativos e gerar desafios.
SetupBrazilianEvents para definir eventos brasileiros.
LoadActiveEvents (simulado, deveria vir do backend).
GenerateDailyChallenges e GenerateWeeklyChallenges com descrições e recompensas aleatórias hardcoded.
UpdateActiveEvents para iniciar/finalizar eventos com base no tempo.
CheckEventSchedule para criar eventos sazonais e de fim de semana.
StartEvent e EndEvent para gerenciar o ciclo de vida dos eventos.
SendEventNotification (placeholder para push notification).
UpdateChallenges para remover desafios expirados e gerar novos.
CheckForContentUpdates e CheckContentVersion (simulado).
CompleteChallenge para atualizar progresso e completar desafios.
CompleteDailyChallenge e CompleteWeeklyChallenge para conceder recompensas via EconomyManager.
Métodos auxiliares para itens de recompensa aleatórios.
GetActiveMultiplier, GetActiveEvents, GetDailyChallenges, GetWeeklyChallenges para consulta.
Funcionalidades Faltantes para a Versão Final:
Persistência dos dados de desafios e progresso do jogador via backend (Firebase/PlayFab).
Eventos e desafios precisam ser configuráveis e gerenciáveis via backend (PlayFab LiveOps/Economy).
EconomyManager.Instance.AddCoins(challenge.reward.coins), AddXP(challenge.reward.xp), GrantItem(challenge.reward.itemId) não existem no EconomyManager fornecido.
Integração real com o sistema de push notification (FCM).
Integração real com o sistema de inventário para conceder itemId.
A verificação de atualização de conteúdo é simulada, precisa de um endpoint real.
UI de eventos e desafios precisa ser implementada no UIManager.
LootPickup.cs
Propósito: Componente para itens de loot no mundo que podem ser coletados.
Estado Atual:
Campos para lootId, itemId, itemName, rarity.
Componentes visuais (renderer, efeito de raridade) e áudio de pickup.
Initialize para configurar os dados do loot.
SetupVisuals para definir cor do item e efeito de raridade.
OnTriggerEnter para detectar colisão com o player.
PickupItem para desativar pickup, tocar som e notificar o LootSystem (Server-side).
ShowPickupNotification (placeholder para UI).
Animação de flutuação e rotação.
Funcionalidades Faltantes para a Versão Final:
UI de notificação de pickup precisa ser implementada no UIManager.
O player.IsOwner é usado para verificar se o player local está pegando o item, mas a notificação PickupLootServerRpc é chamada com player.OwnerClientId, o que é correto.
O LootSystem.Instance.PickupLootServerRpc é o ponto de integração.
LootSystem.cs
Propósito: Gerencia a geração e o gerenciamento de itens de loot no mapa.
Estado Atual:
Singleton Instance e DontDestroyOnLoad implementados.
Listas de tabelas de loot (lootTables) e pontos de spawn (spawnPoints).
Prefab de caixa de loot.
Configurações de máximo de itens, respawn delay, despawn time.
InitializeLootTables para criar tabelas de loot temáticas brasileiras hardcoded.
OnNetworkSpawn para chamar SpawnInitialLoot no servidor.
SpawnInitialLoot para spawnar loot em todos os pontos de spawn.
GenerateSpawnPoints (simulado, gera pontos aleatórios e determina tabela de loot).
DetermineLootTable (lógica simples baseada na distância do centro).
SpawnLootAtPoint para spawnar um item de uma tabela específica.
SelectRandomItem para selecionar um item com base na chance de drop.
SpawnLootItem (Server-side) que chama SpawnLootItemClientRpc.
RPC SpawnLootItemClientRpc para instanciar o prefab do loot no cliente e inicializar LootPickup.
PickupLootServerRpc (Server-side) para processar a coleta do item, notificar o cliente e remover o item.
NotifyLootPickupClientRpc para remover o item visualmente do cliente.
RemoveLootItem e DespawnLoot para gerenciar itens.
GetPlayerById para obter referência do player.
SpawnCarePackage (placeholder).
Funcionalidades Faltantes para a Versão Final:
Os pontos de spawn de loot precisam ser configurados manualmente ou gerados de forma mais inteligente com base no layout do mapa.
As tabelas de loot hardcoded precisam ser configuráveis via backend (PlayFab Catalog/Economy).
SpawnCarePackage precisa ser integrado com eventos de gameplay (ex: zona segura, tempo de partida).
LootPickup precisa de integração com o sistema de inventário do jogador para adicionar o item real.
O lootBoxPrefab precisa ser um prefab real que represente o item no mundo.
LowEndOptimization.cs
Propósito: Otimiza o jogo para diferentes níveis de desempenho de dispositivos, com foco no mercado brasileiro.
Estado Atual:
Singleton Instance e DontDestroyOnLoad implementados.
Detecção automática de nível de desempenho (DevicePerformanceLevel).
Presets de desempenho (Low-End, Mid-Range, High-End) com configurações hardcoded para renderização, sombras, texturas, AA, partículas, jogadores, áudio, FPS, VSync, LOD.
SetupPerformancePresets para definir os presets.
DetectDevicePerformance para classificar o dispositivo com base em RAM, GPU e CPU (incluindo GPUs comuns no Brasil).
ApplyPerformancePreset para aplicar as configurações gráficas, de áudio e de gameplay.
OptimizeForBrazilianNetworks para ajustar a taxa de tick da rede e fixedDeltaTime para dispositivos low-end.
StartDynamicOptimization para monitorar FPS e ajustar a qualidade dinamicamente.
MonitorPerformance, AdjustQualityDynamically, ReduceQuality, IncreaseQuality para otimização em tempo real.
ReduceParticleEffects para otimização de partículas.
PerformanceCheck para coleta de lixo.
InitializeBrazilianOptimizations para streaming de texturas e otimização de aspect ratios.
Funcionalidades Faltantes para a Versão Final:
Os presets de desempenho precisam ser configuráveis e talvez carregáveis de um backend.
A detecção de GPU pode ser expandida com uma lista mais abrangente de GPUs populares no Brasil.
A otimização de rede (OptimizeForBrazilianNetworks) é um bom começo, mas pode ser aprimorada com mais testes em condições de rede reais.
Ajustes de maxPlayers no ArenaBrasilGameManager.Instance são apenas um Debug.Log, precisam ser implementados.
Integração com o UIManager para permitir que o usuário ajuste manualmente o nível de desempenho.
Testes extensivos em uma ampla gama de dispositivos Android.
Main.cs
Propósito: Um arquivo C# simples, comum em projetos de console, que contém o ponto de entrada principal de um programa.
Estado Atual:
Contém apenas um método Main que imprime "hello world" no console.
Funcionalidades Faltantes para a Versão Final:
Este arquivo não é relevante para um projeto Unity. Em Unity, o ponto de entrada é o ciclo de vida dos MonoBehaviour e ScriptableObject. Este arquivo provavelmente é um resquício ou um exemplo de código que não faz parte da lógica do jogo. Deve ser removido ou ignorado no contexto do projeto Unity.
MapManager.cs
Propósito: Gerencia o carregamento de mapas e as fases da zona segura.
Estado Atual:
Singleton Instance e DontDestroyOnLoad implementados.
Lista de mapas brasileiros disponíveis (availableMaps) com dados hardcoded (Favela, Amazônia, Metrópole, Sertão, Pantanal).
InitializeBrazilianMaps para definir os mapas.
LoadRandomMap e LoadMap para carregar cenas de mapa.
InitializeMapSettings para configurar o centro da zona segura e o clima inicial.
GetRandomSafeZoneCenter com lógica básica por tema cultural.
StartSafeZone para iniciar as fases da zona segura.
UpdateSafeZone e AdvanceSafeZonePhase para gerenciar o encolhimento da zona.
ChangeWeather e ApplyWeatherEffects para aplicar efeitos visuais de clima (densidade de nevoeiro).
GetDistanceToSafeZone, GetCurrentSafeZoneRadius, IsPositionInSafeZone para consultas.
GetMapByName, GetMapsByTheme para consulta de dados de mapa.
Eventos para mapa carregado, fase da zona segura alterada, zona atualizada e clima alterado.
Funcionalidades Faltantes para a Versão Final:
A lógica da zona segura aqui é uma versão simplificada. O SafeZoneController.cs é o sistema mais completo e autoritário para a zona segura. É preciso consolidar ou definir qual é o principal. O ArenaBrasilGameManager já inicializa o SafeZoneController. Portanto, a lógica de zona segura do MapManager deve ser removida ou simplificada para apenas carregar o mapa e talvez passar as configurações iniciais para o SafeZoneController.
Os dados dos mapas hardcoded precisam ser configuráveis via backend.
ApplyWeatherEffects é muito básico, precisa de integração com o WeatherSystem para efeitos visuais e de gameplay completos.
O safeZoneCenter é um GameObject criado dinamicamente; idealmente, seria um componente já existente na cena do mapa.
MatchmakingService.cs
Propósito: Gerencia o processo de matchmaking usando PlayFab.
Estado Atual:
Singleton Instance e DontDestroyOnLoad implementados.
Configurações de fila, máximo de jogadores, timeout.
StartMatchmaking para criar um ticket de matchmaking no PlayFab.
PollMatchmakingStatus para verificar o status do ticket.
OnMatchmakingSuccess para lidar com match encontrado e iniciar conexão com o servidor de jogo.
ConnectToMatch para obter detalhes do servidor de jogo do PlayFab e conectar via NetworkManagerClient.
CancelMatchmaking e CancelMatchmakingTicket para cancelar o processo.
FailMatchmaking para lidar com falhas.
UpdateMatchmakingProgress e CheckMatchmakingTimeout para controle de tempo.
GetDefaultPlayerAttributes para criar atributos de jogador (nível, skill rating, região, plataforma) para o matchmaking.
GetPlayerProfile e CalculateSkillRating (placeholders).
ExecutePlayFabRequest para chamadas assíncronas ao PlayFab.
Eventos para início/fim/falha/cancelamento de matchmaking e progresso.
Funcionalidades Faltantes para a Versão Final:
GetPlayerProfile e CalculateSkillRating são placeholders e precisam de integração real com os dados do jogador (provavelmente do FirebaseBackendService ou cache local).
UI de matchmaking (progresso, status, cancelamento) precisa ser implementada no UIManager.
A lógica de serverIP e serverPort no ConnectToGameServer é específica para o transporte de rede.
A fila de matchmaking (matchmakingQueue) deve ser configurada no PlayFab.
MobileInputSystem.cs
Propósito: Gerencia o input em dispositivos móveis, incluindo controles na tela e giroscópio.
Estado Atual:
Singleton Instance e DontDestroyOnLoad implementados.
Referências para controles de toque na tela (joysticks, botões de tiro, pular, recarregar, mirar, agachar, interagir).
Elementos HUD para mobile e PC.
Configurações de sensibilidade de toque/giroscópio, auto-shoot.
DetectPlatform para identificar se é mobile.
SetupHUD para ativar/desativar HUDs.
SetupTouchControls para configurar controles de toque (placeholders para listeners de botões).
SetupInputSystem (placeholder).
SetupGyroscope para ativar giroscópio.
SetupAutoShoot para iniciar verificação de auto-shoot.
UpdateInputs para atualizar inputs mobile/PC.
UpdateMobileInputs, UpdateTouchLook, UpdatePCInputs para processar inputs.
HandleGyroscope para aplicar input do giroscópio.
ProcessAutoAim para assistência de mira.
CheckAutoShoot para verificar e disparar auto-shoot.
FindNearestEnemy para encontrar inimigo mais próximo.
TriggerAutoShoot e ProvideTactileFeedback (feedback háptico).
Métodos On...ButtonPressed/Released para eventos de botões.
Métodos para configurar sensibilidade e ativar/desativar giroscópio/auto-shoot/feedback háptico.
Funcionalidades Faltantes para a Versão Final:
Os listeners de botões (SetupButton) precisam ser implementados usando o novo sistema de input da Unity (InputAction callbacks).
Integração real com o PlayerController para passar os inputs de movimento, mira e ações.
A detecção de inimigos para auto-shoot (FindNearestEnemy) precisa de integração com o sistema de personagens.
UI dos controles na tela (OnScreenStick, OnScreenButton) precisa ser configurada visualmente.
playerCamera é buscado no Start, mas pode ser nulo se o PlayerController ainda não tiver sido instanciado.
NetworkManagerClient.cs
Propósito: Gerencia as conexões de rede do cliente, host e servidor.
Estado Atual:
Singleton Instance e DontDestroyOnLoad implementados.
Configurações de IP/Porta do servidor, máximo de jogadores, prefab do jogador.
SetupNetworkManager para configurar prefab do jogador e eventos de conexão.
StartHost, StartClient, StartServer para iniciar diferentes modos de rede.
Disconnect para desligar a conexão.
ConnectToGameServer para conectar a um IP/Porta específicos.
Eventos de callback de conexão/desconexão de cliente e servidor iniciado.
OnLocalClientConnected e OnLocalClientDisconnected para transição de estado via GameFlowManager.
IsConnected, GetConnectedPlayersCount para consulta.
Funcionalidades Faltantes para a Versão Final:
O playerPrefab precisa ser configurado no Inspector.
A configuração do transporte de rede (var transport = networkManager.NetworkConfig.NetworkTransport;) não está completa, precisa de um transporte específico (ex: UnityTransport).
A lógica de maxPlayersPerMatch aqui é apenas uma variável, não impõe um limite real.
Integração com o MatchmakingService para receber o IP/Porta do servidor.
Tratamento de erros de conexão mais detalhado.
PlayerController.cs
Propósito: Controla o jogador, movimento, combate e habilidades do herói.
Estado Atual:
NetworkBehaviour para sincronização de rede.
Configurações de movimento (velocidade, pulo, sensibilidade do mouse).
Configurações de combate (ponto de tiro, camada inimiga, saúde).
Variáveis de rede para posição, saúde, kills, dano causado.
Componentes (Rigidbody, Camera, WeaponController).
Input (movimento, mouse, sprint, pular, atirar, recarregar, habilidade).
currentHero (HeroLenda) para dados e habilidades do herói.
OnNetworkSpawn para ativar câmera do jogador local e atribuir um herói aleatório.
AssignRandomHero para selecionar e configurar um herói aleatório com stats básicos.
Update para lidar com input, movimento, rotação da câmera e combate (se for o dono).
HandleInput, HandleMovement, HandleCameraRotation, HandleCombat.
Jump, Shoot, Reload para ações.
UseHeroAbility para ativar a habilidade do herói.
PlayCombatPhrase para tocar frases brasileiras.
RPCs MoveServerRpc, MoveClientRpc, UseHeroAbilityServerRpc, UseHeroAbilityClientRpc para sincronização de rede.
TakeDamage (Server-side) para reduzir saúde e chamar Die.
AddKill, AddDamage (Server-side) para atualizar estatísticas.
Die (Server-side) e DieClientRpc para lógica de morte.
OnHealthChanged para debug de saúde.
OnCollisionEnter para detecção de chão.
SetHeroLenda para definir o herói.
Getters para saúde, kills, dano e nome.
Funcionalidades Faltantes para a Versão Final:
A detecção de chão (OnCollisionEnter) pode ser mais robusta.
A lógica de Shoot chama CombatSystem.Instance.ProcessDamageServerRpc, o que é bom para autoridade, mas o WeaponController.Shoot também faz um raycast local. É preciso garantir que a lógica de dano seja exclusivamente do CombatSystem no servidor.
O WeaponController anexado ao player precisa ser o WeaponController do namespace ArenaBrasil.Gameplay.Weapons (o segundo arquivo fornecido).
GetPlayerName() retorna "Guerreiro" ou playerName hardcoded. Precisa de integração com o sistema de perfil do jogador (Firebase/PlayFab).
UI de saúde, munição, habilidades, etc., precisa ser atualizada via UIManager.
Lógica de respawn ou gerenciamento de estado do jogador após a morte.
A seleção de herói é aleatória; precisa de um sistema de seleção de heróis no lobby.
RankingSystem.cs
Propósito: Gerencia o sistema de ranking dos jogadores.
Estado Atual:
Singleton Instance e DontDestroyOnLoad implementados.
Configurações de duração da temporada, decay de rank.
Tiers de rank brasileiros hardcoded (Bronze, Prata, Ouro, Platina, Diamante, Mestre, Predador).
InitializeRankingSystem para criar tiers e iniciar nova temporada.
CreateBrazilianRankTiers para definir os tiers.
StartNewSeason para criar uma nova temporada (simulada).
GetNextSeasonNumber e GetSeasonTheme (simulados).
AddRankPoints e SubtractRankPoints (Server-side) para ajustar pontos de rank, atualizar tier e conceder recompensas de rank-up.
GetTierForPoints e GetTierIndex para determinar o tier.
GiveRankUpRewards para conceder moedas/XP via EconomyManager.
ApplyRankDecay para reduzir pontos de rank por inatividade.
GetLeaderboard, GetTierLeaderboard, GetPlayerRanking para consulta.
RPCs RankChangedClientRpc e RankPointsUpdatedClientRpc para notificação ao cliente.
Funcionalidades Faltantes para a Versão Final:
Persistência dos dados de rank dos jogadores (PlayerRankData) e temporadas via backend (Firebase/PlayFab).
EconomyManager.Instance.AddCoins(playerId, coinReward) e AddExperience(playerId, xpReward) não existem no EconomyManager fornecido.
A lógica de GetNextSeasonNumber e GetSeasonTheme é simulada e precisa ser persistente.
UI de leaderboard, rank do jogador, progresso para o próximo tier precisa ser implementada no UIManager.
Integração com o ArenaBrasilGameManager ou CombatSystem para chamar AddRankPoints após cada partida.
ReplaySystem.cs
Propósito: Grava replays de partidas e permite a criação de clipes.
Estado Atual:
Singleton Instance e DontDestroyOnLoad implementados.
Configurações de gravação (duração, intervalo, máximo de replays).
Configurações de clipes (duração, máximo de clipes).
Tags de clipe brasileiras hardcoded.
StartRecording, StopRecording para controlar a gravação.
RecordFrame para capturar o estado dos jogadores a cada intervalo.
GetPlayerActionType (simulado).
SaveReplay para salvar o replay em arquivo local (JSON).
SaveReplayToFile, LoadSavedReplays, ManageReplayStorage para gerenciamento de arquivos.
CreateClip para gerar clipes de momentos específicos.
GetRecentFrames para obter frames recentes.
GetRandomBrazilianTag para tags de clipe.
SetupAutoClipTriggers (placeholders para eventos de combate/partida).
ShareClip e métodos ShareToTikTok, ShareToInstagram, ShareToTwitter, ShareToWhatsApp (placeholders para integração real).
LoadReplay para carregar replay de arquivo.
GetLocalPlayerName e GetMatchResult (placeholders).
Funcionalidades Faltantes para a Versão Final:
A gravação de replay é local e baseada em frames de posição/rotação/saúde. Para um replay completo, seria necessário gravar mais dados (eventos, disparos, habilidades).
A integração com PlayerController para GetHealth(), GetCurrentWeaponId(), GetPlayerActionType() é um placeholder.
GetLocalPlayerName() e GetMatchResult() são placeholders e precisam de integração real com os dados do jogador e da partida.
SetupAutoClipTriggers precisa de integração real com eventos de gameplay (multi-kills, vitórias, etc.).
A funcionalidade de compartilhamento de clipe é um placeholder e precisa de integração real com as APIs de mídias sociais.
UI de gerenciamento de replays/clipes, reprodução de replay e compartilhamento precisa ser implementada.
O CombatSystem não tem OnMultiKill e o ArenaBrasilGameManager não tem OnVictoryRoyale.
SafeZoneController.cs
Propósito: Controla a zona segura que encolhe durante a partida.
Estado Atual:
Singleton Instance e DontDestroyOnLoad implementados.
OnNetworkSpawn para inicializar a zona no servidor e criar o visual.
Variáveis de rede para centro, raio, fase e tempo restante da zona.
Configurações de raio inicial/final, fases, duração das fases, dano por fase.
InitializeZone para definir centro aleatório e raio inicial.
StartSafeZone para iniciar as corrotinas de sequência de fases e loop de dano.
Corrotina PhaseSequence para gerenciar as fases da zona.
Corrotina ShrinkZone para encolher o raio da zona.
Corrotina DamageLoop para aplicar dano a jogadores fora da zona.
ApplyZoneDamage para encontrar jogadores fora da zona e aplicar dano via PlayerController.TakeDamage.
RPC NotifyZoneDamageClientRpc para notificar clientes sobre dano da zona.
ShowZoneDamageEffect (placeholder para UI).
CreateZoneVisual e UpdateZoneVisual para representar a zona visualmente.
Callbacks de OnValueChanged para variáveis de rede para atualizar o visual e invocar eventos.
Getters para centro, raio, fase e tempo restante.
IsPositionInZone, GetDistanceToZone para consultas.
Funcionalidades Faltantes para a Versão Final:
O safeZoneVisualPrefab precisa ser um prefab real que represente a parede da zona.
ShowZoneDamageEffect precisa ser implementado no UIManager.
A lógica de PlayerController.TakeDamage é o ponto de integração.
A phaseDurations e phaseDamage são hardcoded, podem ser configuráveis via backend.
A NetworkVariable para networkPhaseTimeRemaining é atualizada a cada segundo, o que pode ser otimizado para não enviar a cada frame.
SocialMediaIntegration.cs
Propósito: Permite a integração do jogo com plataformas de mídia social para compartilhamento de conteúdo.
Estado Atual:
Singleton Instance e DontDestroyOnLoad implementados.
Lista de plataformas suportadas (supportedPlatforms) com configs hardcoded (TikTok, Instagram, Twitter, WhatsApp, YouTube).
Templates de conteúdo brasileiro (contentTemplates) e hashtags brasileiras hardcoded.
Recompensas de compartilhamento (sharingRewards) hardcoded.
InitializeSocialIntegration para configurar plataformas, conteúdo e recompensas.
SetupSocialPlatforms, SetupBrazilianContent, InitializeSharingRewards para definir dados.
ConnectSocialAccount para simular conexão de conta e conceder recompensa de conexão.
ShareContent para processar e compartilhar conteúdo em diferentes plataformas.
GetContentTemplate, ProcessContentTemplate, GetRandomBrazilianHashtags para preparar o conteúdo.
ShareToTikTok, ShareToInstagram, ShareToTwitter, ShareToWhatsApp, ShareToYouTube (placeholders para integração real).
SimulateSuccessfulShare para simular sucesso e conceder recompensas.
CheckAndGiveSharingRewards, CanClaimReward, HasClaimedReward para gerenciar recompensas.
GiveSharingReward para conceder recompensas via EconomyManager.
TrackSharedContent (placeholder para analytics).
IsAccountConnected, GetRecentShares, CreateSuggestedContent para consulta.
Funcionalidades Faltantes para a Versão Final:
A integração real com as APIs de cada plataforma social é um grande trabalho e não está implementada (atualmente é apenas Debug.Log e simulação).
GetUsernameFromToken é um placeholder.
EconomyManager.Instance.AddCurrency é chamado, mas HasClaimedReward é um placeholder e o EconomyManager não tem um sistema de rastreamento de recompensas por ID.
A persistência das contas conectadas e do histórico de compartilhamento precisa ser implementada.
UI para conectar contas, compartilhar conteúdo, exibir recompensas e conteúdo sugerido precisa ser implementada no UIManager.
mediaData (byte[]) para imagens/vídeos é um placeholder.
StreamingSystem.cs
Propósito: Gerencia a integração de streaming ao vivo dentro do jogo.
Estado Atual:
Singleton Instance e DontDestroyOnLoad implementados.
Configurações de streaming (habilitar, máximo de streams, qualidade).
Plataformas suportadas (supportedPlatforms) e streamers parceiros brasileiros hardcoded.
InitializeStreaming para configurar plataformas, carregar streamers e inicializar modo espectador.
SetupStreamingPlatforms e LoadPartneredStreamers para definir dados.
InitializeSpectatorMode (configura cullingMask).
StartStreamServerRpc (Server-side) para iniciar um stream, registrar dados e notificar clientes.
RPC StreamStartedClientRpc para notificação de stream iniciado.
EndStreamServerRpc (Server-side) para finalizar um stream.
RPC StreamEndedClientRpc para notificação de stream finalizado.
WatchStream para simular visualização de stream e chamar JoinStreamServerRpc.
JoinStreamServerRpc (Server-side) para aumentar contagem de viewers.
ShowStreamNotification (placeholder para UI).
EnableStreamMode para desabilitar UI sensível, habilitar overlay e ajustar áudio.
DisableSensitiveUI, EnableStreamerOverlay, AdjustAudioForStreaming (placeholders).
GetActiveStreams, GetTotalViewers para consulta.
Funcionalidades Faltantes para a Versão Final:
A integração real com as APIs de streaming (Twitch, YouTube, etc.) é um grande trabalho e não está implementada.
Persistência dos dados de stream e viewers.
ShowStreamNotification precisa ser implementado no UIManager.
DisableSensitiveUI, EnableStreamerOverlay, AdjustAudioForStreaming são placeholders e precisam de implementação real que afete a UI e o AudioManager.
O AudioManager.Instance.EnableStreamingMode() não existe no AudioManager fornecido.
A funcionalidade de modo espectador precisa ser desenvolvida.
UIManager.cs
Propósito: Gerencia todas as telas e elementos da interface do usuário.
Estado Atual:
Singleton Instance e DontDestroyOnLoad implementados.
Referências para todos os GameObject das telas (menu, lobby, HUD, inventário, loja, configurações, resultados).
Elementos HUD (barra de vida, texto de munição, jogadores vivos, timer da zona, kill feed).
Elementos UI brasileiros (texto motivacional, frases brasileiras hardcoded).
InitializeScreens para inicializar e esconder todas as telas, mostrando o menu principal.
ShowScreen para alternar entre telas e invocar OnScreenChanged.
OnScreenChanged para ajustar Cursor.lockState e mostrar frase motivacional.
UpdateHealthBar, UpdateAmmoDisplay, UpdatePlayersAlive, UpdateZoneTimer para atualizar elementos do HUD.
AddKillFeedItem para adicionar itens ao feed de eliminações.
ShowMotivationalPhrase para exibir frases e escondê-las após um tempo.
Métodos para eventos de botões (Play, Shop, Settings, Back, Inventory, Exit).
Classe KillFeedItem para itens do feed.
Funcionalidades Faltantes para a Versão Final:
Todas as chamadas UIManager.Instance.ShowNotification, ShowCreatorCodeReward, ShowLegendaryAnimation, ShowGachaResults, ShowInfluencerEventNotification, ShowKickMessage, ShowVictoryScreen, ShowDefeatScreen, AddChatMessage, UpdateWeatherIndicator, ShowSuggestedContent, UpdateAmmoDisplay, UpdatePlayersAlive, UpdateZoneTimer, AddKillFeedItem precisam ser implementadas com a lógica de UI real.
Os GameObject das telas e elementos HUD precisam ser configurados no Inspector.
Implementação completa do KillFeedItem para exibir texto.
Ajuste da UI para diferentes resoluções e aspect ratios (responsive UI).
Um sistema de notificação genérico para mensagens pop-up.
WeatherSystem.cs
Propósito: Gerencia o sistema de clima dinâmico no jogo.
Estado Atual:
Singleton Instance e DontDestroyOnLoad implementados.
Configurações para habilitar clima dinâmico, intervalo de mudança, duração da transição.
Referências para sistemas de partículas (chuva, tempestade, nevoeiro, tempestade de areia).
Referências para áudio do clima (fonte, clipes).
Referências para iluminação (luz do sol, gradientes de cor).
Variáveis para estado atual/alvo do clima, tempo de transição.
Padrões climáticos regionais hardcoded por tema cultural (Amazônia, Sertão, Costa, Pantanal, Metrópole).
InitializeWeatherSystem para configurar padrões e iniciar ciclo do clima.
Corrotina WeatherCycle para mudar o clima aleatoriamente.
ChangeWeatherRandomly e SelectRandomWeather para escolher o próximo clima.
ChangeWeather (Server-side) para iniciar transição de clima.
Corrotina TransitionWeather para interpolar efeitos visuais (iluminação, nevoeiro, partículas).
ApplyWeatherEffects para aplicar efeitos visuais e sonoros (partículas, luz, som).
ApplyGameplayEffects para aplicar multiplicadores de visibilidade, movimento, precisão de arma, dano (via evento).
Métodos auxiliares para obter cores de luz, intensidade, densidade de nevoeiro, multiplicadores de gameplay.
FadeWeatherParticles, StopAllWeatherParticles, PlayWeatherSound, StopWeatherSound.
Corrotina LightningEffect para tempestades.
RPCs WeatherChangedClientRpc e WeatherEffectClientRpc para notificação e aplicação de efeitos no cliente.
ApplyLocalWeatherEffects (placeholder).
Funcionalidades Faltantes para a Versão Final:
Os sistemas de partículas (rainParticles, stormParticles, etc.) precisam ser configurados no Inspector.
Os AudioClip de clima precisam ser configurados.
ApplyLocalWeatherEffects é um placeholder e precisa de integração real com o PlayerController e outros sistemas para aplicar os efeitos de gameplay (velocidade, precisão, visibilidade).
Integração com o MapManager para obter o tema cultural do mapa atual e aplicar os padrões climáticos regionais corretos.
UI de indicação de clima precisa ser implementada no UIManager.
Os padrões climáticos regionais hardcoded podem ser configuráveis via backend.
WeaponController.cs (Ambas as versões)
Existem duas versões do WeaponController.cs. A análise a seguir considera que a segunda versão (com BrazilianWeapon e weaponSlots) é a mais completa e a que deve ser usada, ou que as funcionalidades de ambas serão consolidadas.

Versão 1 (WeaponController.cs no namespace ArenaBrasil.Gameplay.Weapons, com WeaponData ScriptableObject)

Propósito: Controla o comportamento de uma arma (disparo, recarga).
Estado Atual:
NetworkBehaviour para sincronização de rede.
WeaponData (ScriptableObject) para dados da arma.
Shoot (chamada local) que invoca ShootServerRpc.
ShootServerRpc (Server-side) para lógica autoritária de disparo, raycast, aplicação de dano e decremento de munição.
ShootEffectsClientRpc (Client-side) para efeitos visuais (flash, som) e de impacto.
DrawTracer e HideTracer para traçadores.
Reload (chamada local) que invoca ReloadServerRpc.
ReloadServerRpc (Server-side) para iniciar recarga.
ReloadClientRpc (Client-side) para animação/som de recarga.
CompleteReload para finalizar recarga.
CanShoot para verificar condições de disparo.
EquipWeapon para trocar de arma.
Getters para munição e estado de recarga.
Funcionalidades Faltantes para a Versão Final:
A integração com PlayerController para hitPlayer.TakeDamage é um ponto de integração.
O muzzleFlashPrefab é um ParticleSystem, mas o muzzleFlash é um ParticleSystem no topo da classe. É preciso definir se o prefab será instanciado ou se o ParticleSystem será ativado/desativado.
WeaponData é um ScriptableObject e precisa ser criado como ativo no Unity.
A lógica de dano é duplicada (uma vez no WeaponController.ShootServerRpc e outra no PlayerController.Shoot que chama CombatSystem.ProcessDamageServerRpc). A lógica de dano deve ser centralizada no CombatSystem.
Versão 2 (WeaponController.cs no namespace ArenaBrasil.Gameplay.Weapons, com BrazilianWeapon e weaponSlots)

Propósito: Controla as armas do jogador, incluindo troca, disparo e recarga.
Estado Atual:
Referência para BrazilianWeapon (assumido ser um ScriptableObject ou classe similar a WeaponData).
Slots de arma (weaponSlots) e slot atual.
Start para inicializar com arma padrão.
Update para HandleWeaponSwitching e HandleReloading.
HandleWeaponSwitching para troca de arma via teclado e scroll do mouse.
HandleReloading para recarga via tecla 'R'.
Shoot para iniciar o disparo, com verificação de cooldown e munição.
FireHitscan e FireShotgun para diferentes tipos de disparo.
CalculateSpread para dispersão de tiro.
ProcessHit (placeholder para dano).
PlayShootEffects (flash, som) e PlayImpactEffects (som, placeholder visual).
Corrotina ReloadCoroutine para simular recarga.
EquipWeapon para equipar arma e atualizar UI.
SwitchToWeapon, SwitchToNextWeapon, SwitchToPreviousWeapon para troca de slot.
AddWeapon, AddAmmo para gerenciamento de inventário.
CanShoot para verificar condições de disparo.
Funcionalidades Faltantes para a Versão Final:
Esta versão não é NetworkBehaviour, o que é um problema para um jogo multiplayer. A lógica de disparo e recarga precisa ser sincronizada em rede (RPCs, Server-side authority).
BrazilianWeapon não foi fornecida como um arquivo separado, mas é assumido que seja similar a WeaponData.
ProcessHit é um placeholder. A lógica de dano deve ser delegada ao CombatSystem (como na primeira versão do WeaponController e no PlayerController).
PlayerController é usado, mas a integração não está clara.
Integração com UIManager para UpdateAmmoDisplay.
PlayWeaponSound e PlayImpactSound chamam AudioManager.Instance, o que é bom.
A lógica de currentAmmo e reserveAmmo precisa ser persistente.
Consolidação WeaponController.cs:
A versão final deve ser um NetworkBehaviour (como a primeira versão) e incluir a lógica de slots e troca de armas da segunda versão. A lógica de dano deve ser sempre delegada ao CombatSystem no servidor.

III. Funcionalidades Implementadas e Faltantes para a Versão Final
Esta seção consolida as funcionalidades implementadas e as que ainda precisam de atenção, categorizando-as para uma visão clara do que falta para a versão final.

A. Cliente de Jogo (Game Client)
Implementado:

Motor de Jogo: Unity Engine (Unity 6.0 LTS recomendado no README.md).
Linguagem: C#.
Estrutura Básica: Singleton GameManager, GameFlowManager, UIManager para gerenciamento central.
Controle de Jogador: PlayerController com movimento básico, rotação de câmera, pulo, sprint.
Sistema de Armas: WeaponController com disparo, recarga, gerenciamento de munição (em progresso, precisa de consolidação).
Input: MobileInputSystem com controles de toque, giroscópio, auto-shoot (com placeholders para integração de input actions).
Áudio: AudioManager com música de menu/combate/vitória, SFX, linhas de voz de heróis brasileiros.
Otimização: LowEndOptimization com detecção de dispositivo, presets de desempenho e otimizações de rede para o Brasil.
UI Básica: UIManager com telas (menu, lobby, HUD) e atualização de elementos como barra de vida, munição, jogadores vivos, kill feed (visualização no Debug.Log, não UI real).
Replay e Clipes: ReplaySystem para gravação de replay e criação de clipes (local, em arquivo JSON).
Integração Social: SocialMediaIntegration para simular conexão e compartilhamento de conteúdo em plataformas sociais (placeholders para APIs reais).
Conquistas: AchievementSystem para definir, rastrear e desbloquear conquistas.
Passe de Batalha: BattlePassSystem com temporadas, progressão de XP, recompensas gratuitas e premium.
Sistema de Clãs: ClanSystem para criação, entrada/saída, promoção de membros e experiência de clã.
Sistema de Chat: ChatSystem com chat global/equipe/clã, filtro de profanidade e mensagens rápidas.
Sistema de Gacha: GachaSystem com caixas de loot, taxas de raridade e sistema de "pity".
Influenciadores: InfluencerSystem com parcerias, programa de criadores e eventos.
Esports: EsportsManager com dados de torneios e equipes, registro e início de torneios (placeholders para chaves complexas).
Streaming: StreamingSystem para simular início/fim de stream e modo espectador (placeholders para APIs reais).
Ranking: RankingSystem com tiers de rank brasileiros, adição/subtração de pontos e decay.
Heróis Lendas: HeroLenda (ScriptableObject) para definir heróis e habilidades básicas.
Faltante para a Versão Final:

UI Completa: A maioria dos elementos de UI (telas, notificações, pop-ups, indicadores) está apenas com Debug.Log ou é um placeholder no UIManager. É necessário construir a interface visual completa.
Integração de Input Actions: O MobileInputSystem precisa ser totalmente integrado com o novo sistema de input da Unity para callbacks de botões.
Sistema de Inventário: Um sistema de inventário robusto para gerenciar itens, skins, armas, etc., obtidos via loot, gacha, loja ou recompensas.
Seleção de Heróis: Um sistema de seleção de heróis no lobby, permitindo que o jogador escolha seu HeroLenda antes da partida.
Customização de Personagens: Interface e lógica para equipar skins, emotes e outros cosméticos.
Progressão de Jogador: Visualização e gerenciamento do nível e XP do jogador (além do que o EconomyManager já calcula).
Replay e Compartilhamento: Reprodução de replays, renderização de clipes para compartilhamento e integração real com APIs de mídias sociais (TikTok, Instagram, etc.).
Sistema de Amigos: Adicionar/remover amigos, convites para partidas/clãs.
Notificações Push: Integração real com Firebase Cloud Messaging (FCM).
Anúncios Recompensados: Integração com SDKs de anúncios (AdMob/Unity Ads) e validação server-side.
Tratamento de Erros: Feedback visual e amigável para o usuário em caso de erros de rede, autenticação, etc.
Otimização Visual: Implementação completa de Cinemachine, Post Processing Stack, TextMeshPro para uma experiência visual polida.
Addressable Assets: Configuração completa para carregamento dinâmico de assets.
B. Servidores de Jogo Dedicados (DGS)
Implementado:

Motor de Jogo Headless: Unity Engine.
Linguagem: C#.
Networking: NetworkManagerClient (atua como cliente para o servidor dedicado e pode iniciar um servidor dedicado) e Unity.Netcode.
Lógica de Combate Autoritaria: CombatSystem para detecção de acertos, cálculo de dano, aplicação de dano e eliminações no servidor.
Zona Segura: SafeZoneController com fases de encolhimento, aplicação de dano e sincronização de rede.
Sistema de Loot: LootSystem para spawn de itens, seleção de loot e gerenciamento de pickups (server-side).
Geração de Mapas: BrazilianMapGenerator para geração procedural de mapas temáticos brasileiros (lógica básica).
Sistema de Clima: WeatherSystem com clima dinâmico, padrões regionais e aplicação de efeitos visuais/gameplay (server-side).
Anti-Cheat Server-Side: AntiCheatSystem com detecção de speed/teleport hacks, aimbot, wallhack, injeção de processos e validação de integridade.
Faltante para a Versão Final:

Hospedagem: Configuração e implantação em AWS GameLift.
Matchmaking: Integração completa com o FlexMatch do GameLift para alocação de sessões.
Persistência de Dados de Partida: Envio de estatísticas detalhadas do DGS para o Backend Central ao final da partida.
Otimização de Rede: Afinar a sincronização de rede (Ghosts, RPCs) para alto número de jogadores e baixa latência.
Simulação de Física: Otimização da simulação de física no servidor headless.
Anti-Cheat Avançado: Integração com soluções anti-cheat de terceiros (EasyAntiCheat, BattlEye) e lógica mais complexa de detecção.
Gerenciamento de Jogadores: Lógica para lidar com jogadores que entram/saem da partida, reconexões.
C. Backend Central (Central Backend Services)
Implementado:

Autenticação: FirebaseBackendService para login, registro de usuário e gerenciamento de estado de autenticação.
Banco de Dados de Perfil: FirebaseBackendService para criação, leitura e atualização de perfis de jogador no Firebase Firestore.
Economia Virtual: EconomyManager com gerenciamento de moedas/gemas, loja e concessão de recompensas de partida, utilizando PlayFab para persistência.
Matchmaking: MatchmakingService utilizando PlayFab para criação de tickets e polling de status.
LiveOps: LiveOpsManager para gerenciar eventos ao vivo e desafios diários/semanais (com dados hardcoded e simulação de backend).
Funções Serverless: Mencionado no README.md (Firebase Cloud Functions) e referenciado em FirebaseBackendService para validação de compra (placeholder).
Faltante para a Versão Final:

Implementação de Cloud Functions: Desenvolver as funções serverless reais para validação de compras, concessão de recompensas, anti-cheat server-side, gerenciamento de leaderboards e eventos.
Configuração de PlayFab: Configurar o catálogo de itens, economia virtual, leaderboards e eventos no painel do PlayFab.
Consolidação de Dados: Definir claramente qual sistema (Firebase Firestore ou PlayFab) é a fonte autoritária para cada tipo de dado persistente do jogador (ex: perfil vs. economia).
Analytics: Configuração completa do Google Analytics for Firebase e PlayFab para coleta de métricas detalhadas.
Monitoramento: Configuração de AWS CloudWatch/Google Cloud Monitoring e Sentry/Crashlytics para monitoramento de backend e cliente.
Segurança de API: Implementar chaves de API, tokens e outras medidas de segurança para chamadas entre o cliente/DGS e o backend.
Gerenciamento de Conteúdo: Um sistema robusto para gerenciar e entregar conteúdo dinamicamente (skins, mapas, eventos) via Firebase Cloud Storage/AWS S3.
Ferramentas de LiveOps: Utilizar as ferramentas de LiveOps do PlayFab para gerenciar campanhas, testes A/B, etc.
Escalabilidade: Testes de carga e otimização para garantir que o backend possa lidar com milhões de usuários.
IV. Próximos Passos e Entregáveis Iniciais para a Versão Final
Para levar o "Arena Brasil: Batalha de Lendas" à versão final, os seguintes passos são cruciais:

Consolidação de Gerenciadores:

Definir e consolidar o GameManager e ArenaBrasilGameManager. A versão ArenaBrasilGameManager parece ser a mais completa para o Battle Royale. Mover as responsabilidades relevantes do GameManager para ela ou remover o GameManager se for redundante.
Consolidar a lógica de zona segura entre MapManager e SafeZoneController, mantendo SafeZoneController como o autoritário.
Consolidar as duas versões de GameFlowManager e WeaponController.
Implementação Completa da UI:

Desenvolver todas as telas e elementos de HUD no UIManager (barras de vida, munição, mapas, placares, notificações de eventos, mensagens de chat, resultados de gacha/partida, etc.).
Criar e integrar os prefabs de UI necessários (ex: killFeedItemPrefab, elementos de LootPickup).
Integração de Sistemas Core:

Economia: Garantir que todos os sistemas (Clã, Battle Pass, LiveOps, Esports, Gacha, Influenciadores) utilizem os métodos corretos do EconomyManager para adicionar/deduzir moedas/XP/itens, e que o EconomyManager persista esses dados corretamente via PlayFab.
Inventário: Implementar um sistema de inventário funcional para que os itens obtidos via Gacha, loja, recompensas e loot sejam armazenados e acessíveis pelo jogador.
Dados do Jogador: Centralizar o carregamento e a atualização dos dados do jogador (perfil, stats, rank, conquistas) via FirebaseBackendService e EconomyManager (PlayFab).
Input: Finalizar a integração do MobileInputSystem com o PlayerController usando o sistema de Input Actions da Unity.
Aprimoramento do Gameplay Central:

Combate: Integrar totalmente o WeaponController e PlayerController com o CombatSystem para que toda a lógica de dano seja autoritária no servidor.
Habilidades de Heróis: Expandir e refinar as habilidades dos HeroLenda com efeitos de gameplay reais e integração com sistemas de status/dano.
Loot: Refinar a lógica de spawn de loot e a integração com o sistema de inventário.
Persistência de Dados:

Implementar a persistência de todos os dados de metajogo (Clãs, Battle Pass, Gacha, Influenciadores, Ranking, Conquistas, Replays) via Firebase Firestore e/ou PlayFab.
Configurar as regras de segurança nos bancos de dados.
Integrações de Backend e Terceiros:

Cloud Functions: Desenvolver as Firebase Cloud Functions para validações de compra, recompensas e anti-cheat.
APIs Sociais: Implementar a integração real com as APIs de mídias sociais para compartilhamento de conteúdo.
APIs de Streaming: Implementar a integração real com as APIs de plataformas de streaming.
Matchmaking: Testar e otimizar o MatchmakingService com o PlayFab e AWS GameLift.
Otimização e Performance:

Realizar testes extensivos de desempenho em uma ampla gama de dispositivos Android (especialmente low-end).
Otimizar a sincronização de rede para alto número de jogadores usando Netcode for Entities.
Refinar as otimizações do LowEndOptimization com base em testes reais.
Segurança:

Continuar aprimorando o AntiCheatSystem e integrá-lo com soluções de terceiros, se necessário.
Implementar validação server-side para todas as operações críticas.
Conteúdo e Autenticidade Cultural:

Expandir os assets de mapas, personagens, skins e áudio com mais elementos autênticos brasileiros.
Refinar a geração procedural de mapas (BrazilianMapGenerator) para criar ambientes mais complexos e jogáveis.
Testes e QA:

Desenvolver um plano de QA abrangente, incluindo testes funcionais, de desempenho, de rede, de segurança e de usabilidade.
Realizar testes de estresse em servidores.
Este relatório fornece um roteiro detalhado para a conclusão do projeto "Arena Brasil: Batalha de Lendas", garantindo que a visão técnica e estratégica seja plenamente realizada.

