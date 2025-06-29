
// Firebase Cloud Functions Completas para Arena Brasil
const functions = require('firebase-functions');
const admin = require('firebase-admin');
const cors = require('cors')({ origin: true });
const { Storage } = require('@google-cloud/storage');

admin.initializeApp();
const storage = new Storage();

// Função para validar compras com persistência completa
exports.validatePurchase = functions.https.onCall(async (data, context) => {
    const { playerId, itemId, transactionId, platform, receiptData, amount } = data;
    
    try {
        // Validar receipt com stores
        let isValidPurchase = false;
        
        if (platform === 'android') {
            isValidPurchase = await validateGooglePlayPurchase(receiptData);
        } else if (platform === 'ios') {
            isValidPurchase = await validateAppStorePurchase(receiptData);
        }
        
        if (isValidPurchase) {
            // Transação atômica para garantir consistência
            await admin.firestore().runTransaction(async (transaction) => {
                // Conceder item ao jogador
                await grantItemToPlayer(playerId, itemId, transaction);
                
                // Registrar transação
                const transactionRef = admin.firestore().collection('transactions').doc();
                transaction.set(transactionRef, {
                    playerId,
                    itemId,
                    transactionId,
                    platform,
                    amount,
                    timestamp: admin.firestore.FieldValue.serverTimestamp(),
                    validated: true,
                    status: 'completed'
                });
                
                // Atualizar progresso de conquistas
                await updatePurchaseAchievements(playerId, amount, transaction);
            });
            
            return { success: true, message: 'Compra validada e item concedido' };
        } else {
            return { success: false, message: 'Compra inválida' };
        }
    } catch (error) {
        console.error('Erro na validação de compra:', error);
        return { success: false, message: 'Erro interno' };
    }
});

// Sistema Anti-Cheat avançado com persistência de violações
exports.validatePlayerAction = functions.https.onCall(async (data, context) => {
    const { playerId, actionType, position, velocity, timestamp, sessionId } = data;
    
    try {
        // Carregar perfil e histórico do jogador
        const [playerDoc, recentActions, sessionData] = await Promise.all([
            admin.firestore().collection('players').doc(playerId).get(),
            admin.firestore().collection('player_actions')
                .where('playerId', '==', playerId)
                .where('timestamp', '>', timestamp - 10000)
                .orderBy('timestamp', 'desc')
                .limit(20)
                .get(),
            admin.firestore().collection('game_sessions').doc(sessionId).get()
        ]);
        
        const playerData = playerDoc.data();
        const violations = [];
        
        // 1. Verificar velocidade impossível
        if (velocity && (Math.abs(velocity.x) > 50 || Math.abs(velocity.z) > 50)) {
            violations.push({
                type: 'speed_hack',
                severity: 'high',
                data: { velocity },
                timestamp: admin.firestore.FieldValue.serverTimestamp()
            });
        }
        
        // 2. Verificar teletransporte
        if (playerData.lastPosition && playerData.lastActionTime) {
            const distance = calculateDistance(position, playerData.lastPosition);
            const timeDiff = timestamp - playerData.lastActionTime;
            const maxDistance = (timeDiff / 1000) * 25; // 25 unidades por segundo máximo
            
            if (distance > maxDistance * 2) {
                violations.push({
                    type: 'teleport',
                    severity: 'critical',
                    data: { distance, maxDistance, timeDiff },
                    timestamp: admin.firestore.FieldValue.serverTimestamp()
                });
            }
        }
        
        // 3. Verificar rate de ações suspeitas
        if (recentActions.size > 30) {
            violations.push({
                type: 'action_spam',
                severity: 'medium',
                data: { actionCount: recentActions.size },
                timestamp: admin.firestore.FieldValue.serverTimestamp()
            });
        }
        
        // 4. Verificar padrões de comportamento
        const behaviorScore = await calculateBehaviorScore(playerId, recentActions.docs);
        if (behaviorScore < 0.3) {
            violations.push({
                type: 'suspicious_behavior',
                severity: 'medium',
                data: { behaviorScore },
                timestamp: admin.firestore.FieldValue.serverTimestamp()
            });
        }
        
        // Aplicar punições e persistir dados
        if (violations.length > 0) {
            await admin.firestore().runTransaction(async (transaction) => {
                await applyAntiCheatPunishment(playerId, violations, transaction);
                
                // Registrar violação no histórico
                const violationRef = admin.firestore().collection('cheat_violations').doc();
                transaction.set(violationRef, {
                    playerId,
                    sessionId,
                    violations,
                    actionType,
                    position,
                    velocity,
                    timestamp: admin.firestore.FieldValue.serverTimestamp(),
                    processed: true
                });
            });
        }
        
        // Salvar ação válida
        await admin.firestore().collection('player_actions').add({
            playerId,
            sessionId,
            actionType,
            position,
            velocity,
            timestamp,
            violations: violations.length > 0 ? violations : null,
            valid: violations.length === 0
        });
        
        // Atualizar última posição do jogador
        await admin.firestore().collection('players').doc(playerId).update({
            lastPosition: position,
            lastActionTime: timestamp,
            totalActions: admin.firestore.FieldValue.increment(1)
        });
        
        return {
            valid: violations.length === 0,
            violations,
            action: violations.length > 0 ? 'flag' : 'allow',
            behaviorScore
        };
        
    } catch (error) {
        console.error('Erro na validação anti-cheat:', error);
        return { valid: true, error: 'Erro interno' };
    }
});

