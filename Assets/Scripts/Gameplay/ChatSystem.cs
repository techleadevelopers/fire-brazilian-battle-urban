
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ArenaBrasil.Systems
{
    public class ChatSystem : NetworkBehaviour
    {
        public static ChatSystem Instance { get; private set; }
        
        [Header("Chat Configuration")]
        public int maxMessageLength = 100;
        public int maxMessagesHistory = 50;
        public float chatCooldown = 1f;
        public bool enableProfanityFilter = true;
        
        [Header("Brazilian Chat Features")]
        public string[] brazilianGreetings = { "E aí!", "Opa!", "Salve!", "Beleza?", "Tudo bom?" };
        public string[] brazilianGoodLuck = { "Boa sorte!", "Vai com tudo!", "Arrebenta!", "Mostra quem manda!" };
        
        private Dictionary<ulong, float> lastMessageTime = new Dictionary<ulong, float>();
        private List<ChatMessage> chatHistory = new List<ChatMessage>();
        private HashSet<string> bannedWords = new HashSet<string>();
        
        public event System.Action<ChatMessage> OnMessageReceived;
        public event System.Action<ulong, string> OnPlayerMuted;
        
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeChatSystem();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        void InitializeChatSystem()
        {
            Debug.Log("Arena Brasil - Initializing Chat System");
            LoadProfanityFilter();
        }
        
        void LoadProfanityFilter()
        {
            // Brazilian Portuguese profanity filter
            bannedWords = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
            {
                // Add Brazilian Portuguese profanity words here
                "idiota", "burro", "otário", "trouxa"
                // Note: In a real implementation, this would be loaded from a file
            };
        }
        
        public void SendMessage(ulong senderId, string message, ChatChannel channel = ChatChannel.All)
        {
            if (!IsServer) return;
            
            // Rate limiting
            if (lastMessageTime.ContainsKey(senderId))
            {
                if (Time.time - lastMessageTime[senderId] < chatCooldown)
                {
                    MessageRejectedClientRpc(senderId, "Aguarde antes de enviar outra mensagem");
                    return;
                }
            }
            
            // Validate message
            if (string.IsNullOrWhiteSpace(message) || message.Length > maxMessageLength)
            {
                MessageRejectedClientRpc(senderId, "Mensagem inválida ou muito longa");
                return;
            }
            
            // Filter profanity
            string filteredMessage = FilterProfanity(message);
            
            // Create chat message
            var chatMessage = new ChatMessage
            {
                senderId = senderId,
                senderName = GetPlayerName(senderId),
                message = filteredMessage,
                channel = channel,
                timestamp = System.DateTime.Now,
                messageType = DetectMessageType(filteredMessage)
            };
            
            // Add to history
            chatHistory.Add(chatMessage);
            if (chatHistory.Count > maxMessagesHistory)
            {
                chatHistory.RemoveAt(0);
            }
            
            lastMessageTime[senderId] = Time.time;
            
            // Broadcast message
            BroadcastMessage(chatMessage);
            
            OnMessageReceived?.Invoke(chatMessage);
        }
        
        void BroadcastMessage(ChatMessage message)
        {
            switch (message.channel)
            {
                case ChatChannel.All:
                    MessageReceivedClientRpc(message);
                    break;
                    
                case ChatChannel.Team:
                    BroadcastToTeam(message);
                    break;
                    
                case ChatChannel.Clan:
                    BroadcastToClan(message);
                    break;
                    
                case ChatChannel.Private:
                    // Handle private messages separately
                    break;
            }
        }
        
        void BroadcastToTeam(ChatMessage message)
        {
            // Get team members and send message only to them
            var teamMembers = GetTeamMembers(message.senderId);
            foreach (var memberId in teamMembers)
            {
                MessageReceivedForPlayerClientRpc(memberId, message);
            }
        }
        
        void BroadcastToClan(ChatMessage message)
        {
            // Get clan members and send message only to them
            if (ClanSystem.Instance != null)
            {
                var clan = ClanSystem.Instance.GetPlayerClan(message.senderId);
                if (clan != null)
                {
                    foreach (var member in clan.members)
                    {
                        MessageReceivedForPlayerClientRpc(member.playerId, message);
                    }
                }
            }
        }
        
        string FilterProfanity(string message)
        {
            if (!enableProfanityFilter) return message;
            
            string filtered = message;
            
            foreach (var word in bannedWords)
            {
                string pattern = @"\b" + Regex.Escape(word) + @"\b";
                filtered = Regex.Replace(filtered, pattern, "***", RegexOptions.IgnoreCase);
            }
            
            return filtered;
        }
        
        MessageType DetectMessageType(string message)
        {
            message = message.ToLower();
            
            if (brazilianGreetings.Any(greeting => message.Contains(greeting.ToLower())))
                return MessageType.Greeting;
            
            if (brazilianGoodLuck.Any(luck => message.Contains(luck.ToLower())))
                return MessageType.Encouragement;
            
            if (message.Contains("gg") || message.Contains("good game") || message.Contains("boa partida"))
                return MessageType.GoodGame;
            
            if (message.Contains("help") || message.Contains("ajuda") || message.Contains("socorro"))
                return MessageType.Help;
            
            return MessageType.Normal;
        }
        
        public void SendQuickMessage(ulong senderId, QuickMessageType type)
        {
            string message = GetQuickMessage(type);
            SendMessage(senderId, message, ChatChannel.Team);
        }
        
        string GetQuickMessage(QuickMessageType type)
        {
            switch (type)
            {
                case QuickMessageType.Hello:
                    return brazilianGreetings[Random.Range(0, brazilianGreetings.Length)];
                case QuickMessageType.GoodLuck:
                    return brazilianGoodLuck[Random.Range(0, brazilianGoodLuck.Length)];
                case QuickMessageType.Help:
                    return "Preciso de ajuda!";
                case QuickMessageType.Thanks:
                    return "Valeu!";
                case QuickMessageType.Sorry:
                    return "Desculpa!";
                case QuickMessageType.GoodGame:
                    return "Boa partida!";
                case QuickMessageType.AttackHere:
                    return "Ataquem aqui!";
                case QuickMessageType.DefendHere:
                    return "Defendam aqui!";
                case QuickMessageType.FollowMe:
                    return "Me sigam!";
                case QuickMessageType.Retreat:
                    return "Recuar!";
                default:
                    return "...";
            }
        }
        
        public void MutePlayer(ulong playerId, float duration = 300f) // 5 minutes default
        {
            if (!IsServer) return;
            
            lastMessageTime[playerId] = Time.time + duration;
            OnPlayerMuted?.Invoke(playerId, $"Mutado por {duration / 60f:F1} minutos");
            
            PlayerMutedClientRpc(playerId, duration);
        }
        
        List<ulong> GetTeamMembers(ulong playerId)
        {
            // This would integrate with team/squad system
            return new List<ulong> { playerId }; // Placeholder
        }
        
        string GetPlayerName(ulong playerId)
        {
            // This would integrate with player data system
            return $"Jogador{playerId}";
        }
        
        // Client RPCs
        [ClientRpc]
        void MessageReceivedClientRpc(ChatMessage message)
        {
            DisplayMessage(message);
        }
        
        [ClientRpc]
        void MessageReceivedForPlayerClientRpc(ulong targetPlayerId, ChatMessage message)
        {
            if (NetworkManager.Singleton.LocalClientId == targetPlayerId)
            {
                DisplayMessage(message);
            }
        }
        
        [ClientRpc]
        void MessageRejectedClientRpc(ulong playerId, string reason)
        {
            if (NetworkManager.Singleton.LocalClientId == playerId)
            {
                Debug.Log($"❌ Mensagem rejeitada: {reason}");
            }
        }
        
        [ClientRpc]
        void PlayerMutedClientRpc(ulong playerId, float duration)
        {
            if (NetworkManager.Singleton.LocalClientId == playerId)
            {
                Debug.Log($"🔇 Você foi mutado por {duration / 60f:F1} minutos");
            }
        }
        
        void DisplayMessage(ChatMessage message)
        {
            string channelPrefix = GetChannelPrefix(message.channel);
            string messageColor = GetMessageColor(message.messageType);
            string timeStr = message.timestamp.ToString("HH:mm");
            
            Debug.Log($"{channelPrefix}[{timeStr}] {message.senderName}: {message.message}");
            
            // This would update the UI chat window
            // UIManager.Instance?.AddChatMessage(message);
        }
        
        string GetChannelPrefix(ChatChannel channel)
        {
            switch (channel)
            {
                case ChatChannel.All: return "[TODOS] ";
                case ChatChannel.Team: return "[EQUIPE] ";
                case ChatChannel.Clan: return "[CLÃ] ";
                case ChatChannel.Private: return "[PRIVADO] ";
                default: return "";
            }
        }
        
        string GetMessageColor(MessageType type)
        {
            switch (type)
            {
                case MessageType.Greeting: return "#00FF00";
                case MessageType.Encouragement: return "#FFFF00";
                case MessageType.GoodGame: return "#00FFFF";
                case MessageType.Help: return "#FF0000";
                default: return "#FFFFFF";
            }
        }
        
        // Public getters
        public List<ChatMessage> GetChatHistory() => chatHistory.ToList();
        
        public bool IsPlayerMuted(ulong playerId)
        {
            return lastMessageTime.ContainsKey(playerId) && 
                   Time.time < lastMessageTime[playerId];
        }
    }
    
    [System.Serializable]
    public class ChatMessage
    {
        public ulong senderId;
        public string senderName;
        public string message;
        public ChatChannel channel;
        public System.DateTime timestamp;
        public MessageType messageType;
    }
    
    public enum ChatChannel
    {
        All,
        Team,
        Clan,
        Private
    }
    
    public enum MessageType
    {
        Normal,
        Greeting,
        Encouragement,
        GoodGame,
        Help,
        System
    }
    
    public enum QuickMessageType
    {
        Hello,
        GoodLuck,
        Help,
        Thanks,
        Sorry,
        GoodGame,
        AttackHere,
        DefendHere,
        FollowMe,
        Retreat
    }
}
