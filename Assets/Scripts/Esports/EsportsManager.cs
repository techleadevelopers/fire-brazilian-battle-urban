
using UnityEngine;
using System.Collections.Generic;
using System;
using ArenaBrasil.Backend;
using Unity.Netcode;

namespace ArenaBrasil.Esports
{
    public class EsportsManager : NetworkBehaviour
    {
        public static EsportsManager Instance { get; private set; }
        
        [Header("Tournament Configuration")]
        public List<Tournament> activeTournaments = new List<Tournament>();
        public List<Team> registeredTeams = new List<Team>();
        
        [Header("Brazilian Championships")]
        public ChampionshipData lbaf; // Liga Brasileira Arena Brasil
        public RegionalChampionship[] regionalChampionships;
        
        [Header("Prize Pools")]
        public int monthlyTournamentPrize = 10000;
        public int seasonalChampionshipPrize = 50000;
        public int nationalChampionshipPrize = 100000;
        
        [Header("Streaming Integration")]
        public string twitchStreamKey;
        public string youtubeStreamKey;
        public bool enableAutoStreaming = true;
        
        // Events
        public event Action<Tournament> OnTournamentStarted;
        public event Action<Tournament> OnTournamentEnded;
        public event Action<Team> OnTeamRegistered;
        public event Action<Match> OnMatchCompleted;
        public event Action<string> OnChampionCrowned;
        
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        void Start()
        {
            InitializeEsportsSystem();
            SetupBrazilianChampionships();
        }
        
        void InitializeEsportsSystem()
        {
            Debug.Log("Arena Brasil - Inicializando sistema de eSports");
            
            CreateSeasonalTournaments();
            SetupRankingSystem();
            InitializeStreamingIntegration();
        }
        
        void SetupBrazilianChampionships()
        {
            // Liga Brasileira Arena Brasil (LBAB)
            lbaf = new ChampionshipData
            {
                championshipId = "lbab_2024",
                name = "Liga Brasileira Arena Brasil",
                description = "O maior campeonato de Arena Brasil do país!",
                maxTeams = 16,
                prizePool = nationalChampionshipPrize,
                format = TournamentFormat.League,
                startDate = new DateTime(2024, 3, 1),
                endDate = new DateTime(2024, 11, 30),
                regions = new string[] { "Nacional" }
            };
            
            // Regional Championships
            regionalChampionships = new RegionalChampionship[]
            {
                new RegionalChampionship
                {
                    region = "Sudeste",
                    name = "Campeonato Sudeste Arena Brasil",
                    maxTeams = 32,
                    prizePool = 25000,
                    qualifierSpots = 4
                },
                new RegionalChampionship
                {
                    region = "Sul",
                    name = "Campeonato Sul Arena Brasil", 
                    maxTeams = 24,
                    prizePool = 20000,
                    qualifierSpots = 3
                },
                new RegionalChampionship
                {
                    region = "Nordeste",
                    name = "Campeonato Nordeste Arena Brasil",
                    maxTeams = 28,
                    prizePool = 22000,
                    qualifierSpots = 3
                },
                new RegionalChampionship
                {
                    region = "Norte",
                    name = "Campeonato Norte Arena Brasil",
                    maxTeams = 20,
                    prizePool = 18000,
                    qualifierSpots = 2
                },
                new RegionalChampionship
                {
                    region = "Centro-Oeste",
                    name = "Campeonato Centro-Oeste Arena Brasil",
                    maxTeams = 16,
                    prizePool = 15000,
                    qualifierSpots = 2
                }
            };
        }
        
        void CreateSeasonalTournaments()
        {
            // Weekly tournaments
            CreateWeeklyTournament();
            
            // Monthly championship
            CreateMonthlyChampionship();
            
            // Special event tournaments
            CreateSpecialEventTournaments();
        }
        
        void CreateWeeklyTournament()
        {
            var weeklyTournament = new Tournament
            {
                tournamentId = $"weekly_{DateTime.Now:yyyyMMdd}",
                name = "Torneio Semanal das Lendas",
                description = "Competição semanal para todos os níveis",
                maxTeams = 64,
                prizePool = 2000,
                format = TournamentFormat.Elimination,
                registrationStart = DateTime.Now,
                registrationEnd = DateTime.Now.AddDays(5),
                tournamentStart = DateTime.Now.AddDays(6),
                tournamentEnd = DateTime.Now.AddDays(7),
                entryFee = 0,
                minimumRank = Rank.Bronze,
                type = TournamentType.Weekly
            };
            
            activeTournaments.Add(weeklyTournament);
        }
        
