using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Web3Fps.GameFoundation.Gameplay;
using Web3Fps.GameFoundation.Gameplay.Combat;
using Web3Fps.GameFoundation.Prototype;

namespace Web3Fps.GameFoundation.Editor
{
    public static class PrototypeSceneBuilder
    {
        private const string RootFolder = "Assets/Web3FpsPrototype";
        private const string PrefabFolder = RootFolder + "/Prefabs";
        private const string MaterialFolder = RootFolder + "/Materials";
        private const string ScenePath = RootFolder + "/Prototype.unity";

        [MenuItem("Tools/Web3 FPS/Create Local Prototype Scene")]
        public static void CreateLocalPrototypeScene()
        {
            if (AssetDatabase.IsValidFolder(RootFolder) && !EditorUtility.DisplayDialog(
                    "Rebuild Web3 FPS Prototype",
                    "Assets/Web3FpsPrototype already exists. Replace the generated prototype assets?",
                    "Replace",
                    "Cancel")) return;

            if (AssetDatabase.IsValidFolder(RootFolder)) AssetDatabase.DeleteAsset(RootFolder);
            EnsureFolder(RootFolder);
            EnsureFolder(PrefabFolder);
            EnsureFolder(MaterialFolder);

            var floorMaterial = CreateMaterial(MaterialFolder + "/ArenaFloor.mat", new Color(0.11f, 0.14f, 0.18f));
            var wallMaterial = CreateMaterial(MaterialFolder + "/ArenaWall.mat", new Color(0.2f, 0.26f, 0.34f));
            var botMaterial = CreateMaterial(MaterialFolder + "/Bot.mat", new Color(0.75f, 0.12f, 0.1f));
            var playerPrefab = CreatePlayerPrefab();
            var botPrefab = CreateBotPrefab(botMaterial);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "Prototype";
            var root = new GameObject("=== WEB3 FPS LOCAL PROTOTYPE ===").transform;
            CreateLighting(root);
            CreateArena(root, floorMaterial, wallMaterial);

            var spawnRoot = new GameObject("SpawnPoints").transform;
            spawnRoot.SetParent(root);
            var playerSpawn = CreateSpawn("PlayerSpawn", new Vector3(-7f, 0f, 0f), Quaternion.LookRotation(Vector3.right), spawnRoot);
            var botSpawn = CreateSpawn("BotSpawn", new Vector3(7f, 0f, 0f), Quaternion.LookRotation(Vector3.left), spawnRoot);

            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            var bot = (GameObject)PrefabUtility.InstantiatePrefab(botPrefab);
            player.name = "LocalPlayer";
            bot.name = "PrototypeBot";
            player.transform.SetPositionAndRotation(playerSpawn.position, playerSpawn.rotation);
            bot.transform.SetPositionAndRotation(botSpawn.position, botSpawn.rotation);

            var playerParticipant = player.GetComponent<PrototypeParticipant>();
            var playerMotor = player.GetComponent<PlayerMotor>();
            var playerLook = player.GetComponent<FirstPersonLook>();
            var playerWeapon = player.GetComponent<HitscanWeapon>();
            var playerInput = player.GetComponent<PrototypeInputDriver>();
            var aimSource = player.GetComponentInChildren<Camera>().transform;
            playerParticipant.Configure(
                "local-player",
                "You",
                "blue",
                playerSpawn,
                new MonoBehaviour[] { playerMotor, playerLook, playerInput });

            var botParticipant = bot.GetComponent<PrototypeParticipant>();
            var botController = bot.GetComponent<PrototypeBotController>();
            botParticipant.Configure(
                "prototype-bot",
                "Bot",
                "red",
                botSpawn,
                new MonoBehaviour[] { botController });
            botController.Configure(botParticipant, playerParticipant);

            var systems = new GameObject("PrototypeSystems");
            systems.transform.SetParent(root);
            var match = systems.AddComponent<PrototypeMatchController>();
            match.Configure(playerParticipant, botParticipant, 5, 180f, 2f);
            var hud = systems.AddComponent<PrototypeHud>();
            hud.Configure(match, playerParticipant);
            playerInput.Configure(playerParticipant, playerMotor, playerLook, playerWeapon, aimSource, match);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            EditorGUIUtility.PingObject(Selection.activeObject);
            Debug.Log("Playable prototype created at " + ScenePath + ". Open it and press Play.");
        }

