using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
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
using Web3Fps.GameFoundation.Server;
using Object = UnityEngine.Object;

namespace Web3Fps.GameFoundation.Editor
{
    /// <summary>Generates the editable ASH//LEDGER Gate B vertical slice from package-owned code.</summary>
    public static class FormalVerticalSliceBuilder
    {
        private const string Root = "Assets/AshLedgerVerticalSlice";
        private const string Materials = Root + "/Materials";
        private const string Prefabs = Root + "/Prefabs";
        private const string Animations = Root + "/Animations";
        private const string GeneratedMeshes = Root + "/GeneratedMeshes";
        private const string Scenes = Root + "/Scenes";
        private const string LobbyScene = Scenes + "/AshLedgerLobby.unity";
        private const string RelayScene = Scenes + "/RiftRelay.unity";
        private const string LobbyUxml = "Packages/com.web3fps.game-foundation/Runtime/Formal/UI/AshLedgerLobby.uxml";
        private const string CombatUxml = "Packages/com.web3fps.game-foundation/Runtime/Formal/UI/AshLedgerCombatHud.uxml";
        private const string PostMatchUxml = "Packages/com.web3fps.game-foundation/Runtime/Formal/UI/AshLedgerPostMatch.uxml";
        private const string BackdropTexture = "Packages/com.web3fps.game-foundation/Runtime/Formal/Art/RiftRelayOrbitalBackdrop.png";
        private const string ArmorTexture = "Packages/com.web3fps.game-foundation/Runtime/Formal/Art/OperatorArmorSurface.png";
        private const string CobaltCharacterFbx = "Packages/com.web3fps.game-foundation/Runtime/Formal/ThirdParty/Quaternius/Characters/BlueSoldier_Male.fbx";
        private const string CoralCharacterFbx = "Packages/com.web3fps.game-foundation/Runtime/Formal/ThirdParty/Quaternius/Characters/Soldier_Male.fbx";

        [MenuItem("Tools/Web3 FPS/Create ASH LEDGER Vertical Slice")]
        public static void Create()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog(
                    "Stop Play Mode",
                    "Stop Play Mode before rebuilding the ASH//LEDGER scenes.",
                    "OK");
                return;
            }
            if (EditorApplication.isCompiling)
            {
                EditorUtility.DisplayDialog("Scripts Are Compiling", "Wait for compilation to finish, then run the generator again.", "OK");
                return;
            }

            if (AssetDatabase.IsValidFolder(Root) && !EditorUtility.DisplayDialog(
                    "Rebuild ASH//LEDGER Vertical Slice",
                    "The generated Assets/AshLedgerVerticalSlice folder will be replaced.",
                    "Replace",
                    "Cancel")) return;

            if (AssetDatabase.IsValidFolder(Root)) AssetDatabase.DeleteAsset(Root);
            EnsureFolder(Materials);
            EnsureFolder(Prefabs);
            EnsureFolder(Animations);
            EnsureFolder(GeneratedMeshes);
            EnsureFolder(Scenes);

            var palette = CreatePalette();
            var weapons = CreateWeaponPrefabs(palette);
            var playerPrefab = CreateFormalPlayerPrefab(palette);
            var botPrefab = CreateFormalBotPrefab(palette, weapons[0]);
            var panelSettings = CreatePanelSettings();
            CreateLobbyScene(palette, weapons, botPrefab, panelSettings);
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

        [MenuItem("Tools/Web3 FPS/Create ASH LEDGER Vertical Slice", true)]
        private static bool ValidateCreate()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling;
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

