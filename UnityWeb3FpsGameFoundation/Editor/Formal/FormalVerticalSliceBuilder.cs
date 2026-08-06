using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Web3Fps.GameFoundation.Composition;
using Web3Fps.GameFoundation.Formal;
using Web3Fps.GameFoundation.Gameplay;
using Web3Fps.GameFoundation.Gameplay.Combat;
using Web3Fps.GameFoundation.Lobby;
using Web3Fps.GameFoundation.Prototype;

namespace Web3Fps.GameFoundation.Editor
{
    /// <summary>Generates the editable ASH//LEDGER Gate B vertical slice from package-owned code.</summary>
    public static class FormalVerticalSliceBuilder
    {
        private const string Root = "Assets/AshLedgerVerticalSlice";
        private const string Materials = Root + "/Materials";
        private const string Prefabs = Root + "/Prefabs";
        private const string Scenes = Root + "/Scenes";
        private const string LobbyScene = Scenes + "/AshLedgerLobby.unity";
        private const string RelayScene = Scenes + "/RiftRelay.unity";
        private const string PlayerPrefab = "Assets/Web3FpsPrototype/Prefabs/LocalPlayer.prefab";
        private const string BotPrefab = "Assets/Web3FpsPrototype/Prefabs/PrototypeBot.prefab";
        private const string LobbyUxml = "Packages/com.web3fps.game-foundation/Runtime/Formal/UI/AshLedgerLobby.uxml";
        private const string CombatUxml = "Packages/com.web3fps.game-foundation/Runtime/Formal/UI/AshLedgerCombatHud.uxml";

        [MenuItem("Tools/Web3 FPS/Create ASH LEDGER Vertical Slice")]
        public static void Create()
        {
            if (AssetDatabase.IsValidFolder(Root) && !EditorUtility.DisplayDialog(
                    "Rebuild ASH//LEDGER Vertical Slice",
                    "The generated Assets/AshLedgerVerticalSlice folder will be replaced.",
                    "Replace",
                    "Cancel")) return;

            EnsurePrototypeAssets();
            if (AssetDatabase.IsValidFolder(Root)) AssetDatabase.DeleteAsset(Root);
            EnsureFolder(Materials);
            EnsureFolder(Prefabs);
            EnsureFolder(Scenes);

            var palette = CreatePalette();
            var weapons = CreateWeaponPrefabs(palette);
            var panelSettings = CreatePanelSettings();
            CreateLobbyScene(palette, weapons, panelSettings);
            CreateRiftRelayScene(palette, weapons[0], panelSettings);
            SetBuildScenes();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(LobbyScene);
            Selection.activeObject = sceneAsset;
            EditorGUIUtility.PingObject(sceneAsset);
            EditorSceneManager.OpenScene(LobbyScene);
            Debug.Log("ASH//LEDGER vertical slice created. Lobby is open; press Play, then select DEPLOY TO RIFT RELAY.");
        }