// Gerenciamento completo de dados de gacha com pity system
exports.performGachaPull = functions.https.onCall(async (data, context) => {
    const { playerId, gachaType, pullCount = 1 } = data;
    
    try {
        return await admin.firestore().runTransaction(async (transaction) => {
            // Carregar dados de gacha do jogador
            const gachaDoc = await transaction.get(
                admin.firestore().collection('player_gacha').doc(playerId)
            );
            
            let gachaData = gachaDoc.exists ? gachaDoc.data() : {
                pulls: {},
                pityCounters: {},
                guaranteedCounters: {},
                totalPulls: 0
            };
            
            if (!gachaData.pulls[gachaType]) {
                gachaData.pulls[gachaType] = [];
                gachaData.pityCounters[gachaType] = 0;
                gachaData.guaranteedCounters[gachaType] = 0;
            }
            
            const results = [];
            
            for (let i = 0; i < pullCount; i++) {
                gachaData.pityCounters[gachaType]++;
                gachaData.guaranteedCounters[gachaType]++;
                
                // Determinar raridade com sistema pity
                const rarity = determineGachaRarity(
                    gachaType, 
                    gachaData.pityCounters[gachaType],
                    gachaData.guaranteedCounters[gachaType]
                );
                
                // Selecionar item específico
                const item = selectGachaItem(gachaType, rarity);
                
                results.push({
                    itemId: item.id,
                    rarity: rarity,
                    isNew: !playerHasItem(playerId, item.id),
                    pullNumber: gachaData.pityCounters[gachaType]
                });
                
                // Reset counters conforme necessário
                if (rarity === 'legendary') {
                    gachaData.pityCounters[gachaType] = 0;
                }
                if (rarity === 'epic' || rarity === 'legendary') {
                    gachaData.guaranteedCounters[gachaType] = 0;
                }
                
                // Adicionar ao inventário
                await grantItemToPlayer(playerId, item.id, transaction);
            }
            
            gachaData.totalPulls += pullCount;
            gachaData.pulls[gachaType].push({
                results,
                timestamp: admin.firestore.FieldValue.serverTimestamp(),
                pullCount
            });
            
            // Salvar dados de gacha atualizados
            transaction.set(
                admin.firestore().collection('player_gacha').doc(playerId),
                gachaData
            );
            
            return {
                success: true,
                results,
                newPityCounter: gachaData.pityCounters[gachaType],
                totalPulls: gachaData.totalPulls
            };
        });
        
    } catch (error) {
        console.error('Erro no gacha pull:', error);
        return { success: false, error: error.message };
    }
});