        private static GameObject CreatePlayerPrefab()
        {
            var source = new GameObject("LocalPlayer");
            var controller = source.AddComponent<CharacterController>();
            controller.center = Vector3.up;
            controller.height = 2f;
            controller.radius = 0.42f;
            source.AddComponent<Health>();
            var motor = source.AddComponent<PlayerMotor>();
            var look = source.AddComponent<FirstPersonLook>();
            var shotSink = source.AddComponent<LocalAuthoritativeShotSink>();
            var weapon = source.AddComponent<HitscanWeapon>();
            var participant = source.AddComponent<PrototypeParticipant>();
            var input = source.AddComponent<PrototypeInputDriver>();

            var view = new GameObject("ViewPivot").transform;
            view.SetParent(source.transform, false);
            view.localPosition = new Vector3(0f, 1.62f, 0f);
            var camera = view.gameObject.AddComponent<Camera>();
            camera.fieldOfView = 75f;
            camera.nearClipPlane = 0.05f;
            view.gameObject.AddComponent<AudioListener>();
            view.gameObject.tag = "MainCamera";

            look.Configure(view);
            weapon.Configure("local-player", view, shotSink);
            participant.Configure("local-player", "You", "blue", null, new MonoBehaviour[] { motor, look, input });
            input.Configure(participant, motor, look, weapon, view, null);
            var prefab = PrefabUtility.SaveAsPrefabAsset(source, PrefabFolder + "/LocalPlayer.prefab");
            Object.DestroyImmediate(source);
            return prefab;
        }

        private static GameObject CreateBotPrefab(Material botMaterial)
        {
            var source = new GameObject("PrototypeBot");
            var controller = source.AddComponent<CharacterController>();
            controller.center = Vector3.up;
            controller.height = 2f;
            controller.radius = 0.5f;
            source.AddComponent<Health>();
            var botController = source.AddComponent<PrototypeBotController>();
            var participant = source.AddComponent<PrototypeParticipant>();
            participant.Configure("prototype-bot", "Bot", "red", null, new MonoBehaviour[] { botController });
            botController.Configure(participant, null);

            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(source.transform, false);
            body.transform.localPosition = Vector3.up;
            var primitiveCollider = body.GetComponent<Collider>();
            if (primitiveCollider != null) Object.DestroyImmediate(primitiveCollider);
            body.GetComponent<Renderer>().sharedMaterial = botMaterial;

            var prefab = PrefabUtility.SaveAsPrefabAsset(source, PrefabFolder + "/PrototypeBot.prefab");
            Object.DestroyImmediate(source);
            return prefab;
        }

        private static void CreateLighting(Transform root)
        {
            var lightObject = new GameObject("Directional Light");
            lightObject.transform.SetParent(root);
            lightObject.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
        }

        private static void CreateArena(Transform root, Material floorMaterial, Material wallMaterial)
        {
            var arena = new GameObject("Arena").transform;
            arena.SetParent(root);
            CreateCube("Floor", new Vector3(0f, -0.5f, 0f), new Vector3(24f, 1f, 24f), floorMaterial, arena);
            CreateCube("NorthWall", new Vector3(0f, 1.5f, 12f), new Vector3(24f, 3f, 1f), wallMaterial, arena);
            CreateCube("SouthWall", new Vector3(0f, 1.5f, -12f), new Vector3(24f, 3f, 1f), wallMaterial, arena);
            CreateCube("EastWall", new Vector3(12f, 1.5f, 0f), new Vector3(1f, 3f, 24f), wallMaterial, arena);
            CreateCube("WestWall", new Vector3(-12f, 1.5f, 0f), new Vector3(1f, 3f, 24f), wallMaterial, arena);
            CreateCube("CoverA", new Vector3(-2f, 1f, 3f), new Vector3(3f, 2f, 1f), wallMaterial, arena);
            CreateCube("CoverB", new Vector3(2f, 1f, -3f), new Vector3(3f, 2f, 1f), wallMaterial, arena);
        }

        private static GameObject CreateCube(
            string name,
            Vector3 position,
            Vector3 scale,
            Material material,
            Transform parent)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent);
            cube.transform.position = position;
            cube.transform.localScale = scale;
            cube.GetComponent<Renderer>().sharedMaterial = material;
            return cube;
        }

        private static Transform CreateSpawn(string name, Vector3 position, Quaternion rotation, Transform parent)
        {
            var spawn = new GameObject(name).transform;
            spawn.SetParent(parent);
            spawn.SetPositionAndRotation(position, rotation);
            return spawn;
        }

        private static Material CreateMaterial(string path, Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = new Material(shader) { color = color };
            AssetDatabase.CreateAsset(material, path);
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

        private static void AddSceneToBuildSettings(string scenePath)
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.Any(x => x.path == scenePath)) return;
            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