        private static void EnsurePrototypeAssets()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefab) != null &&
                AssetDatabase.LoadAssetAtPath<GameObject>(BotPrefab) != null) return;
            PrototypeSceneBuilder.CreateLocalPrototypeScene();
        }

        private static Dictionary<string, Material> CreatePalette()
        {
            return new Dictionary<string, Material>
            {
                ["graphite"] = CreateMaterial("Graphite", new Color(0.035f, 0.047f, 0.055f), 0.72f),
                ["slate"] = CreateMaterial("Slate", new Color(0.095f, 0.13f, 0.15f), 0.62f),
                ["bone"] = CreateMaterial("Bone", new Color(0.72f, 0.73f, 0.67f), 0.45f),
                ["copper"] = CreateMaterial("Copper", new Color(0.72f, 0.3f, 0.12f), 0.28f),
                ["cobalt"] = CreateMaterial("Cobalt", new Color(0.06f, 0.42f, 0.78f), 0.22f),
                ["coral"] = CreateMaterial("Coral", new Color(0.88f, 0.16f, 0.13f), 0.25f),
                ["aurora"] = CreateMaterial("Aurora", new Color(0.18f, 0.88f, 0.66f), 0.18f)
            };
        }

        private static GameObject[] CreateWeaponPrefabs(IReadOnlyDictionary<string, Material> palette)
        {
            return new[]
            {
                CreateWeapon("KESTREL-7", new Vector3(1.18f, 0.18f, 0.16f), palette["cobalt"], palette["graphite"], true),
                CreateWeapon("PULSE-9", new Vector3(0.82f, 0.22f, 0.18f), palette["coral"], palette["graphite"], true),
                CreateWeapon("RELAY-3", new Vector3(0.58f, 0.17f, 0.13f), palette["aurora"], palette["graphite"], false)
            };
        }

        private static GameObject CreateWeapon(
            string name,
            Vector3 receiverScale,
            Material accent,
            Material dark,
            bool stock)
        {
            var root = new GameObject(name);
            CreatePart("Receiver", Vector3.zero, receiverScale, dark, root.transform);
            CreatePart("AccentRail", new Vector3(0f, 0.12f, 0f), new Vector3(receiverScale.x * 0.62f, 0.035f, 0.19f), accent, root.transform);
            CreatePart("Barrel", new Vector3(receiverScale.x * 0.68f, 0.015f, 0f), new Vector3(receiverScale.x * 0.46f, 0.075f, 0.075f), dark, root.transform);
            CreatePart("Grip", new Vector3(-receiverScale.x * 0.16f, -0.2f, 0f), new Vector3(0.12f, 0.28f, 0.12f), dark, root.transform, new Vector3(0f, 0f, -12f));
            CreatePart("Magazine", new Vector3(0.12f, -0.18f, 0f), new Vector3(0.13f, 0.27f, 0.12f), accent, root.transform, new Vector3(0f, 0f, 7f));
            if (stock) CreatePart("Stock", new Vector3(-receiverScale.x * 0.68f, -0.015f, 0f), new Vector3(receiverScale.x * 0.35f, 0.17f, 0.13f), dark, root.transform);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, Prefabs + "/" + name + ".prefab");
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static PanelSettings CreatePanelSettings()
        {
            var settings = ScriptableObject.CreateInstance<PanelSettings>();
            settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            settings.referenceResolution = new Vector2Int(1920, 1080);
            settings.match = 0.5f;
            settings.sortingOrder = 20;
            AssetDatabase.CreateAsset(settings, Root + "/AshLedgerPanelSettings.asset");
            return settings;
        }

        private static void CreateLobbyScene(
            IReadOnlyDictionary<string, Material> palette,
            IReadOnlyList<GameObject> weapons,
            PanelSettings panelSettings)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = FormalContentCatalog.LobbySceneName;
            var root = new GameObject("=== ASH LEDGER // ORBITAL ARCHIVE ===").transform;
            CreateCamera("Main Camera", new Vector3(0f, 2.8f, -10f), new Vector3(8f, 0f, 0f), new Color(0.012f, 0.018f, 0.022f), root);
            CreateLight("Key Light", new Vector3(45f, -30f, 0f), 1.5f, root);

            CreatePart("ArchiveDeck", new Vector3(0f, -0.3f, 1f), new Vector3(18f, 0.4f, 11f), palette["graphite"], root);
            CreatePart("RearBulkhead", new Vector3(0f, 3.6f, 5.8f), new Vector3(18f, 7.5f, 0.35f), palette["slate"], root);
            for (var i = -4; i <= 4; i++)
                CreatePart("LightRib_" + i, new Vector3(i * 2f, 3.5f, 5.55f), new Vector3(0.045f, 6.6f, 0.08f), i % 2 == 0 ? palette["copper"] : palette["bone"], root);

            var operatorRoot = new GameObject("ArchiveOperator").transform;
            operatorRoot.SetParent(root);
            operatorRoot.position = new Vector3(3.6f, 0f, 1.2f);
            CreatePrimitive("Body", PrimitiveType.Capsule, new Vector3(0f, 1.15f, 0f), new Vector3(0.72f, 1.15f, 0.48f), palette["bone"], operatorRoot);
            CreatePrimitive("Helmet", PrimitiveType.Sphere, new Vector3(0f, 2.35f, 0f), new Vector3(0.62f, 0.52f, 0.58f), palette["graphite"], operatorRoot);
            CreatePart("Visor", new Vector3(0f, 2.38f, -0.48f), new Vector3(0.48f, 0.13f, 0.04f), palette["aurora"], operatorRoot);
            var heldWeapon = (GameObject)PrefabUtility.InstantiatePrefab(weapons[0], operatorRoot);
            heldWeapon.name = "KESTREL-7 // Equipped";
            heldWeapon.transform.localPosition = new Vector3(-0.35f, 1.25f, -0.7f);
            heldWeapon.transform.localRotation = Quaternion.Euler(0f, -90f, -8f);

            for (var i = 0; i < weapons.Count; i++)
            {
                var display = (GameObject)PrefabUtility.InstantiatePrefab(weapons[i], root);
                display.name += " // Archive Display";
                display.transform.position = new Vector3(6.2f, 0.45f + i * 1.15f, 3.5f);
                display.transform.rotation = Quaternion.Euler(0f, 18f, 0f);
            }

            var systems = new GameObject("AshLedgerLobbySystems");
            systems.transform.SetParent(root);
            var bootstrap = systems.AddComponent<GameFoundationBootstrap>();
            bootstrap.Configure(true);
            var lobby = systems.AddComponent<Web3LobbyController>();
            lobby.Configure(bootstrap);
            var document = systems.AddComponent<UIDocument>();
            document.panelSettings = panelSettings;
            document.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(LobbyUxml);
            var view = systems.AddComponent<FormalLobbyView>();
            view.Configure(lobby, document, FormalContentCatalog.RiftRelaySceneName);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, LobbyScene);
        }

        private static void CreateRiftRelayScene(
            IReadOnlyDictionary<string, Material> palette,
            GameObject viewWeapon,
            PanelSettings panelSettings)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = FormalContentCatalog.RiftRelaySceneName;
            var root = new GameObject("=== RIFT RELAY // COMBAT DECK 07 ===").transform;
            CreateLight("Orbital Sun", new Vector3(52f, -28f, 0f), 1.35f, root);
            RenderSettings.ambientLight = new Color(0.16f, 0.19f, 0.21f);
            CreateRelayArena(root, palette);

            var spawnRoot = new GameObject("SpawnPoints").transform;
            spawnRoot.SetParent(root);
            var playerSpawn = CreateSpawn("CobaltSpawn", new Vector3(-18f, 0f, 0f), Quaternion.LookRotation(Vector3.right), spawnRoot);
            var botSpawn = CreateSpawn("CoralSpawn", new Vector3(18f, 0f, 0f), Quaternion.LookRotation(Vector3.left), spawnRoot);
            var playerSource = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefab);
            var botSource = AssetDatabase.LoadAssetAtPath<GameObject>(BotPrefab);
            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerSource);
            var bot = (GameObject)PrefabUtility.InstantiatePrefab(botSource);
            player.name = "CobaltOperator";
            bot.name = "CoralOperator";
            player.transform.SetPositionAndRotation(playerSpawn.position, playerSpawn.rotation);
            bot.transform.SetPositionAndRotation(botSpawn.position, botSpawn.rotation);

            var playerParticipant = player.GetComponent<PrototypeParticipant>();
            var playerMotor = player.GetComponent<PlayerMotor>();
            var playerLook = player.GetComponent<FirstPersonLook>();
            var playerWeapon = player.GetComponent<HitscanWeapon>();
            var playerInput = player.GetComponent<PrototypeInputDriver>();
            var camera = player.GetComponentInChildren<Camera>();
            var aimSource = camera.transform;
            camera.backgroundColor = new Color(0.025f, 0.035f, 0.045f);
            playerParticipant.Configure("local-player", "Cobalt", "cobalt", playerSpawn, new MonoBehaviour[] { playerMotor, playerLook, playerInput });

            var held = (GameObject)PrefabUtility.InstantiatePrefab(viewWeapon, aimSource);
            held.name = "KESTREL-7 // View Model";
            held.transform.localPosition = new Vector3(0.42f, -0.31f, 0.78f);
            held.transform.localRotation = Quaternion.Euler(2f, 2f, 0f);
            held.transform.localScale = Vector3.one * 0.72f;

            var botParticipant = bot.GetComponent<PrototypeParticipant>();
            var botController = bot.GetComponent<PrototypeBotController>();
            botParticipant.Configure("prototype-bot", "Coral", "coral", botSpawn, new MonoBehaviour[] { botController });
            botController.Configure(botParticipant, playerParticipant);

            var systems = new GameObject("RiftRelaySystems");
            systems.transform.SetParent(root);
            var match = systems.AddComponent<PrototypeMatchController>();
            match.Configure(playerParticipant, botParticipant, 7, 300f, 2f, FormalContentCatalog.ModeId, FormalContentCatalog.MapId);
            playerInput.Configure(playerParticipant, playerMotor, playerLook, playerWeapon, aimSource, match);
            var document = systems.AddComponent<UIDocument>();
            document.panelSettings = panelSettings;
            document.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(CombatUxml);
            var hud = systems.AddComponent<FormalCombatHud>();
            hud.Configure(document, match, playerParticipant);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, RelayScene);
        }

        private static void CreateRelayArena(Transform root, IReadOnlyDictionary<string, Material> palette)
        {
            var arena = new GameObject("RiftRelayGeometry").transform;
            arena.SetParent(root);
            CreatePart("Deck", new Vector3(0f, -0.5f, 0f), new Vector3(48f, 1f, 32f), palette["graphite"], arena);
            CreatePart("NorthBulkhead", new Vector3(0f, 2.5f, 16f), new Vector3(48f, 5f, 0.7f), palette["slate"], arena);
            CreatePart("SouthBulkhead", new Vector3(0f, 2.5f, -16f), new Vector3(48f, 5f, 0.7f), palette["slate"], arena);
            CreatePart("WestBulkhead", new Vector3(-24f, 2.5f, 0f), new Vector3(0.7f, 5f, 32f), palette["slate"], arena);
            CreatePart("EastBulkhead", new Vector3(24f, 2.5f, 0f), new Vector3(0.7f, 5f, 32f), palette["slate"], arena);

            CreatePart("RelayCore", new Vector3(0f, 3f, 0f), new Vector3(2.5f, 6f, 2.5f), palette["copper"], arena);
            CreatePart("RelayHaloA", new Vector3(0f, 5.2f, 0f), new Vector3(7f, 0.18f, 0.32f), palette["aurora"], arena);
            CreatePart("RelayHaloB", new Vector3(0f, 5.2f, 0f), new Vector3(0.32f, 0.18f, 7f), palette["aurora"], arena);
            CreatePart("CoolingChannelNorth", new Vector3(0f, 0.03f, 9.8f), new Vector3(39f, 0.08f, 2.4f), palette["cobalt"], arena);
            CreatePart("CoolingChannelSouth", new Vector3(0f, 0.03f, -9.8f), new Vector3(39f, 0.08f, 2.4f), palette["coral"], arena);

            var covers = new[]
            {
                new Vector3(-12f, 1f, 4.5f), new Vector3(-12f, 1f, -4.5f),
                new Vector3(-5f, 1f, 8f), new Vector3(-5f, 1f, -8f),
                new Vector3(5f, 1f, 8f), new Vector3(5f, 1f, -8f),
                new Vector3(12f, 1f, 4.5f), new Vector3(12f, 1f, -4.5f)
            };
            for (var i = 0; i < covers.Length; i++)
                CreatePart("Cover_" + i.ToString("00"), covers[i], new Vector3(3f, 2f, 1.2f), i < 4 ? palette["cobalt"] : palette["coral"], arena);
        }

        private static Camera CreateCamera(string name, Vector3 position, Vector3 euler, Color background, Transform parent)
        {
            var go = new GameObject(name);
            go.tag = "MainCamera";
            go.transform.SetParent(parent);
            go.transform.position = position;
            go.transform.rotation = Quaternion.Euler(euler);
            var camera = go.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = background;
            camera.fieldOfView = 52f;
            go.AddComponent<AudioListener>();
            return camera;
        }

        private static void CreateLight(string name, Vector3 euler, float intensity, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.rotation = Quaternion.Euler(euler);
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = intensity;
        }

        private static GameObject CreatePart(string name, Vector3 position, Vector3 scale, Material material, Transform parent, Vector3? euler = null)
        {
            return CreatePrimitive(name, PrimitiveType.Cube, position, scale, material, parent, euler);
        }

        private static GameObject CreatePrimitive(string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material, Transform parent, Vector3? euler = null)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            if (euler.HasValue) go.transform.localRotation = Quaternion.Euler(euler.Value);
            go.GetComponent<Renderer>().sharedMaterial = material;
            if (parent.name.Contains("Weapon") || parent.name.Contains("KESTREL") || parent.name.Contains("PULSE") || parent.name.Contains("RELAY-3"))
            {
                var collider = go.GetComponent<Collider>();
                if (collider != null) Object.DestroyImmediate(collider);
            }
            return go;
        }

        private static Transform CreateSpawn(string name, Vector3 position, Quaternion rotation, Transform parent)
        {
            var spawn = new GameObject(name).transform;
            spawn.SetParent(parent);
            spawn.SetPositionAndRotation(position, rotation);
            return spawn;
        }

        private static Material CreateMaterial(string name, Color color, float smoothness)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = new Material(shader) { color = color };
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            AssetDatabase.CreateAsset(material, Materials + "/" + name + ".mat");
            return material;
        }

        private static void EnsureFolder(string path)
        {
            var parts = path.Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private static void SetBuildScenes()
        {
            var otherScenes = EditorBuildSettings.scenes
                .Where(item => item.path != LobbyScene && item.path != RelayScene)
                .ToList();
            otherScenes.Insert(0, new EditorBuildSettingsScene(RelayScene, true));
            otherScenes.Insert(0, new EditorBuildSettingsScene(LobbyScene, true));
            EditorBuildSettings.scenes = otherScenes.ToArray();
        }
    }
}
