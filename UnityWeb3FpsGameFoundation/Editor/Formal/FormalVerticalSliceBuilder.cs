using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using Web3Fps.GameFoundation.Composition;
using Web3Fps.GameFoundation.Formal;
using Web3Fps.GameFoundation.Gameplay;
using Web3Fps.GameFoundation.Gameplay.Combat;
using Web3Fps.GameFoundation.Lobby;
using Web3Fps.GameFoundation.Prototype;
using Object = UnityEngine.Object;

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
        private const string LobbyUxml = "Packages/com.web3fps.game-foundation/Runtime/Formal/UI/AshLedgerLobby.uxml";
        private const string CombatUxml = "Packages/com.web3fps.game-foundation/Runtime/Formal/UI/AshLedgerCombatHud.uxml";
        private const string BackdropTexture = "Packages/com.web3fps.game-foundation/Runtime/Formal/Art/RiftRelayOrbitalBackdrop.png";
        private const string ArmorTexture = "Packages/com.web3fps.game-foundation/Runtime/Formal/Art/OperatorArmorSurface.png";

        [MenuItem("Tools/Web3 FPS/Create ASH LEDGER Vertical Slice")]
        public static void Create()
        {
            if (AssetDatabase.IsValidFolder(Root) && !EditorUtility.DisplayDialog(
                    "Rebuild ASH//LEDGER Vertical Slice",
                    "The generated Assets/AshLedgerVerticalSlice folder will be replaced.",
                    "Replace",
                    "Cancel")) return;

            if (AssetDatabase.IsValidFolder(Root)) AssetDatabase.DeleteAsset(Root);
            EnsureFolder(Materials);
            EnsureFolder(Prefabs);
            EnsureFolder(Scenes);

            var palette = CreatePalette();
            var weapons = CreateWeaponPrefabs(palette);
            var playerPrefab = CreateFormalPlayerPrefab();
            var botPrefab = CreateFormalBotPrefab(palette, weapons[0]);
            var panelSettings = CreatePanelSettings();
            CreateLobbyScene(palette, weapons, panelSettings);
            CreateRiftRelayScene(palette, weapons[0], playerPrefab, botPrefab, panelSettings);
            SetBuildScenes();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(LobbyScene);
            Selection.activeObject = sceneAsset;
            EditorGUIUtility.PingObject(sceneAsset);
            EditorSceneManager.OpenScene(LobbyScene);
            Debug.Log("ASH//LEDGER vertical slice created. Lobby is open; press Play, then select DEPLOY TO RIFT RELAY.");
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
                ["aurora"] = CreateEmissiveMaterial("Aurora", new Color(0.18f, 0.88f, 0.66f), 2.4f),
                ["cobaltGlow"] = CreateEmissiveMaterial("CobaltGlow", new Color(0.06f, 0.42f, 0.78f), 1.8f),
                ["coralGlow"] = CreateEmissiveMaterial("CoralGlow", new Color(0.88f, 0.16f, 0.13f), 1.8f),
                ["warmGlow"] = CreateEmissiveMaterial("WarmGlow", new Color(0.94f, 0.48f, 0.18f), 2.1f),
                ["armor"] = CreateTexturedMaterial("OperatorArmor", new Color(0.86f, 0.85f, 0.78f), ArmorTexture, 0.42f),
                ["backdrop"] = CreateBackdropMaterial()
            };
        }

        private static GameObject[] CreateWeaponPrefabs(IReadOnlyDictionary<string, Material> palette)
        {
            return new[]
            {
                CreateWeapon("KESTREL-7", new Vector3(1.18f, 0.18f, 0.16f), palette["cobalt"], palette["graphite"], palette["bone"], true),
                CreateWeapon("PULSE-9", new Vector3(0.82f, 0.22f, 0.18f), palette["coral"], palette["graphite"], palette["bone"], true),
                CreateWeapon("RELAY-3", new Vector3(0.58f, 0.17f, 0.13f), palette["aurora"], palette["graphite"], palette["bone"], false)
            };
        }

        private static GameObject CreateWeapon(
            string name,
            Vector3 receiverScale,
            Material accent,
            Material dark,
            Material shell,
            bool stock)
        {
            var root = new GameObject(name);
            CreatePart("Receiver", Vector3.zero, receiverScale, shell, root.transform);
            CreatePart("LowerReceiver", new Vector3(-0.08f, -0.09f, 0f), new Vector3(receiverScale.x * 0.72f, receiverScale.y * 0.62f, receiverScale.z * 1.08f), dark, root.transform);
            CreatePart("AccentRail", new Vector3(0f, 0.12f, 0f), new Vector3(receiverScale.x * 0.62f, 0.035f, 0.19f), accent, root.transform);
            CreatePart("BarrelShroud", new Vector3(receiverScale.x * 0.57f, 0.015f, 0f), new Vector3(receiverScale.x * 0.32f, 0.12f, 0.12f), dark, root.transform);
            CreatePrimitive("Muzzle", PrimitiveType.Cylinder, new Vector3(receiverScale.x * 0.8f, 0.015f, 0f), new Vector3(0.075f, receiverScale.x * 0.18f, 0.075f), dark, root.transform, new Vector3(0f, 0f, 90f));
            CreatePart("Grip", new Vector3(-receiverScale.x * 0.16f, -0.2f, 0f), new Vector3(0.12f, 0.28f, 0.12f), dark, root.transform, new Vector3(0f, 0f, -12f));
            CreatePart("Magazine", new Vector3(0.12f, -0.18f, 0f), new Vector3(0.13f, 0.27f, 0.12f), accent, root.transform, new Vector3(0f, 0f, 7f));
            CreatePart("RearSight", new Vector3(-receiverScale.x * 0.22f, 0.18f, 0f), new Vector3(0.06f, 0.09f, 0.18f), dark, root.transform);
            CreatePart("FrontSight", new Vector3(receiverScale.x * 0.42f, 0.17f, 0f), new Vector3(0.045f, 0.08f, 0.16f), dark, root.transform);
            if (stock)
            {
                CreatePart("StockArm", new Vector3(-receiverScale.x * 0.67f, -0.015f, 0f), new Vector3(receiverScale.x * 0.34f, 0.08f, 0.11f), dark, root.transform);
                CreatePart("StockPad", new Vector3(-receiverScale.x * 0.88f, -0.015f, 0f), new Vector3(0.12f, 0.28f, 0.15f), shell, root.transform);
            }
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, Prefabs + "/" + name + ".prefab");
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject CreateFormalPlayerPrefab()
        {
            var source = new GameObject("CobaltOperator");
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
            camera.fieldOfView = 72f;
            camera.nearClipPlane = 0.03f;
            view.gameObject.AddComponent<AudioListener>();
            view.gameObject.tag = "MainCamera";

            look.Configure(view);
            weapon.Configure("local-player", view, shotSink);
            participant.Configure("local-player", "Cobalt", "cobalt", null, new MonoBehaviour[] { motor, look, input });
            input.Configure(participant, motor, look, weapon, view, null);
            var prefab = PrefabUtility.SaveAsPrefabAsset(source, Prefabs + "/CobaltOperator.prefab");
            Object.DestroyImmediate(source);
            return prefab;
        }

        private static GameObject CreateFormalBotPrefab(
            IReadOnlyDictionary<string, Material> palette,
            GameObject weaponPrefab)
        {
            var source = new GameObject("CoralOperator");
            var controller = source.AddComponent<CharacterController>();
            controller.center = Vector3.up;
            controller.height = 2f;
            controller.radius = 0.48f;
            source.AddComponent<Health>();
            var botController = source.AddComponent<PrototypeBotController>();
            var participant = source.AddComponent<PrototypeParticipant>();
            participant.Configure("prototype-bot", "Coral", "coral", null, new MonoBehaviour[] { botController });
            botController.Configure(participant, null);

            var visual = new GameObject("OperatorVisual").transform;
            visual.SetParent(source.transform, false);
            CreateVisualPrimitive("Torso", PrimitiveType.Capsule, new Vector3(0f, 1.35f, 0f), new Vector3(0.55f, 0.72f, 0.38f), palette["graphite"], visual);
            CreateVisualPrimitive("NeckSeal", PrimitiveType.Cylinder, new Vector3(0f, 1.9f, 0f), new Vector3(0.4f, 0.13f, 0.4f), palette["copper"], visual);
            CreateVisualPrimitive("ChestCeramic", PrimitiveType.Cube, new Vector3(0f, 1.55f, 0.34f), new Vector3(0.76f, 0.64f, 0.18f), palette["armor"], visual, new Vector3(5f, 0f, 0f));
            CreateVisualPrimitive("ChestInset", PrimitiveType.Cube, new Vector3(0f, 1.49f, 0.455f), new Vector3(0.44f, 0.28f, 0.045f), palette["graphite"], visual);
            CreateVisualPrimitive("UpperBackPlate", PrimitiveType.Cube, new Vector3(0f, 1.57f, -0.34f), new Vector3(0.68f, 0.55f, 0.15f), palette["armor"], visual);
            CreateVisualPrimitive("Helmet", PrimitiveType.Sphere, new Vector3(0f, 2.2f, 0f), new Vector3(0.6f, 0.54f, 0.58f), palette["armor"], visual);
            CreateVisualPrimitive("HelmetJaw", PrimitiveType.Cube, new Vector3(0f, 2.02f, 0.1f), new Vector3(0.48f, 0.24f, 0.42f), palette["graphite"], visual);
            CreateVisualPrimitive("Faceplate", PrimitiveType.Cube, new Vector3(0f, 2.2f, 0.47f), new Vector3(0.48f, 0.23f, 0.08f), palette["graphite"], visual);
            CreateVisualPrimitive("VisorSignal", PrimitiveType.Cube, new Vector3(0f, 2.23f, 0.52f), new Vector3(0.34f, 0.07f, 0.025f), palette["coralGlow"], visual);
            CreateVisualPrimitive("LeftCommsPod", PrimitiveType.Cylinder, new Vector3(-0.47f, 2.2f, 0f), new Vector3(0.13f, 0.1f, 0.13f), palette["copper"], visual, new Vector3(0f, 0f, 90f));
            CreateVisualPrimitive("RightCommsPod", PrimitiveType.Cylinder, new Vector3(0.47f, 2.2f, 0f), new Vector3(0.13f, 0.1f, 0.13f), palette["copper"], visual, new Vector3(0f, 0f, 90f));
            CreateVisualPrimitive("LeftShoulder", PrimitiveType.Cube, new Vector3(-0.63f, 1.68f, 0f), new Vector3(0.4f, 0.34f, 0.52f), palette["armor"], visual, new Vector3(0f, 0f, -7f));
            CreateVisualPrimitive("RightShoulder", PrimitiveType.Cube, new Vector3(0.63f, 1.68f, 0f), new Vector3(0.4f, 0.34f, 0.52f), palette["armor"], visual, new Vector3(0f, 0f, 7f));
            CreateVisualPrimitive("LeftArm", PrimitiveType.Capsule, new Vector3(-0.68f, 1.08f, 0.08f), new Vector3(0.22f, 0.48f, 0.22f), palette["graphite"], visual, new Vector3(22f, 0f, 0f));
            CreateVisualPrimitive("RightArm", PrimitiveType.Capsule, new Vector3(0.68f, 1.08f, 0.08f), new Vector3(0.22f, 0.48f, 0.22f), palette["graphite"], visual, new Vector3(22f, 0f, 0f));
            CreateVisualPrimitive("LeftForearmPlate", PrimitiveType.Cube, new Vector3(-0.68f, 0.98f, 0.28f), new Vector3(0.3f, 0.46f, 0.2f), palette["armor"], visual, new Vector3(22f, 0f, 0f));
            CreateVisualPrimitive("RightForearmPlate", PrimitiveType.Cube, new Vector3(0.68f, 0.98f, 0.28f), new Vector3(0.3f, 0.46f, 0.2f), palette["armor"], visual, new Vector3(22f, 0f, 0f));
            CreateVisualPrimitive("LeftGlove", PrimitiveType.Sphere, new Vector3(-0.42f, 1.02f, 0.66f), new Vector3(0.22f, 0.18f, 0.24f), palette["graphite"], visual);
            CreateVisualPrimitive("RightGlove", PrimitiveType.Sphere, new Vector3(0.34f, 1.04f, 0.72f), new Vector3(0.22f, 0.18f, 0.24f), palette["graphite"], visual);
            CreateVisualPrimitive("Pelvis", PrimitiveType.Cube, new Vector3(0f, 0.72f, 0f), new Vector3(0.62f, 0.38f, 0.4f), palette["graphite"], visual);
            CreateVisualPrimitive("Belt", PrimitiveType.Cube, new Vector3(0f, 0.83f, 0.04f), new Vector3(0.78f, 0.16f, 0.46f), palette["copper"], visual);
            CreateVisualPrimitive("LeftBeltPouch", PrimitiveType.Cube, new Vector3(-0.48f, 0.72f, 0.28f), new Vector3(0.22f, 0.28f, 0.16f), palette["graphite"], visual);
            CreateVisualPrimitive("RightBeltPouch", PrimitiveType.Cube, new Vector3(0.48f, 0.72f, 0.28f), new Vector3(0.22f, 0.28f, 0.16f), palette["graphite"], visual);
            CreateVisualPrimitive("LeftLeg", PrimitiveType.Capsule, new Vector3(-0.25f, 0.26f, 0f), new Vector3(0.25f, 0.48f, 0.25f), palette["graphite"], visual);
            CreateVisualPrimitive("RightLeg", PrimitiveType.Capsule, new Vector3(0.25f, 0.26f, 0f), new Vector3(0.25f, 0.48f, 0.25f), palette["graphite"], visual);
            CreateVisualPrimitive("LeftKnee", PrimitiveType.Cube, new Vector3(-0.25f, 0.36f, 0.24f), new Vector3(0.3f, 0.32f, 0.16f), palette["armor"], visual);
            CreateVisualPrimitive("RightKnee", PrimitiveType.Cube, new Vector3(0.25f, 0.36f, 0.24f), new Vector3(0.3f, 0.32f, 0.16f), palette["armor"], visual);
            CreateVisualPrimitive("LeftBoot", PrimitiveType.Cube, new Vector3(-0.25f, 0.05f, 0.14f), new Vector3(0.32f, 0.22f, 0.52f), palette["graphite"], visual);
            CreateVisualPrimitive("RightBoot", PrimitiveType.Cube, new Vector3(0.25f, 0.05f, 0.14f), new Vector3(0.32f, 0.22f, 0.52f), palette["graphite"], visual);
            CreateVisualPrimitive("ArchivePack", PrimitiveType.Cube, new Vector3(0f, 1.43f, -0.5f), new Vector3(0.54f, 0.72f, 0.24f), palette["slate"], visual);
            CreateVisualPrimitive("PackBeacon", PrimitiveType.Cylinder, new Vector3(0.28f, 1.98f, -0.48f), new Vector3(0.08f, 0.32f, 0.08f), palette["coralGlow"], visual);
            CreateVisualPrimitive("CoralBand", PrimitiveType.Cube, new Vector3(0f, 1.42f, 0.54f), new Vector3(0.5f, 0.08f, 0.035f), palette["coralGlow"], visual);

            var heldWeapon = Object.Instantiate(weaponPrefab, visual);
            heldWeapon.name = "KESTREL-7 // Bot Weapon";
            heldWeapon.transform.localPosition = new Vector3(0.25f, 1.18f, 0.7f);
            heldWeapon.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
            heldWeapon.transform.localScale = Vector3.one * 0.72f;

            var prefab = PrefabUtility.SaveAsPrefabAsset(source, Prefabs + "/CoralOperator.prefab");
            Object.DestroyImmediate(source);
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

        private static void ConfigureDocument(
            UIDocument document,
            PanelSettings panelSettings,
            VisualTreeAsset visualTree)
        {
            if (visualTree == null) throw new InvalidOperationException("The generated UI document is missing its UXML asset.");
            document.enabled = false;
            document.panelSettings = panelSettings;
            document.visualTreeAsset = visualTree;

            // Unity 6 can discard PanelSettings when UIDocument is added and configured
            // in the same editor frame. Persist both references explicitly.
            var serialized = new SerializedObject(document);
            serialized.FindProperty("m_PanelSettings").objectReferenceValue = panelSettings;
            serialized.FindProperty("sourceAsset").objectReferenceValue = visualTree;
            serialized.FindProperty("m_SortingOrder").floatValue = 20f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(document);
            document.enabled = true;
        }

        private static void CreateLobbyScene(
            IReadOnlyDictionary<string, Material> palette,
            IReadOnlyList<GameObject> weapons,
            PanelSettings panelSettings)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = FormalContentCatalog.LobbySceneName;
            var root = new GameObject("=== ASH LEDGER // ORBITAL ARCHIVE ===").transform;
            ConfigureAtmosphere(new Color(0.008f, 0.014f, 0.02f), 0.008f);
            CreateCamera("Main Camera", new Vector3(0f, 2.8f, -11.5f), new Vector3(7f, 0f, 0f), new Color(0.008f, 0.014f, 0.02f), root);
            CreateLight("Orbital Key", new Vector3(42f, -32f, 0f), 1.65f, root);
            CreateBackdrop("Broken Ring Vista", new Vector3(0f, 4.4f, 10.5f), new Vector3(23f, 12.8f, 1f), new Vector3(0f, 180f, 0f), palette["backdrop"], root);

            CreatePart("ArchiveDeck", new Vector3(0f, -0.3f, 1f), new Vector3(18f, 0.4f, 11f), palette["graphite"], root);
            CreatePart("LeftArchivePylon", new Vector3(-8.3f, 3.3f, 5.5f), new Vector3(1.1f, 7.2f, 1.7f), palette["slate"], root);
            CreatePart("RightArchivePylon", new Vector3(8.3f, 3.3f, 5.5f), new Vector3(1.1f, 7.2f, 1.7f), palette["slate"], root);
            CreatePart("OverheadTruss", new Vector3(0f, 7f, 5.5f), new Vector3(17.5f, 0.45f, 1.25f), palette["bone"], root);
            for (var i = -4; i <= 4; i++)
            {
                CreatePart("DeckRib_" + i, new Vector3(i * 2f, -0.04f, 1f), new Vector3(0.04f, 0.05f, 10.5f), palette["copper"], root);
                CreatePart("TrussLight_" + i, new Vector3(i * 1.85f, 6.7f, 4.85f), new Vector3(0.72f, 0.08f, 0.08f), palette["warmGlow"], root);
            }
            CreatePart("CobaltStageLine", new Vector3(-3.5f, -0.03f, -1.5f), new Vector3(5.5f, 0.04f, 0.08f), palette["cobaltGlow"], root);
            CreatePart("AuroraStageLine", new Vector3(3.5f, -0.03f, -1.5f), new Vector3(5.5f, 0.04f, 0.08f), palette["aurora"], root);
            CreatePointLight("Operator Rim", new Vector3(4.6f, 4.3f, -0.5f), new Color(0.33f, 0.68f, 1f), 5.5f, 11f, root);
            CreatePointLight("Archive Warm Light", new Vector3(-5f, 2.2f, 2f), new Color(1f, 0.38f, 0.15f), 3.2f, 9f, root);

            var operatorRoot = new GameObject("ArchiveOperator").transform;
            operatorRoot.SetParent(root);
            operatorRoot.position = new Vector3(3.6f, 0f, 1.2f);
            CreatePrimitive("Body", PrimitiveType.Capsule, new Vector3(0f, 1.15f, 0f), new Vector3(0.72f, 1.15f, 0.48f), palette["bone"], operatorRoot);
            CreatePrimitive("Helmet", PrimitiveType.Sphere, new Vector3(0f, 2.35f, 0f), new Vector3(0.62f, 0.52f, 0.58f), palette["graphite"], operatorRoot);
            CreatePart("Visor", new Vector3(0f, 2.38f, -0.48f), new Vector3(0.48f, 0.13f, 0.04f), palette["aurora"], operatorRoot);
            var heldWeapon = Object.Instantiate(weapons[0], operatorRoot);
            heldWeapon.name = "KESTREL-7 // Equipped";
            heldWeapon.transform.localPosition = new Vector3(-0.35f, 1.25f, -0.7f);
            heldWeapon.transform.localRotation = Quaternion.Euler(0f, -90f, -8f);

            for (var i = 0; i < weapons.Count; i++)
            {
                var display = Object.Instantiate(weapons[i], root);
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
            ConfigureDocument(document, panelSettings, AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(LobbyUxml));
            var view = systems.AddComponent<FormalLobbyView>();
            view.Configure(lobby, document, FormalContentCatalog.RiftRelaySceneName);

            SaveGeneratedScene(scene, LobbyScene);
        }

        private static void CreateRiftRelayScene(
            IReadOnlyDictionary<string, Material> palette,
            GameObject viewWeapon,
            GameObject playerSource,
            GameObject botSource,
            PanelSettings panelSettings)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = FormalContentCatalog.RiftRelaySceneName;
            var root = new GameObject("=== RIFT RELAY // COMBAT DECK 07 ===").transform;
            ConfigureAtmosphere(new Color(0.02f, 0.028f, 0.038f), 0.006f);
            CreateLight("Orbital Sun", new Vector3(52f, -28f, 0f), 1.35f, root);
            CreateRelayArena(root, palette);

            var spawnRoot = new GameObject("SpawnPoints").transform;
            spawnRoot.SetParent(root);
            var playerSpawn = CreateSpawn("CobaltSpawn", new Vector3(-18f, 0f, 0f), Quaternion.LookRotation(Vector3.right), spawnRoot);
            var botSpawn = CreateSpawn("CoralSpawn", new Vector3(18f, 0f, 0f), Quaternion.LookRotation(Vector3.left), spawnRoot);
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

            var held = Object.Instantiate(viewWeapon, aimSource);
            held.name = "KESTREL-7 // View Model";
            held.transform.localPosition = new Vector3(0.42f, -0.31f, 0.78f);
            held.transform.localRotation = Quaternion.Euler(2f, 2f, 0f);
            held.transform.localScale = Vector3.one * 0.72f;
            CreateFirstPersonArms(aimSource, palette);

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
            ConfigureDocument(document, panelSettings, AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(CombatUxml));
            var hud = systems.AddComponent<FormalCombatHud>();
            hud.Configure(document, match, playerParticipant);

            SaveGeneratedScene(scene, RelayScene);
        }

        private static void CreateRelayArena(Transform root, IReadOnlyDictionary<string, Material> palette)
        {
            var arena = new GameObject("RiftRelayGeometry").transform;
            arena.SetParent(root);
            var vista = new GameObject("OrbitalVista").transform;
            vista.SetParent(root);
            CreateBackdrop("NorthVista", new Vector3(0f, 17f, 49f), new Vector3(88f, 49f, 1f), new Vector3(0f, 180f, 0f), palette["backdrop"], vista);
            CreateBackdrop("SouthVista", new Vector3(0f, 17f, -49f), new Vector3(88f, 49f, 1f), Vector3.zero, palette["backdrop"], vista);
            CreateBackdrop("EastVista", new Vector3(62f, 17f, 0f), new Vector3(88f, 49f, 1f), new Vector3(0f, -90f, 0f), palette["backdrop"], vista);
            CreateBackdrop("WestVista", new Vector3(-62f, 17f, 0f), new Vector3(88f, 49f, 1f), new Vector3(0f, 90f, 0f), palette["backdrop"], vista);

            CreatePart("Deck", new Vector3(0f, -0.5f, 0f), new Vector3(48f, 1f, 32f), palette["graphite"], arena);
            CreatePart("NorthSafetyWall", new Vector3(0f, 0.65f, 15.6f), new Vector3(48f, 1.3f, 0.65f), palette["slate"], arena);
            CreatePart("SouthSafetyWall", new Vector3(0f, 0.65f, -15.6f), new Vector3(48f, 1.3f, 0.65f), palette["slate"], arena);
            CreatePart("WestSafetyWall", new Vector3(-23.6f, 0.65f, 0f), new Vector3(0.65f, 1.3f, 32f), palette["slate"], arena);
            CreatePart("EastSafetyWall", new Vector3(23.6f, 0.65f, 0f), new Vector3(0.65f, 1.3f, 32f), palette["slate"], arena);
            CreateInvisibleBlocker("NorthBlocker", new Vector3(0f, 3f, 16.1f), new Vector3(49f, 6f, 0.5f), arena);
            CreateInvisibleBlocker("SouthBlocker", new Vector3(0f, 3f, -16.1f), new Vector3(49f, 6f, 0.5f), arena);
            CreateInvisibleBlocker("WestBlocker", new Vector3(-24.1f, 3f, 0f), new Vector3(0.5f, 6f, 33f), arena);
            CreateInvisibleBlocker("EastBlocker", new Vector3(24.1f, 3f, 0f), new Vector3(0.5f, 6f, 33f), arena);

            for (var x = -20; x <= 20; x += 4)
            {
                CreatePart("DeckSeamNorth_" + x, new Vector3(x, 0.015f, 5.35f), new Vector3(0.035f, 0.025f, 9.8f), palette["slate"], arena);
                CreatePart("DeckSeamSouth_" + x, new Vector3(x, 0.015f, -5.35f), new Vector3(0.035f, 0.025f, 9.8f), palette["slate"], arena);
            }
            CreatePart("CobaltLaneLight", new Vector3(0f, 0.035f, 10.2f), new Vector3(39f, 0.045f, 0.16f), palette["cobaltGlow"], arena);
            CreatePart("CoralLaneLight", new Vector3(0f, 0.035f, -10.2f), new Vector3(39f, 0.045f, 0.16f), palette["coralGlow"], arena);
            CreatePart("CenterLaneLightA", new Vector3(-12f, 0.035f, 0f), new Vector3(8f, 0.045f, 0.1f), palette["aurora"], arena);
            CreatePart("CenterLaneLightB", new Vector3(12f, 0.035f, 0f), new Vector3(8f, 0.045f, 0.1f), palette["aurora"], arena);

            var relay = new GameObject("CentralProofRelay").transform;
            relay.SetParent(arena);
            CreatePrimitive("RelayPlinth", PrimitiveType.Cylinder, new Vector3(0f, 0.35f, 0f), new Vector3(3.5f, 0.35f, 3.5f), palette["slate"], relay);
            CreatePrimitive("RelayCore", PrimitiveType.Cylinder, new Vector3(0f, 3.1f, 0f), new Vector3(1.15f, 3.1f, 1.15f), palette["bone"], relay);
            CreatePrimitive("DataSpine", PrimitiveType.Cylinder, new Vector3(0f, 4.4f, 0f), new Vector3(0.38f, 4.4f, 0.38f), palette["aurora"], relay);
            for (var i = 0; i < 4; i++)
            {
                var angle = i * 90f;
                var radians = angle * Mathf.Deg2Rad;
                var p = new Vector3(Mathf.Cos(radians) * 2.5f, 2.3f, Mathf.Sin(radians) * 2.5f);
                CreatePart("RelayFin_" + i, p, new Vector3(0.3f, 3.8f, 1.3f), palette["copper"], relay, new Vector3(0f, -angle, 0f));
            }
            CreatePart("RelayHaloA", new Vector3(0f, 6.5f, 0f), new Vector3(8f, 0.12f, 0.26f), palette["aurora"], relay);
            CreatePart("RelayHaloB", new Vector3(0f, 6.5f, 0f), new Vector3(0.26f, 0.12f, 8f), palette["aurora"], relay);
            CreatePointLight("RelayGlow", new Vector3(0f, 5.2f, 0f), new Color(0.2f, 1f, 0.72f), 6.5f, 16f, relay);

            CreateExteriorTower("NorthWestArchive", new Vector3(-18f, 0f, 18f), 9f, palette, arena);
            CreateExteriorTower("NorthEastArchive", new Vector3(18f, 0f, 18f), 12f, palette, arena);
            CreateExteriorTower("SouthWestArchive", new Vector3(-18f, 0f, -18f), 11f, palette, arena);
            CreateExteriorTower("SouthEastArchive", new Vector3(18f, 0f, -18f), 8f, palette, arena);

            for (var x = -18; x <= 18; x += 6)
            {
                CreatePart("NorthRailPost_" + x, new Vector3(x, 1.65f, 15.25f), new Vector3(0.12f, 2f, 0.12f), palette["bone"], arena);
                CreatePart("SouthRailPost_" + x, new Vector3(x, 1.65f, -15.25f), new Vector3(0.12f, 2f, 0.12f), palette["bone"], arena);
            }
            CreatePart("NorthRail", new Vector3(0f, 2.2f, 15.25f), new Vector3(42f, 0.1f, 0.1f), palette["copper"], arena);
            CreatePart("SouthRail", new Vector3(0f, 2.2f, -15.25f), new Vector3(42f, 0.1f, 0.1f), palette["copper"], arena);

            var covers = new[]
            {
                new Vector3(-12f, 1f, 4.5f), new Vector3(-12f, 1f, -4.5f),
                new Vector3(-5f, 1f, 8f), new Vector3(-5f, 1f, -8f),
                new Vector3(5f, 1f, 8f), new Vector3(5f, 1f, -8f),
                new Vector3(12f, 1f, 4.5f), new Vector3(12f, 1f, -4.5f)
            };
            for (var i = 0; i < covers.Length; i++)
            {
                CreatePart("CoverShell_" + i.ToString("00"), covers[i], new Vector3(3f, 2f, 1.2f), palette["bone"], arena);
                var accent = i % 2 == 0 ? palette["cobaltGlow"] : palette["coralGlow"];
                CreatePart("CoverSignal_" + i.ToString("00"), covers[i] + new Vector3(0f, 0.72f, -0.62f), new Vector3(2.2f, 0.14f, 0.04f), accent, arena);
            }

            CreatePointLight("NorthWorkLight", new Vector3(-13f, 4.2f, 12.5f), new Color(0.28f, 0.58f, 1f), 4.2f, 12f, arena);
            CreatePointLight("SouthWorkLight", new Vector3(13f, 4.2f, -12.5f), new Color(1f, 0.31f, 0.16f), 4.2f, 12f, arena);
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

        private static void ConfigureAtmosphere(Color fogColor, float density)
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.105f, 0.13f, 0.16f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogDensity = density;
        }

        private static void CreateBackdrop(
            string name,
            Vector3 position,
            Vector3 scale,
            Vector3 euler,
            Material material,
            Transform parent)
        {
            var backdrop = CreatePrimitive(name, PrimitiveType.Quad, position, scale, material, parent, euler);
            var collider = backdrop.GetComponent<Collider>();
            if (collider != null) Object.DestroyImmediate(collider);
            var renderer = backdrop.GetComponent<Renderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }

        private static void CreatePointLight(
            string name,
            Vector3 position,
            Color color,
            float intensity,
            float range,
            Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.Soft;
        }

        private static void CreateExteriorTower(
            string name,
            Vector3 position,
            float height,
            IReadOnlyDictionary<string, Material> palette,
            Transform parent)
        {
            var tower = new GameObject(name).transform;
            tower.SetParent(parent, false);
            tower.localPosition = position;
            CreatePart("StructuralCore", new Vector3(0f, height * 0.5f, 0f), new Vector3(4.6f, height, 4.6f), palette["slate"], tower);
            CreatePart("CeramicCrown", new Vector3(0f, height + 0.8f, 0f), new Vector3(5.6f, 1.7f, 5.6f), palette["bone"], tower);
            CreatePart("ServiceBandA", new Vector3(0f, height * 0.35f, -2.36f), new Vector3(3.4f, 0.16f, 0.08f), palette["warmGlow"], tower);
            CreatePart("ServiceBandB", new Vector3(0f, height * 0.68f, -2.36f), new Vector3(3.4f, 0.16f, 0.08f), palette["warmGlow"], tower);
            CreatePrimitive("ArchiveAntenna", PrimitiveType.Cylinder, new Vector3(0f, height + 3f, 0f), new Vector3(0.22f, 2.2f, 0.22f), palette["copper"], tower);
            CreatePart("AntennaSignal", new Vector3(0f, height + 5.1f, 0f), new Vector3(0.5f, 0.12f, 0.5f), palette["aurora"], tower);
        }

        private static void CreateInvisibleBlocker(string name, Vector3 position, Vector3 scale, Transform parent)
        {
            var blocker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blocker.name = name;
            blocker.transform.SetParent(parent, false);
            blocker.transform.localPosition = position;
            blocker.transform.localScale = scale;
            blocker.GetComponent<Renderer>().enabled = false;
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

        private static void CreateFirstPersonArms(
            Transform camera,
            IReadOnlyDictionary<string, Material> palette)
        {
            var rig = new GameObject("FirstPersonArms").transform;
            rig.SetParent(camera, false);
            CreateVisualPrimitive("LeftSleeve", PrimitiveType.Capsule, new Vector3(-0.23f, -0.38f, 0.6f), new Vector3(0.13f, 0.34f, 0.13f), palette["graphite"], rig, new Vector3(68f, 0f, -16f));
            CreateVisualPrimitive("RightSleeve", PrimitiveType.Capsule, new Vector3(0.25f, -0.39f, 0.63f), new Vector3(0.13f, 0.36f, 0.13f), palette["graphite"], rig, new Vector3(66f, 0f, 15f));
            CreateVisualPrimitive("LeftForearmArmor", PrimitiveType.Cube, new Vector3(-0.18f, -0.31f, 0.73f), new Vector3(0.18f, 0.28f, 0.18f), palette["armor"], rig, new Vector3(18f, 0f, -10f));
            CreateVisualPrimitive("RightForearmArmor", PrimitiveType.Cube, new Vector3(0.3f, -0.31f, 0.76f), new Vector3(0.18f, 0.3f, 0.18f), palette["armor"], rig, new Vector3(16f, 0f, 10f));
            CreateVisualPrimitive("LeftGlove", PrimitiveType.Sphere, new Vector3(-0.1f, -0.23f, 0.84f), new Vector3(0.14f, 0.12f, 0.17f), palette["graphite"], rig);
            CreateVisualPrimitive("RightGlove", PrimitiveType.Sphere, new Vector3(0.36f, -0.24f, 0.88f), new Vector3(0.14f, 0.12f, 0.17f), palette["graphite"], rig);
            CreateVisualPrimitive("CobaltWristSignal", PrimitiveType.Cube, new Vector3(0.32f, -0.29f, 0.86f), new Vector3(0.12f, 0.035f, 0.09f), palette["cobaltGlow"], rig);
        }

        private static GameObject CreatePart(string name, Vector3 position, Vector3 scale, Material material, Transform parent, Vector3? euler = null)
        {
            return CreatePrimitive(name, PrimitiveType.Cube, position, scale, material, parent, euler);
        }

        private static GameObject CreateVisualPrimitive(
            string name,
            PrimitiveType type,
            Vector3 position,
            Vector3 scale,
            Material material,
            Transform parent,
            Vector3? euler = null)
        {
            var visual = CreatePrimitive(name, type, position, scale, material, parent, euler);
            var collider = visual.GetComponent<Collider>();
            if (collider != null) Object.DestroyImmediate(collider);
            return visual;
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

        private static Material CreateEmissiveMaterial(string name, Color color, float intensity)
        {
            var material = CreateMaterial(name, color, 0.35f);
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * intensity);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            return material;
        }

        private static Material CreateTexturedMaterial(
            string name,
            Color color,
            string texturePath,
            float smoothness)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = new Material(shader) { color = color };
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (texture == null) throw new InvalidOperationException("Missing formal material texture: " + texturePath);
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            AssetDatabase.CreateAsset(material, Materials + "/" + name + ".mat");
            return material;
        }

        private static Material CreateBackdropMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ??
                         Shader.Find("Unlit/Texture") ??
                         Shader.Find("Standard");
            var material = new Material(shader) { color = Color.white };
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(BackdropTexture);
            if (texture == null) throw new InvalidOperationException("Missing formal backdrop texture: " + BackdropTexture);
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
            if (material.HasProperty("_Cull")) material.SetInt("_Cull", (int)CullMode.Off);
            material.doubleSidedGI = true;
            AssetDatabase.CreateAsset(material, Materials + "/OrbitalVista.mat");
            return material;
        }

        private static void SaveGeneratedScene(Scene scene, string path)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, path))
                throw new InvalidOperationException("Could not save generated scene: " + path);
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