// Gerenciamento completo de clãs com persistência
exports.manageClan = functions.https.onCall(async (data, context) => {
    const { action, playerId, clanData } = data;
    
    try {
        switch (action) {
            case 'create':
                return await createClanWithPersistence(playerId, clanData);
            case 'join':
                return await joinClanWithPersistence(playerId, clanData.clanId);
            case 'leave':
                return await leaveClanWithPersistence(playerId);
            case 'invite':
                return await invitePlayerToClan(playerId, clanData.targetPlayerId, clanData.clanId);
            case 'promote':
                return await promoteClanMember(playerId, clanData.targetPlayerId, clanData.newRole);
            case 'kick':
                return await kickClanMember(playerId, clanData.targetPlayerId);
            default:
                return { success: false, error: 'Ação inválida' };
        }
    } catch (error) {
        console.error('Erro no gerenciamento de clã:', error);
        return { success: false, error: error.message };
    }
});

// Sistema de eventos ao vivo com progressão persistente
exports.triggerLiveEvent = functions.https.onCall(async (data, context) => {
    const { eventType, eventData, duration = 3600000 } = data; // 1 hora padrão
    
    try {
        return await admin.firestore().runTransaction(async (transaction) => {
            // Criar evento
            const eventRef = admin.firestore().collection('live_events').doc();
            const eventId = eventRef.id;
            
            const eventDocument = {
                id: eventId,
                type: eventType,
                data: eventData,
                startTime: admin.firestore.FieldValue.serverTimestamp(),
                endTime: new Date(Date.now() + duration),
                active: true,
                participants: [],
                rewards: generateEventRewards(eventType),
                progressTracking: {},
                completedBy: []
            };
            
            transaction.set(eventRef, eventDocument);
            
            // Criar desafios do evento
            const challenges = generateEventChallenges(eventType);
            for (const challenge of challenges) {
                const challengeRef = admin.firestore()
                    .collection('live_events')
                    .doc(eventId)
                    .collection('challenges')
                    .doc();
                    
                transaction.set(challengeRef, {
                    ...challenge,
                    eventId,
                    createdAt: admin.firestore.FieldValue.serverTimestamp()
                });
            }
            
            // Notificar todos os jogadores
            const message = {
                notification: {
                    title: 'Evento Arena Brasil!',
                    body: getEventMessage(eventType),
                    icon: 'event_icon'
                },
                data: {
                    eventId,
                    eventType,
                    eventData: JSON.stringify(eventData),
                    action: 'new_event'
                },
                topic: 'all_players'
            };
            
            await admin.messaging().send(message);
            
            return { success: true, eventId, challenges: challenges.length };
        });
        
    } catch (error) {
        console.error('Erro ao criar evento:', error);
        return { success: false, error: error.message };
    }
});

// Atualização de progresso de evento
exports.updateEventProgress = functions.https.onCall(async (data, context) => {
    const { playerId, eventId, challengeId, progressData } = data;
    
    try {
        return await admin.firestore().runTransaction(async (transaction) => {
            const progressRef = admin.firestore()
                .collection('event_progress')
                .doc(`${eventId}_${playerId}`);
                
            const progressDoc = await transaction.get(progressRef);
            let progress = progressDoc.exists ? progressDoc.data() : {
                playerId,
                eventId,
                challenges: {},
                totalProgress: 0,
                rewardsCollected: [],
                startedAt: admin.firestore.FieldValue.serverTimestamp()
            };
            
            if (!progress.challenges[challengeId]) {
                progress.challenges[challengeId] = {
                    progress: 0,
                    completed: false,
                    completedAt: null
                };
            }
            
            const challengeProgress = progress.challenges[challengeId];
            challengeProgress.progress += progressData.increment || 1;
            
            // Verificar se desafio foi completado
            const challengeDoc = await transaction.get(
                admin.firestore()
                    .collection('live_events')
                    .doc(eventId)
                    .collection('challenges')
                    .doc(challengeId)
            );
            
            const challengeData = challengeDoc.data();
            if (challengeProgress.progress >= challengeData.requirement && !challengeProgress.completed) {
                challengeProgress.completed = true;
                challengeProgress.completedAt = admin.firestore.FieldValue.serverTimestamp();
                
                // Conceder recompensa
                await grantEventReward(playerId, challengeData.reward, transaction);
            }
            
            progress.totalProgress = Object.values(progress.challenges)
                .reduce((sum, ch) => sum + ch.progress, 0);
            
            transaction.set(progressRef, progress);
            
            return {
                success: true,
                challengeCompleted: challengeProgress.completed,
                totalProgress: progress.totalProgress
            };
        });
        
    } catch (error) {
        console.error('Erro ao atualizar progresso:', error);
        return { success: false, error: error.message };
    }
});