        void CreateMonthlyChampionship()
        {
            var monthlyChampionship = new Tournament
            {
                tournamentId = $"monthly_{DateTime.Now:yyyyMM}",
                name = "Campeonato Mensal Arena Brasil",
                description = "O maior torneio mensal com as melhores equipes",
                maxTeams = 32,
                prizePool = monthlyTournamentPrize,
                format = TournamentFormat.Swiss,
                registrationStart = DateTime.Now,
                registrationEnd = DateTime.Now.AddDays(20),
                tournamentStart = DateTime.Now.AddDays(25),
                tournamentEnd = DateTime.Now.AddDays(30),
                entryFee = 100,
                minimumRank = Rank.Gold,
                type = TournamentType.Monthly
            };
            
            activeTournaments.Add(monthlyChampionship);
        }
        
        void CreateSpecialEventTournaments()
        {
            // Carnaval Tournament
            if (DateTime.Now.Month == 2)
            {
                var carnavalTournament = new Tournament
                {
                    tournamentId = "carnaval_special_2024",
                    name = "Torneio Especial de Carnaval",
                    description = "Celebre o Carnaval competindo pelas melhores recompensas!",
                    maxTeams = 128,
                    prizePool = 15000,
                    format = TournamentFormat.Elimination,
                    type = TournamentType.Special,
                    specialRewards = new List<string> { "Skin Saci Carnavalesco", "Emote Samba Vitória", "Paraquedas Confete" }
                };
                
                activeTournaments.Add(carnavalTournament);
            }
            
            // Independence Day Tournament
            if (DateTime.Now.Month == 9)
            {
                var independenceTournament = new Tournament
                {
                    tournamentId = "independence_special_2024",
                    name = "Torneio da Independência",
                    description = "Mostre seu patriotismo brasileiro na arena!",
                    maxTeams = 64,
                    prizePool = 20000,
                    format = TournamentFormat.Elimination,
                    type = TournamentType.Special,
                    specialRewards = new List<string> { "Skin Bandeira do Brasil", "Emote Grito do Ipiranga", "Arma Dourada BR" }
                };
                
                activeTournaments.Add(independenceTournament);
            }
        }
        
        public bool RegisterTeam(Team team, string tournamentId)
        {
            var tournament = activeTournaments.Find(t => t.tournamentId == tournamentId);
            if (tournament == null)
            {
                Debug.LogError($"Torneio não encontrado: {tournamentId}");
                return false;
            }
            
            // Check registration period
            if (DateTime.Now < tournament.registrationStart || DateTime.Now > tournament.registrationEnd)
            {
                Debug.LogError("Fora do período de inscrição");
                return false;
            }
            
            // Check team limit
            if (tournament.registeredTeams.Count >= tournament.maxTeams)
            {
                Debug.LogError("Torneio lotado");
                return false;
            }
            
            // Check minimum rank requirement
            if (!CheckTeamEligibility(team, tournament))
            {
                Debug.LogError("Equipe não atende aos requisitos mínimos");
                return false;
            }
            
            // Check entry fee
            if (tournament.entryFee > 0 && !ProcessEntryFee(team, tournament.entryFee))
            {
                Debug.LogError("Taxa de inscrição não paga");
                return false;
            }
            
            // Register team
            tournament.registeredTeams.Add(team);
            if (!registeredTeams.Contains(team))
            {
                registeredTeams.Add(team);
            }
            
            OnTeamRegistered?.Invoke(team);
            
            Debug.Log($"Equipe {team.teamName} registrada no torneio {tournament.name}");
            return true;
        }
        
        bool CheckTeamEligibility(Team team, Tournament tournament)
        {
            foreach (var player in team.players)
            {
                if (player.rank < tournament.minimumRank)
                {
                    return false;
                }
            }
            return true;
        }
        
        bool ProcessEntryFee(Team team, int fee)
        {
            // Integration with economy system
            if (EconomyManager.Instance != null)
            {
                return EconomyManager.Instance.SpendCoins(team.captainId, fee);
            }
            return true; // Temporary for testing
        }
        
        void SetupRankingSystem()
        {
            InvokeRepeating(nameof(UpdateRankings), 0f, 3600f); // Update every hour
        }
        
        void UpdateRankings()
        {
            // Update team rankings based on recent performance
            foreach (var team in registeredTeams)
            {
                CalculateTeamRating(team);
            }
            
            // Sort teams by rating
            registeredTeams.Sort((a, b) => b.rating.CompareTo(a.rating));
        }
        
