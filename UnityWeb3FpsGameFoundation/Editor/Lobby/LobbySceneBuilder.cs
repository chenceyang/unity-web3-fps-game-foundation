using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Web3Fps.GameFoundation.Composition;
using Web3Fps.GameFoundation.Lobby;

namespace Web3Fps.GameFoundation.Editor
{
    public static class LobbySceneBuilder
    {
        private const string RootFolder = "Assets/Web3FpsLobby";
        private const string ScenePath = RootFolder + "/Lobby.unity";

        [MenuItem("Tools/Web3 FPS/Create Web3 Lobby Scene")]
        public static void CreateWeb3LobbyScene()
        {
            if (AssetDatabase.IsValidFolder(RootFolder) && !EditorUtility.DisplayDialog(
                    "Rebuild Web3 FPS Lobby",
                    "Assets/Web3FpsLobby already exists. Replace the generated lobby scene?",
                    "Replace",
                    "Cancel")) return;
            if (AssetDatabase.IsValidFolder(RootFolder)) AssetDatabase.DeleteAsset(RootFolder);
            AssetDatabase.CreateFolder("Assets", "Web3FpsLobby");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "Lobby";

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 2f, -10f);
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.035f, 0.05f, 0.075f);
            cameraObject.AddComponent<AudioListener>();

            var systems = new GameObject("Web3LobbySystems");
            var bootstrap = systems.AddComponent<GameFoundationBootstrap>();
            bootstrap.Configure(true);
            var controller = systems.AddComponent<Web3LobbyController>();
            controller.Configure(bootstrap);
            var hud = systems.AddComponent<Web3LobbyHud>();
            hud.Configure(controller);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            EditorGUIUtility.PingObject(Selection.activeObject);
            Debug.Log("Web3 lobby created at " + ScenePath + ". Open it and press Play with the mock backend.");
        }

        private static void AddSceneToBuildSettings(string scenePath)
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.Any(item => item.path == scenePath)) return;
            scenes.Insert(0, new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
