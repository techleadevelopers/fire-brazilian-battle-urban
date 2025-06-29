
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using ArenaBrasil.Environment;

namespace ArenaBrasil.Maps
{
    public class BrazilianMapGenerator : NetworkBehaviour
    {
        public static BrazilianMapGenerator Instance { get; private set; }
        
        [Header("Brazilian Map Themes")]
        public MapTheme currentTheme = MapTheme.Favela;
        
        [Header("Map Assets")]
        public GameObject[] favelaBuildings;
        public GameObject[] amazonTrees;
        public GameObject[] cityBuildings;
        public GameObject[] beachAssets;
        public GameObject[] cerradoVegetation;
        
        [Header("Cultural Elements")]
        public GameObject[] streetArt; // Grafites
        public GameObject[] brazilianFlags;
        public GameObject[] footballFields;
        public GameObject[] musicStages;
        
        [Header("Map Configuration")]
        public Vector2 mapSize = new Vector2(2000f, 2000f);
        public int buildingDensity = 150;
        public int vegetationDensity = 300;
        
        private List<GameObject> spawnedObjects = new List<GameObject>();
        
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
        
        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                GenerateMap();
            }
        }
        
        public void GenerateMap()
        {
            Debug.Log($"Gerando mapa brasileiro: {currentTheme}");
            
            ClearExistingMap();
            
            switch (currentTheme)
            {
                case MapTheme.Favela:
                    GenerateFavelaMap();
                    break;
                case MapTheme.Amazonia:
                    GenerateAmazonMap();
                    break;
                case MapTheme.Metropole:
                    GenerateCityMap();
                    break;
                case MapTheme.Praia:
                    GenerateBeachMap();
                    break;
                case MapTheme.Cerrado:
                    GenerateCerradoMap();
                    break;
            }
            
            AddCulturalElements();
            GenerateStreetNames();
            
            NotifyMapGeneratedClientRpc();
        }
        
        void GenerateFavelaMap()
        {
            // Gerar casas coloridas em diferentes níveis
            for (int i = 0; i < buildingDensity; i++)
            {
                Vector3 position = GetRandomPosition();
                GameObject building = Instantiate(favelaBuildings[Random.Range(0, favelaBuildings.Length)], position, GetRandomRotation());
                
                // Adicionar cores vibrantes típicas das favelas
                var renderer = building.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = GetBrazilianColor();
                }
                
                spawnedObjects.Add(building);
            }
            
            // Adicionar elementos específicos
            AddFavelaElements();
        }
        
        void GenerateAmazonMap()
        {
            // Densidade alta de vegetação
            for (int i = 0; i < vegetationDensity; i++)
            {
                Vector3 position = GetRandomPosition();
                GameObject tree = Instantiate(amazonTrees[Random.Range(0, amazonTrees.Length)], position, GetRandomRotation());
                
                // Variar tamanhos das árvores
                float scale = Random.Range(0.8f, 2.5f);
                tree.transform.localScale = Vector3.one * scale;
                
                spawnedObjects.Add(tree);
            }
            
            // Adicionar rios e clareiras
            AddAmazonElements();
        }
        
        void GenerateCityMap()
        {
            // Prédios modernos organizados
            for (int i = 0; i < buildingDensity; i++)
            {
                Vector3 position = GetGridPosition(i);
                GameObject building = Instantiate(cityBuildings[Random.Range(0, cityBuildings.Length)], position, Quaternion.identity);
                
                // Variar alturas dos prédios
                float height = Random.Range(1f, 5f);
                building.transform.localScale = new Vector3(1f, height, 1f);
                
                spawnedObjects.Add(building);
            }
            
            AddCityElements();
        }
        
        void GenerateBeachMap()
        {
            // Elementos de praia
            for (int i = 0; i < buildingDensity / 2; i++)
            {
                Vector3 position = GetRandomPosition();
                GameObject asset = Instantiate(beachAssets[Random.Range(0, beachAssets.Length)], position, GetRandomRotation());
                spawnedObjects.Add(asset);
            }
            
            AddBeachElements();
        }
        
        void GenerateCerradoMap()
        {
            // Vegetação do cerrado
            for (int i = 0; i < vegetationDensity; i++)
            {
                Vector3 position = GetRandomPosition();
                GameObject vegetation = Instantiate(cerradoVegetation[Random.Range(0, cerradoVegetation.Length)], position, GetRandomRotation());
                spawnedObjects.Add(vegetation);
            }
            
            AddCerradoElements();
        }
        
        void AddCulturalElements()
        {
            // Adicionar arte de rua
            int streetArtCount = Random.Range(10, 25);
            for (int i = 0; i < streetArtCount; i++)
            {
                Vector3 position = GetRandomWallPosition();
                GameObject art = Instantiate(streetArt[Random.Range(0, streetArt.Length)], position, GetWallRotation());
                spawnedObjects.Add(art);
            }
            
            // Adicionar bandeiras do Brasil
            int flagCount = Random.Range(5, 15);
            for (int i = 0; i < flagCount; i++)
            {
                Vector3 position = GetRandomHighPosition();
                GameObject flag = Instantiate(brazilianFlags[Random.Range(0, brazilianFlags.Length)], position, GetRandomRotation());
                spawnedObjects.Add(flag);
            }
            
            // Adicionar campos de futebol
            int fieldCount = Random.Range(2, 5);
            for (int i = 0; i < fieldCount; i++)
            {
                Vector3 position = GetOpenAreaPosition();
                GameObject field = Instantiate(footballFields[Random.Range(0, footballFields.Length)], position, GetRandomRotation());
                spawnedObjects.Add(field);
            }
        }
        
        void AddFavelaElements()
        {
            // Lajes conectadas, escadarias, becos
            Debug.Log("Adicionando elementos específicos da favela");
        }
        
        void AddAmazonElements()
        {
            // Rios, pontes, casas na árvore
            Debug.Log("Adicionando elementos da Amazônia");
        }
        
        void AddCityElements()
        {
            // Avenidas, shopping centers, arranha-céus
            Debug.Log("Adicionando elementos urbanos");
        }
        
        void AddBeachElements()
        {
            // Quiosques, pranchas de surf, coqueiros
            Debug.Log("Adicionando elementos de praia");
        }
        
        void AddCerradoElements()
        {
            // Cachoeiras, pedras, vegetação baixa
            Debug.Log("Adicionando elementos do cerrado");
        }
        
        void GenerateStreetNames()
        {
            string[] brazilianStreetNames = {
                "Rua do Saci", "Avenida Iara", "Travessa Curupira",
                "Rua dos Boitatás", "Alameda das Lendas", "Praça do Folclore",
                "Rua da Independência", "Avenida Brasil", "Rua das Palmeiras"
            };
            
            // Implementar sistema de nomes de ruas
            Debug.Log("Gerando nomes de ruas brasileiras");
        }
        
        Vector3 GetRandomPosition()
        {
            return new Vector3(
                Random.Range(-mapSize.x / 2, mapSize.x / 2),
                0f,
                Random.Range(-mapSize.y / 2, mapSize.y / 2)
            );
        }
        
        Vector3 GetGridPosition(int index)
        {
            int gridSize = Mathf.FloorToInt(Mathf.Sqrt(buildingDensity));
            int x = index % gridSize;
            int z = index / gridSize;
            
            return new Vector3(x * 50f - mapSize.x / 2, 0f, z * 50f - mapSize.y / 2);
        }
        
        Vector3 GetRandomWallPosition()
        {
            // Lógica para encontrar paredes para street art
            return GetRandomPosition();
        }
        
        Vector3 GetRandomHighPosition()
        {
            Vector3 pos = GetRandomPosition();
            pos.y = Random.Range(10f, 30f);
            return pos;
        }
        
        Vector3 GetOpenAreaPosition()
        {
            // Lógica para encontrar áreas abertas
            return GetRandomPosition();
        }
        
        Quaternion GetRandomRotation()
        {
            return Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        }
        
        Quaternion GetWallRotation()
        {
            float[] angles = { 0f, 90f, 180f, 270f };
            return Quaternion.Euler(0f, angles[Random.Range(0, angles.Length)], 0f);
        }
        
        Color GetBrazilianColor()
        {
            Color[] brazilianColors = {
                new Color(1f, 1f, 0f),      // Amarelo
                new Color(0f, 1f, 0f),      // Verde
                new Color(0f, 0f, 1f),      // Azul
                new Color(1f, 0.5f, 0f),    // Laranja
                new Color(1f, 0f, 1f),      // Rosa
                new Color(0f, 1f, 1f),      // Cyan
            };
            
            return brazilianColors[Random.Range(0, brazilianColors.Length)];
        }
        
        void ClearExistingMap()
        {
            foreach (GameObject obj in spawnedObjects)
            {
                if (obj != null)
                {
                    DestroyImmediate(obj);
                }
            }
            spawnedObjects.Clear();
        }
        
        [ClientRpc]
        void NotifyMapGeneratedClientRpc()
        {
            Debug.Log($"Mapa {currentTheme} gerado com sucesso!");
            OnMapGenerated?.Invoke(currentTheme);
        }
        
        public event System.Action<MapTheme> OnMapGenerated;
        
        public void ChangeMapTheme(MapTheme newTheme)
        {
            if (IsServer)
            {
                currentTheme = newTheme;
                GenerateMap();
            }
        }
    }
    
    public enum MapTheme
    {
        Favela,
        Amazonia,
        Metropole,
        Praia,
        Cerrado
    }
}