        void CalculateTeamRating(Team team)
        {
            // ELO-based rating system
            float baseRating = 1000f;
            float ratingChange = 0f;
            
            foreach (var match in team.matchHistory)
            {
                if (match.won)
                {
                    ratingChange += 25f - (team.rating - match.opponentRating) * 0.01f;
                }
                else
                {
                    ratingChange -= 25f + (team.rating - match.opponentRating) * 0.01f;
                }
            }
            
            team.rating = Mathf.Max(baseRating, team.rating + ratingChange);
        }
        
        void InitializeStreamingIntegration()
        {
            if (enableAutoStreaming)
            {
                Debug.Log("Configurando integração com streaming");
                SetupTwitchIntegration();
                SetupYouTubeIntegration();
            }
        }
        
        void SetupTwitchIntegration()
        {
            // Integration with Twitch API for automatic streaming
            Debug.Log("Configurando integração com Twitch");
        }
        
        void SetupYouTubeIntegration()
        {
            // Integration with YouTube Live API
            Debug.Log("Configurando integração com YouTube Live");
        }
        
        [ServerRpc(RequireOwnership = false)]
        public void StartTournamentServerRpc(string tournamentId)
        {
            var tournament = activeTournaments.Find(t => t.tournamentId == tournamentId);
            if (tournament == null) return;
            
            if (DateTime.Now >= tournament.tournamentStart && tournament.status == TournamentStatus.Registration)
            {
                tournament.status = TournamentStatus.Active;
                GenerateTournamentBracket(tournament);
                OnTournamentStarted?.Invoke(tournament);
                
                StartTournamentClientRpc(tournamentId);
            }
        }
        
        [ClientRpc]
        void StartTournamentClientRpc(string tournamentId)
        {
            Debug.Log($"Torneio iniciado: {tournamentId}");
        }
        
        void GenerateTournamentBracket(Tournament tournament)
        {
            tournament.bracket = new TournamentBracket();
            
            switch (tournament.format)
            {
                case TournamentFormat.Elimination:
                    GenerateEliminationBracket(tournament);
                    break;
                case TournamentFormat.Swiss:
                    GenerateSwissBracket(tournament);
                    break;
                case TournamentFormat.League:
                    GenerateLeagueBracket(tournament);
                    break;
            }
        }
        
        void GenerateEliminationBracket(Tournament tournament)
        {
            var teams = tournament.registeredTeams.ToArray();
            ShuffleArray(teams);
            
            // Create first round matches
            for (int i = 0; i < teams.Length; i += 2)
            {
                if (i + 1 < teams.Length)
                {
                    var match = new Match
                    {
                        matchId = $"{tournament.tournamentId}_r1_{i / 2}",
                        team1 = teams[i],
                        team2 = teams[i + 1],
                        round = 1,
                        scheduledTime = tournament.tournamentStart.AddMinutes(i * 30)
                    };
                    
                    tournament.bracket.matches.Add(match);
                }
            }
        }
        
        void GenerateSwissBracket(Tournament tournament)
        {
            // Swiss system implementation
            Debug.Log("Gerando chaveamento suíço");
        }
        
        void GenerateLeagueBracket(Tournament tournament)
        {
            // Round-robin league implementation
            Debug.Log("Gerando chaveamento de liga");
        }
        
        void ShuffleArray(Team[] array)
        {
            for (int i = 0; i < array.Length; i++)
            {
                int randomIndex = UnityEngine.Random.Range(i, array.Length);
                Team temp = array[i];
                array[i] = array[randomIndex];
                array[randomIndex] = temp;
            }
        }
        
        public void CompleteMatch(string matchId, string winnerTeamId, MatchResult result)
        {
            var tournament = activeTournaments.Find(t => 
                t.bracket.matches.Exists(m => m.matchId == matchId));
            
            if (tournament == null) return;
            
            var match = tournament.bracket.matches.Find(m => m.matchId == matchId);
            if (match == null) return;
            
            match.completed = true;
            match.winnerTeamId = winnerTeamId;
            match.result = result;
            
            OnMatchCompleted?.Invoke(match);
            
            // Update tournament progress
            AdvanceTournament(tournament, match);
        }
        
        void AdvanceTournament(Tournament tournament, Match completedMatch)
        {
            // Advance winner to next round
            Team winner = completedMatch.team1.teamId == completedMatch.winnerTeamId ? 
                         completedMatch.team1 : completedMatch.team2;
            
            // Check if tournament is complete
            if (IsTournamentComplete(tournament))
            {
                CompleteTournament(tournament, winner);
            }
            else
            {
                // Generate next round matches if needed
                GenerateNextRoundMatches(tournament);
            }
        }
        
        bool IsTournamentComplete(Tournament tournament)
        {
            return tournament.bracket.matches.Count == 1 && 
                   tournament.bracket.matches[0].completed;
        }
        