        // Order must match FormalContentCatalog.LaunchWeapons; index 0 stays the
        // KESTREL-7 used by the view model and the bot.
        private static GameObject[] CreateWeaponPrefabs(IReadOnlyDictionary<string, Material> palette)
        {
            return new[]
            {
                CreateWeapon("KESTREL-7", new Vector3(1.18f, 0.18f, 0.16f), palette["cobalt"], palette["graphite"], palette["bone"], true),
                CreateWeapon("PULSE-9", new Vector3(0.82f, 0.22f, 0.18f), palette["coral"], palette["graphite"], palette["bone"], true),
                CreateWeapon("WITNESS", new Vector3(1.42f, 0.15f, 0.14f), palette["aurora"], palette["graphite"], palette["bone"], true,
                    root => AddWitnessDetail(root, palette)),
                CreateWeapon("BREACH-12", new Vector3(0.98f, 0.24f, 0.2f), palette["copper"], palette["graphite"], palette["bone"], true,
                    root => AddBreachDetail(root, palette)),
                CreateWeapon("ANCHOR", new Vector3(1.3f, 0.26f, 0.2f), palette["warmGlow"], palette["graphite"], palette["bone"], true,
                    root => AddAnchorDetail(root, palette)),
                CreateWeapon("RELAY-3", new Vector3(0.58f, 0.17f, 0.13f), palette["aurora"], palette["graphite"], palette["bone"], false)
            };
        }