// Gerenciamento de ranking e temporadas
exports.updatePlayerRank = functions.https.onCall(async (data, context) => {
    const { playerId, seasonId, rankPoints, matchResult } = data;
    
    try {
        return await admin.firestore().runTransaction(async (transaction) => {
            const rankRef = admin.firestore()
                .collection('player_ranks')
                .doc(`${seasonId}_${playerId}`);
                
            const rankDoc = await transaction.get(rankRef);
            let rankData = rankDoc.exists ? rankDoc.data() : {
                playerId,
                seasonId,
                currentRank: 'bronze_i',
                rankPoints: 1000,
                matches: 0,
                wins: 0,
                topPlacements: 0,
                bestRank: 'bronze_i',
                rankHistory: []
            };
            
            const oldRank = rankData.currentRank;
            const oldPoints = rankData.rankPoints;
            
            // Calcular novos pontos baseado no resultado
            let pointsChange = calculateRankPointsChange(matchResult, rankData.currentRank);
            rankData.rankPoints = Math.max(0, rankData.rankPoints + pointsChange);
            
            // Atualizar estatísticas
            rankData.matches++;
            if (matchResult.won) rankData.wins++;
            if (matchResult.placement <= 3) rankData.topPlacements++;
            
            // Determinar novo rank
            const newRank = determineRankFromPoints(rankData.rankPoints);
            const rankChanged = newRank !== oldRank;
            
            if (rankChanged) {
                rankData.rankHistory.push({
                    fromRank: oldRank,
                    toRank: newRank,
                    pointsChange,
                    timestamp: admin.firestore.FieldValue.serverTimestamp(),
                    matchResult
                });
                
                rankData.currentRank = newRank;
                
                // Atualizar melhor rank se necessário
                if (isRankHigher(newRank, rankData.bestRank)) {
                    rankData.bestRank = newRank;
                }
            }
            
            transaction.set(rankRef, rankData);
            
            // Atualizar leaderboard global da temporada
            const leaderboardRef = admin.firestore()
                .collection('season_leaderboards')
                .doc(seasonId)
                .collection('players')
                .doc(playerId);
                
            transaction.set(leaderboardRef, {
                playerId,
                displayName: matchResult.playerName,
                rankPoints: rankData.rankPoints,
                currentRank: rankData.currentRank,
                wins: rankData.wins,
                matches: rankData.matches,
                lastUpdated: admin.firestore.FieldValue.serverTimestamp()
            });
            
            return {
                success: true,
                rankChanged,
                oldRank,
                newRank: rankData.currentRank,
                pointsChange,
                newPoints: rankData.rankPoints
            };
        });
        
    } catch (error) {
        console.error('Erro ao atualizar rank:', error);
        return { success: false, error: error.message };
    }
});