        void CompleteTournament(Tournament tournament, Team winner)
        {
            tournament.status = TournamentStatus.Completed;
            tournament.winner = winner;
            
            // Distribute prizes
            DistributePrizes(tournament);
            
            OnTournamentEnded?.Invoke(tournament);
            OnChampionCrowned?.Invoke($"{winner.teamName} é o campeão do {tournament.name}!");
            
            Debug.Log($"Torneio {tournament.name} finalizado! Campeão: {winner.teamName}");
        }
        
        void DistributePrizes(Tournament tournament)
        {
            if (tournament.winner == null) return;
            
            // Distribute prize pool among team members
            int prizePerPlayer = tournament.prizePool / tournament.winner.players.Count;
            
            foreach (var player in tournament.winner.players)
            {
                if (EconomyManager.Instance != null)
                {
                    EconomyManager.Instance.AddCoins(prizePerPlayer);
                }
            }
            
            // Grant special rewards if available
            if (tournament.specialRewards != null && tournament.specialRewards.Count > 0)
            {
                foreach (var player in tournament.winner.players)
                {
                    foreach (var reward in tournament.specialRewards)
                    {
                        if (EconomyManager.Instance != null)
                        {
                            EconomyManager.Instance.GrantItem(reward);
                        }
                    }
                }
            }
        }
        
        void GenerateNextRoundMatches(Tournament tournament)
        {
            // Implementation depends on tournament format
            Debug.Log("Gerando próxima rodada");
        }
        
        public List<Tournament> GetActiveTournaments() => activeTournaments;
        public List<Team> GetRegisteredTeams() => registeredTeams;
        
        public Tournament GetTournament(string tournamentId)
        {
            return activeTournaments.Find(t => t.tournamentId == tournamentId);
        }
    }
    
    [Serializable]
    public class Tournament
    {
        public string tournamentId;
        public string name;
        public string description;
        public int maxTeams;
        public int prizePool;
        public TournamentFormat format;
        public DateTime registrationStart;
        public DateTime registrationEnd;
        public DateTime tournamentStart;
        public DateTime tournamentEnd;
        public int entryFee;
        public Rank minimumRank;
        public TournamentType type;
        public TournamentStatus status = TournamentStatus.Registration;
        public List<Team> registeredTeams = new List<Team>();
        public List<string> specialRewards = new List<string>();
        public TournamentBracket bracket;
        public Team winner;
    }
    
    [Serializable]
    public class Team
    {
        public string teamId;
        public string teamName;
        public string captainId;
        public List<Player> players = new List<Player>();
        public float rating = 1000f;
        public List<MatchHistory> matchHistory = new List<MatchHistory>();
        public string region;
    }
    
    [Serializable]
    public class Player
    {
        public string playerId;
        public string playerName;
        public Rank rank;
        public float individualRating;
    }
    
    [Serializable]
    public class TournamentBracket
    {
        public List<Match> matches = new List<Match>();
    }
    
    [Serializable]
    public class Match
    {
        public string matchId;
        public Team team1;
        public Team team2;
        public int round;
        public DateTime scheduledTime;
        public bool completed;
        public string winnerTeamId;
        public MatchResult result;
    }
    
    [Serializable]
    public class MatchResult
    {
        public int team1Score;
        public int team2Score;
        public float matchDuration;
        public Dictionary<string, PlayerStats> playerStats;
    }
    
    [Serializable]
    public class PlayerStats
    {
        public int kills;
        public int deaths;
        public float damage;
        public float survivalTime;
    }
    
    [Serializable]
    public class MatchHistory
    {
        public bool won;
        public float opponentRating;
        public DateTime matchDate;
    }
    
    [Serializable]
    public class ChampionshipData
    {
        public string championshipId;
        public string name;
        public string description;
        public int maxTeams;
        public int prizePool;
        public TournamentFormat format;
        public DateTime startDate;
        public DateTime endDate;
        public string[] regions;
    }
    
    [Serializable]
    public class RegionalChampionship
    {
        public string region;
        public string name;
        public int maxTeams;
        public int prizePool;
        public int qualifierSpots;
    }
    
    public enum TournamentFormat
    {
        Elimination,
        Swiss,
        League
    }
    
    public enum TournamentType
    {
        Weekly,
        Monthly,
        Seasonal,
        Special
    }
    
    public enum TournamentStatus
    {
        Registration,
        Active,
        Completed,
        Cancelled
    }
    
    public enum Rank
    {
        Bronze,
        Silver,
        Gold,
        Platinum,
        Diamond,
        Master,
        Legend
    }
}