        private static GameObject CreateWeapon(
            string name,
            Vector3 receiverScale,
            Material accent,
            Material dark,
            Material shell,
            bool stock,
            Action<Transform> detail = null)
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
            detail?.Invoke(root.transform);
            // Weapon blockouts are pure visuals; a stray collider would shadow the
            // explicit hit zones, so every part is stripped regardless of name.
            foreach (var collider in root.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(collider);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, Prefabs + "/" + name + ".prefab");
            Object.DestroyImmediate(root);
            return prefab;
        }

        // 细长导轨与档案刻度 silhouette per the FORMAL_CONTENT_DESIGN weapon table.
        private static void AddWitnessDetail(Transform root, IReadOnlyDictionary<string, Material> palette)
        {
            CreatePrimitive("ScopeTube", PrimitiveType.Cylinder, new Vector3(-0.06f, 0.26f, 0f), new Vector3(0.09f, 0.26f, 0.09f), palette["graphite"], root, new Vector3(0f, 0f, 90f));
            CreatePart("ScopeMountFront", new Vector3(0.12f, 0.19f, 0f), new Vector3(0.05f, 0.08f, 0.1f), palette["graphite"], root);
            CreatePart("ScopeMountRear", new Vector3(-0.24f, 0.19f, 0f), new Vector3(0.05f, 0.08f, 0.1f), palette["graphite"], root);
            CreatePrimitive("BarrelExtension", PrimitiveType.Cylinder, new Vector3(1.28f, 0.015f, 0f), new Vector3(0.05f, 0.18f, 0.05f), palette["graphite"], root, new Vector3(0f, 0f, 90f));
            CreatePart("ArchiveGauge", new Vector3(0.42f, 0.03f, 0.085f), new Vector3(0.5f, 0.035f, 0.02f), palette["aurora"], root);
        }

        // 粗壮泵动结构 silhouette per the FORMAL_CONTENT_DESIGN weapon table.
        private static void AddBreachDetail(Transform root, IReadOnlyDictionary<string, Material> palette)
        {
            CreatePart("PumpSlide", new Vector3(0.42f, -0.1f, 0f), new Vector3(0.32f, 0.13f, 0.17f), palette["bone"], root);
            CreatePrimitive("MuzzleRing", PrimitiveType.Cylinder, new Vector3(0.83f, 0.015f, 0f), new Vector3(0.11f, 0.05f, 0.11f), palette["copper"], root, new Vector3(0f, 0f, 90f));
            CreatePart("ShellRackA", new Vector3(-0.12f, 0.02f, 0.13f), new Vector3(0.06f, 0.06f, 0.05f), palette["copper"], root);
            CreatePart("ShellRackB", new Vector3(-0.02f, 0.02f, 0.13f), new Vector3(0.06f, 0.06f, 0.05f), palette["copper"], root);
            CreatePart("ShellRackC", new Vector3(0.08f, 0.02f, 0.13f), new Vector3(0.06f, 0.06f, 0.05f), palette["copper"], root);
        }

        // 外露散热片与弹鼓 silhouette per the FORMAL_CONTENT_DESIGN weapon table.
        private static void AddAnchorDetail(Transform root, IReadOnlyDictionary<string, Material> palette)
        {
            CreatePart("HeatFinA", new Vector3(0.5f, 0.14f, 0f), new Vector3(0.05f, 0.1f, 0.24f), palette["graphite"], root);
            CreatePart("HeatFinB", new Vector3(0.62f, 0.14f, 0f), new Vector3(0.05f, 0.1f, 0.24f), palette["graphite"], root);
            CreatePart("HeatFinC", new Vector3(0.74f, 0.14f, 0f), new Vector3(0.05f, 0.1f, 0.24f), palette["graphite"], root);
            CreatePrimitive("AmmoDrum", PrimitiveType.Cylinder, new Vector3(0.08f, -0.26f, 0f), new Vector3(0.26f, 0.09f, 0.26f), palette["slate"], root, new Vector3(90f, 0f, 0f));
            CreatePart("ForwardGrip", new Vector3(0.52f, -0.19f, 0f), new Vector3(0.09f, 0.18f, 0.09f), palette["graphite"], root);
        }

        private static GameObject CreateFormalPlayerPrefab(
            IReadOnlyDictionary<string, Material> palette)
        {
            var source = new GameObject("CobaltOperator");
            source.layer = 2;
            var controller = source.AddComponent<CharacterController>();
            controller.center = Vector3.up;
            controller.height = 2f;
            controller.radius = 0.42f;
            var health = source.AddComponent<Health>();
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

            CreateDamageZones(source.transform, health);
            CreateFirstPersonArms(view, palette, weapon);

            look.Configure(view);
            weapon.Configure("local-player", view, shotSink);
            // Combat numbers come from the server-approved catalog, never from skins.
            weapon.ApplyDefinition(FormalContentCatalog.FindWeapon("kestrel-7").definition);
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
            source.layer = 2;
            var controller = source.AddComponent<CharacterController>();
            controller.center = Vector3.up;
            controller.height = 2f;
            controller.radius = 0.48f;
            var health = source.AddComponent<Health>();
            var botController = source.AddComponent<PrototypeBotController>();
            var participant = source.AddComponent<PrototypeParticipant>();
            participant.Configure("prototype-bot", "Coral", "coral", null, new MonoBehaviour[] { botController });
            botController.Configure(participant, null);
            botController.ConfigureDifficulty(PrototypeBotDifficulty.Standard);
            CreateDamageZones(source.transform, health);

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
            CreateVisualPrimitive("LeftLeg", PrimitiveType.Capsule, new Vector3(-…7931 tokens truncated… 2f, 1.1f) : new Vector3(3.4f, 1.42f, 1.15f);
                CreatePart("CoverShell_" + i.ToString("00"), covers[i], scale, palette["bone"], arena, new Vector3(0f, i % 2 == 0 ? 8f : -8f, 0f));
                var accent = i % 2 == 0 ? palette["cobaltGlow"] : palette["coralGlow"];
                CreatePart("CoverSignal_" + i.ToString("00"), covers[i] + new Vector3(0f, tall ? 0.72f : 0.48f, -0.6f), new Vector3(2.15f, 0.1f, 0.04f), accent, arena);
            }

            CreatePart("CobaltSpawnShield", new Vector3(-19.4f, 0.7f, 8.1f), new Vector3(0.16f, 1.4f, 4.2f), palette["cobaltGlow"], arena);
            CreatePart("CoralSpawnShield", new Vector3(19.4f, 0.7f, -8.1f), new Vector3(0.16f, 1.4f, 4.2f), palette["coralGlow"], arena);

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
            ConfigureBackdropRenderer(backdrop);

            // Built-in Unlit/Texture has fixed back-face culling. Add a reverse-facing
            // plane so the vista works consistently across Built-in, URP and HDRP.
            var reverseEuler = euler + new Vector3(0f, 180f, 0f);
            var reverse = CreatePrimitive(name + " Reverse", PrimitiveType.Quad, position, scale, material, parent, reverseEuler);
            ConfigureBackdropRenderer(reverse);
        }

        private static void ConfigureBackdropRenderer(GameObject backdrop)
        {
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
            IReadOnlyDictionary<string, Material> palette,
            HitscanWeapon weapon)
        {
            var fallback = CreateFirstPersonArmsFallback(camera, palette);
            var presentation = camera.gameObject.AddComponent<FormalFirstPersonRig>();
            presentation.Configure(weapon, null, fallback.transform);
        }

        private static Mesh CreateArmOnlyMesh(SkinnedMeshRenderer renderer, int rendererIndex)
        {
            var source = renderer.sharedMesh;
            if (source == null || source.vertexCount == 0 || source.boneWeights.Length != source.vertexCount) return null;
            var armBones = new bool[renderer.bones.Length];
            var hasArmBone = false;
            for (var i = 0; i < renderer.bones.Length; i++)
            {
                var boneName = renderer.bones[i] == null ? string.Empty : renderer.bones[i].name.ToLowerInvariant();
                armBones[i] = boneName.Contains("arm") || boneName.Contains("hand") ||
                              boneName.Contains("shoulder") || boneName.Contains("clavicle");
                hasArmBone |= armBones[i];
            }
            if (!hasArmBone) return null;

            var weights = source.boneWeights;
            var armVertex = new bool[source.vertexCount];
            for (var i = 0; i < weights.Length; i++) armVertex[i] = IsInfluencedByArm(weights[i], armBones);

            var filtered = Object.Instantiate(source);
            filtered.name = "CobaltFirstPersonArms_" + rendererIndex;
            var keptTriangleCount = 0;
            for (var subMesh = 0; subMesh < source.subMeshCount; subMesh++)
            {
                var triangles = source.GetTriangles(subMesh);
                var kept = new List<int>(triangles.Length / 3);
                for (var triangle = 0; triangle + 2 < triangles.Length; triangle += 3)
                {
                    var a = triangles[triangle];
                    var b = triangles[triangle + 1];
                    var c = triangles[triangle + 2];
                    var influenced = (armVertex[a] ? 1 : 0) + (armVertex[b] ? 1 : 0) + (armVertex[c] ? 1 : 0);
                    if (influenced < 2) continue;
                    kept.Add(a);
                    kept.Add(b);
                    kept.Add(c);
                }
                filtered.SetTriangles(kept, subMesh, false);
                keptTriangleCount += kept.Count / 3;
            }
            if (keptTriangleCount == 0)
            {
                Object.DestroyImmediate(filtered);
                return null;
            }
            filtered.bounds = source.bounds;
            AssetDatabase.CreateAsset(filtered, GeneratedMeshes + "/" + filtered.name + ".asset");
            return filtered;
        }

        private static bool IsInfluencedByArm(BoneWeight weight, IReadOnlyList<bool> armBones)
        {
            return IsArmWeight(weight.boneIndex0, weight.weight0, armBones) ||
                   IsArmWeight(weight.boneIndex1, weight.weight1, armBones) ||
                   IsArmWeight(weight.boneIndex2, weight.weight2, armBones) ||
                   IsArmWeight(weight.boneIndex3, weight.weight3, armBones);
        }

        private static bool IsArmWeight(int boneIndex, float weight, IReadOnlyList<bool> armBones)
        {
            return weight >= 0.08f && boneIndex >= 0 && boneIndex < armBones.Count && armBones[boneIndex];
        }

        private static GameObject CreateFirstPersonArmsFallback(
            Transform camera,
            IReadOnlyDictionary<string, Material> palette)
        {
            var root = new GameObject("FirstPersonArms // Safe Fallback");
            var rig = root.transform;
            rig.SetParent(camera, false);
            rig.localPosition = new Vector3(0.04f, -0.12f, 0.18f);
            rig.localScale = Vector3.one * 0.3f;
            CreateVisualPrimitive("LeftSleeve", PrimitiveType.Capsule, new Vector3(-0.12f, -0.5f, 0.72f), new Vector3(0.1f, 0.27f, 0.1f), palette["graphite"], rig, new Vector3(72f, 0f, -12f));
            CreateVisualPrimitive("RightSleeve", PrimitiveType.Capsule, new Vector3(0.28f, -0.53f, 0.7f), new Vector3(0.1f, 0.29f, 0.1f), palette["graphite"], rig, new Vector3(70f, 0f, 12f));
            CreateVisualPrimitive("LeftForearmArmor", PrimitiveType.Cube, new Vector3(-0.08f, -0.39f, 0.8f), new Vector3(0.12f, 0.2f, 0.12f), palette["armor"], rig, new Vector3(20f, 0f, -8f));
            CreateVisualPrimitive("RightForearmArmor", PrimitiveType.Cube, new Vector3(0.3f, -0.41f, 0.82f), new Vector3(0.12f, 0.21f, 0.12f), palette["armor"], rig, new Vector3(18f, 0f, 8f));
            CreateVisualPrimitive("LeftGlove", PrimitiveType.Sphere, new Vector3(-0.02f, -0.3f, 0.88f), new Vector3(0.1f, 0.08f, 0.12f), palette["graphite"], rig);
            CreateVisualPrimitive("RightGlove", PrimitiveType.Sphere, new Vector3(0.34f, -0.31f, 0.91f), new Vector3(0.1f, 0.08f, 0.12f), palette["graphite"], rig);
            return root;
        }

        private static void CreateDamageZones(Transform actor, Health health)
        {
            var zones = new GameObject("DamageZones").transform;
            zones.SetParent(actor, false);
            CreateSphereDamageZone("Head", new Vector3(0f, 1.72f, 0f), 0.28f, 2f, health, zones);
            CreateBoxDamageZone("Torso", new Vector3(0f, 1.14f, 0f), new Vector3(0.78f, 0.92f, 0.52f), 1f, health, zones);
            CreateBoxDamageZone("Legs", new Vector3(0f, 0.42f, 0f), new Vector3(0.66f, 0.72f, 0.48f), 0.78f, health, zones);
        }

        private static void CreateSphereDamageZone(
            string name, Vector3 center, float radius, float multiplier, Health health, Transform parent)
        {
            var zone = new GameObject(name + "HitZone");
            zone.transform.SetParent(parent, false);
            var collider = zone.AddComponent<SphereCollider>();
            collider.center = center;
            collider.radius = radius;
            collider.isTrigger = true;
            zone.AddComponent<DamageZone>().Configure(health, name.ToLowerInvariant(), multiplier);
        }

        private static void CreateBoxDamageZone(
            string name, Vector3 center, Vector3 size, float multiplier, Health health, Transform parent)
        {
            var zone = new GameObject(name + "HitZone");
            zone.transform.SetParent(parent, false);
            var collider = zone.AddComponent<BoxCollider>();
            collider.center = center;
            collider.size = size;
            collider.isTrigger = true;
            zone.AddComponent<DamageZone>().Configure(health, name.ToLowerInvariant(), multiplier);
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
            var shader = ResolveLitShader();
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
            var shader = ResolveLitShader();
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
            var shader = ResolveUnlitShader();
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

        private static Shader ResolveLitShader()
        {
            if (GraphicsSettings.currentRenderPipeline == null)
                return Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit");

            var pipelineName = GraphicsSettings.currentRenderPipeline.GetType().FullName ?? string.Empty;
            if (pipelineName.Contains("HDRenderPipeline"))
                return Shader.Find("HDRP/Lit") ?? Shader.Find("Standard");
            return Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        }

        private static Shader ResolveUnlitShader()
        {
            if (GraphicsSettings.currentRenderPipeline == null)
                return Shader.Find("Unlit/Texture") ?? Shader.Find("Standard");

            var pipelineName = GraphicsSettings.currentRenderPipeline.GetType().FullName ?? string.Empty;
            if (pipelineName.Contains("HDRenderPipeline"))
                return Shader.Find("HDRP/Unlit") ?? Shader.Find("Unlit/Texture");
            return Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Texture");
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