// Gerenciamento de conteúdo dinâmico
exports.getContentManifest = functions.https.onCall(async (data, context) => {
    const { version, platform } = data;
    
    try {
        // Buscar manifest atual
        const manifestDoc = await admin.firestore()
            .collection('content_manifests')
            .doc('current')
            .get();
            
        if (!manifestDoc.exists) {
            throw new Error('Manifest não encontrado');
        }
        
        const manifest = manifestDoc.data();
        
        // Verificar se há atualizações
        const needsUpdate = manifest.version !== version;
        
        if (needsUpdate) {
            // Gerar URLs assinadas para download
            const downloadUrls = {};
            
            for (const [category, files] of Object.entries(manifest.content)) {
                downloadUrls[category] = {};
                
                for (const [fileName, fileData] of Object.entries(files)) {
                    if (fileData.platforms.includes(platform)) {
                        const bucket = storage.bucket('arena-brasil-content');
                        const file = bucket.file(`${category}/${fileName}`);
                        
                        const [url] = await file.getSignedUrl({
                            action: 'read',
                            expires: Date.now() + 3600000 // 1 hora
                        });
                        
                        downloadUrls[category][fileName] = {
                            url,
                            size: fileData.size,
                            checksum: fileData.checksum
                        };
                    }
                }
            }
            
            return {
                needsUpdate: true,
                newVersion: manifest.version,
                downloadUrls,
                totalSize: calculateTotalSize(downloadUrls)
            };
        }
        
        return {
            needsUpdate: false,
            currentVersion: version
        };
        
    } catch (error) {
        console.error('Erro ao buscar manifest:', error);
        return { success: false, error: error.message };
    }
});

// Integração com mídias sociais - histórico de compartilhamento
exports.trackSocialShare = functions.https.onCall(async (data, context) => {
    const { playerId, platform, contentType, contentId, metadata } = data;
    
    try {
        const shareRef = admin.firestore().collection('social_shares').doc();
        
        await shareRef.set({
            playerId,
            platform,
            contentType,
            contentId,
            metadata,
            timestamp: admin.firestore.FieldValue.serverTimestamp(),
            verified: false
        });
        
        // Atualizar estatísticas do jogador
        await admin.firestore().collection('players').doc(playerId).update({
            [`socialShares.${platform}`]: admin.firestore.FieldValue.increment(1),
            totalShares: admin.firestore.FieldValue.increment(1)
        });
        
        // Verificar conquistas de compartilhamento
        await checkSocialAchievements(playerId, platform);
        
        return { success: true, shareId: shareRef.id };
        
    } catch (error) {
        console.error('Erro ao registrar compartilhamento:', error);
        return { success: false, error: error.message };
    }
});

// Funções auxiliares expandidas
async function grantItemToPlayer(playerId, itemId, transaction) {
    const inventoryRef = admin.firestore().collection('player_inventory').doc(playerId);
    const inventoryDoc = await transaction.get(inventoryRef);
    
    let inventory = inventoryDoc.exists ? inventoryDoc.data() : {
        items: [],
        capacity: 100,
        categories: {}
    };
    
    const existingItem = inventory.items.find(item => item.itemId === itemId);
    
    if (existingItem) {
        existingItem.quantity += 1;
    } else {
        inventory.items.push({
            itemId,
            quantity: 1,
            acquiredAt: admin.firestore.FieldValue.serverTimestamp(),
            source: 'purchase'
        });
    }
    
    transaction.set(inventoryRef, inventory);
}

async function calculateBehaviorScore(playerId, recentActions) {
    // Algoritmo simples de análise comportamental
    let score = 1.0;
    
    const actionTypes = recentActions.map(doc => doc.data().actionType);
    const uniqueTypes = [...new Set(actionTypes)];
    
    // Penalizar ações muito repetitivas
    if (uniqueTypes.length < actionTypes.length * 0.3) {
        score -= 0.3;
    }
    
    // Penalizar velocidade de ações muito alta
    const timeSpan = recentActions.length > 1 ? 
        recentActions[0].data().timestamp - recentActions[recentActions.length - 1].data().timestamp : 1000;
    
    const actionsPerSecond = recentActions.length / (timeSpan / 1000);
    if (actionsPerSecond > 10) {
        score -= 0.4;
    }
    
    return Math.max(0, score);
}

function determineGachaRarity(gachaType, pityCounter, guaranteedCounter) {
    // Sistema pity - probabilidades aumentam com pulls
    const baseProbabilities = {
        legendary: 0.02,
        epic: 0.08,
        rare: 0.25,
        common: 0.65
    };
    
    // Pity para legendary (90 pulls)
    if (pityCounter >= 90) return 'legendary';
    
    // Guaranteed epic a cada 10 pulls
    if (guaranteedCounter >= 10) return 'epic';
    
    // Aumentar chances conforme pity
    const legendaryChance = baseProbabilities.legendary + (pityCounter * 0.006);
    const epicChance = baseProbabilities.epic + (guaranteedCounter * 0.02);
    
    const roll = Math.random();
    
    if (roll < legendaryChance) return 'legendary';
    if (roll < legendaryChance + epicChance) return 'epic';
    if (roll < legendaryChance + epicChance + baseProbabilities.rare) return 'rare';
    
    return 'common';
}

function selectGachaItem(gachaType, rarity) {
    // Pool de itens por raridade e tipo de gacha
    const itemPools = {
        heroes: {
            legendary: [
                { id: 'hero_saci_gold', name: 'Saci Dourado' },
                { id: 'hero_curupira_ancient', name: 'Curupira Ancestral' }
            ],
            epic: [
                { id: 'hero_iara_mystical', name: 'Iara Mística' },
                { id: 'hero_boitata_flame', name: 'Boitatá Flamejante' }
            ]
        },
        weapons: {
            legendary: [
                { id: 'weapon_fal_brasil_elite', name: 'FAL Brasil Elite' }
            ]
        }
    };
    
    const pool = itemPools[gachaType]?.[rarity] || itemPools.heroes[rarity];
    return pool[Math.floor(Math.random() * pool.length)];
}

function calculateRankPointsChange(matchResult, currentRank) {
    let basePoints = 0;
    
    // Pontos por colocação
    if (matchResult.placement === 1) basePoints += 100;
    else if (matchResult.placement <= 3) basePoints += 60;
    else if (matchResult.placement <= 10) basePoints += 20;
    else basePoints -= 20;
    
    // Bônus por kills
    basePoints += matchResult.kills * 5;
    
    // Modificador por rank atual
    const rankMultipliers = {
        'bronze_i': 1.5,
        'silver_i': 1.2,
        'gold_i': 1.0,
        'platinum_i': 0.8,
        'diamond_i': 0.6
    };
    
    return Math.floor(basePoints * (rankMultipliers[currentRank] || 1.0));
}

function determineRankFromPoints(points) {
    if (points >= 10000) return 'predator';
    if (points >= 8000) return 'master';
    if (points >= 6000) return 'diamond_i';
    if (points >= 4000) return 'platinum_i';
    if (points >= 2500) return 'gold_i';
    if (points >= 1500) return 'silver_i';
    return 'bronze_i';
}

async function createClanWithPersistence(playerId, clanData) {
    return await admin.firestore().runTransaction(async (transaction) => {
        // Verificar se jogador já está em clã
        const playerDoc = await transaction.get(
            admin.firestore().collection('players').doc(playerId)
        );
        
        if (playerDoc.data()?.clanId) {
            throw new Error('Jogador já está em um clã');
        }
        
        // Criar clã
        const clanRef = admin.firestore().collection('clans').doc();
        const clanId = clanRef.id;
        
        const clanDocument = {
            id: clanId,
            name: clanData.name,
            tag: clanData.tag,
            description: clanData.description || '',
            leaderId: playerId,
            members: [playerId],
            memberRoles: { [playerId]: 'leader' },
            createdAt: admin.firestore.FieldValue.serverTimestamp(),
            level: 1,
            xp: 0,
            settings: {
                isPublic: clanData.isPublic || true,
                autoAccept: clanData.autoAccept || false,
                language: 'pt-BR'
            },
            statistics: {
                totalMatches: 0,
                totalWins: 0,
                totalKills: 0
            }
        };
        
        transaction.set(clanRef, clanDocument);
        
        // Atualizar jogador
        transaction.update(
            admin.firestore().collection('players').doc(playerId),
            {
                clanId,
                clanRole: 'leader',
                joinedClanAt: admin.firestore.FieldValue.serverTimestamp()
            }
        );
        
        return { success: true, clanId };
    });
}

// Limpeza automática de dados antigos
exports.cleanupOldData = functions.pubsub.schedule('every 24 hours').onRun(async (context) => {
    const oneWeekAgo = new Date(Date.now() - 7 * 24 * 60 * 60 * 1000);
    const oneMonthAgo = new Date(Date.now() - 30 * 24 * 60 * 60 * 1000);
    
    const batch = admin.firestore().batch();
    
    // Limpar ações antigas dos jogadores
    const oldActions = await admin.firestore()
        .collection('player_actions')
        .where('timestamp', '<', oneWeekAgo.getTime())
        .limit(500)
        .get();
    
    oldActions.forEach(doc => batch.delete(doc.ref));
    
    // Limpar eventos expirados
    const expiredEvents = await admin.firestore()
        .collection('live_events')
        .where('endTime', '<', oneWeekAgo)
        .where('active', '==', false)
        .get();
    
    expiredEvents.forEach(doc => batch.delete(doc.ref));
    
    // Limpar violações anti-cheat antigas (manter histórico crítico)
    const oldViolations = await admin.firestore()
        .collection('cheat_violations')
        .where('timestamp', '<', oneMonthAgo.getTime())
        .where('severity', 'in', ['low', 'medium'])
        .limit(200)
        .get();
    
    oldViolations.forEach(doc => batch.delete(doc.ref));
    
    await batch.commit();
    
    console.log(`Limpeza concluída: ${oldActions.size + expiredEvents.size + oldViolations.size} documentos removidos`);
    
    return null;
});

// Utilitários
function calculateDistance(pos1, pos2) {
    const dx = pos1.x - pos2.x;
    const dz = pos1.z - pos2.z;
    return Math.sqrt(dx * dx + dz * dz);
}

function getEventMessage(eventType) {
    const messages = {
        'double_xp': 'XP em dobro por tempo limitado! 🎯',
        'rare_loot': 'Loot raro aparecendo no mapa! 💎',
        'boss_spawn': 'Boss especial apareceu na arena! 👹',
        'brazilian_celebration': 'Evento especial Brasil! 🇧🇷',
        'clan_war': 'Guerra de Clãs iniciada! ⚔️',
        'community_challenge': 'Desafio da Comunidade ativo! 🏆'
    };
    
    return messages[eventType] || 'Evento especial ativo! ✨';
}

function generateEventRewards(eventType) {
    const rewardPools = {
        'double_xp': { xp: 1000, coins: 500 },
        'rare_loot': { coins: 1000, gems: 50 },
        'boss_spawn': { gems: 100, exclusive_skin: 1 },
        'brazilian_celebration': { coins: 2000, gems: 100, brasil_emote: 1 }
    };
    
    return rewardPools[eventType] || { coins: 100 };
}

function generateEventChallenges(eventType) {
    const challengeTemplates = {
        'double_xp': [
            { name: 'Ganhar 3 partidas', requirement: 3, reward: { coins: 500 } },
            { name: 'Conseguir 10 kills', requirement: 10, reward: { xp: 1000 } }
        ],
        'brazilian_celebration': [
            { name: 'Jogar com herói brasileiro', requirement: 5, reward: { gems: 25 } },
            { name: 'Vencer em mapa brasileiro', requirement: 1, reward: { exclusive_skin: 1 } }
        ]
    };
    
    return challengeTemplates[eventType] || [];
}

function calculateTotalSize(downloadUrls) {
    let total = 0;
    
    for (const category of Object.values(downloadUrls)) {
        for (const file of Object.values(category)) {
            total += file.size || 0;
        }
    }
    
    return total;
}

async function checkSocialAchievements(playerId, platform) {
    // Verificar conquistas relacionadas a compartilhamento social
    const playerDoc = await admin.firestore().collection('players').doc(playerId).get();
    const playerData = playerDoc.data();
    
    const totalShares = playerData.totalShares || 0;
    
    if (totalShares >= 10 && !playerData.achievements?.includes('social_sharer')) {
        await admin.firestore().collection('players').doc(playerId).update({
            achievements: admin.firestore.FieldValue.arrayUnion('social_sharer'),
            coins: admin.firestore.FieldValue.increment(500)
        });
    }
}
