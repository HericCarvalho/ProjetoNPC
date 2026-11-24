using UnityEditor;
using UnityEditor.AI;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class NPCSceneSetup
{
    [MenuItem("Vortex/Montar Cena NPC Hibrido")]
    public static void Build()
    {
        int npcLayer = EnsureLayer("NPC");
        int obstacleLayer = EnsureLayer("Obstacle");
        Physics.IgnoreLayerCollision(npcLayer, npcLayer, true);

        EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        if (Camera.main != null)
        {
            Camera.main.transform.position = new Vector3(0f, 8f, -10f);
            Camera.main.transform.eulerAngles = new Vector3(30f, 0f, 0f);
        }

        Shader std = Shader.Find("Standard");
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");

        Material matChao   = CreateMat("Assets/Materials/MatChao.mat",     std, new Color(0.22f, 0.22f, 0.22f));
        Material matObs    = CreateMat("Assets/Materials/MatObstaculo.mat", std, new Color(0.65f, 0.3f,  0.3f));
        Material matNPC    = CreateMat("Assets/Materials/MatNPC.mat",       std, new Color(0.2f,  0.6f,  1f));
        Material matPlayer = CreateMat("Assets/Materials/MatPlayer.mat",    std, new Color(0.95f, 0.75f, 0.2f));
        Material matGoal   = CreateMat("Assets/Materials/MatMissao.mat",    std, new Color(0.2f,  0.9f,  0.4f));

        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Chao";
        ground.transform.localScale = new Vector3(2.4f, 1f, 2.4f);
        ground.GetComponent<Renderer>().sharedMaterial = matChao;
        ground.isStatic = true;

        CreateWall("ParedeN",  new Vector3(0f,1f,12f),  new Vector3(24f,2f,1f), obstacleLayer, matObs);
        CreateWall("ParedeS",  new Vector3(0f,1f,-12f), new Vector3(24f,2f,1f), obstacleLayer, matObs);
        CreateWall("ParedeE",  new Vector3(12f,1f,0f),  new Vector3(1f,2f,24f), obstacleLayer, matObs);
        CreateWall("ParedeO",  new Vector3(-12f,1f,0f), new Vector3(1f,2f,24f), obstacleLayer, matObs);
        CreateWall("Obstac1",  new Vector3(-3f,1f,2f),  new Vector3(2f,2f,5f),  obstacleLayer, matObs);
        CreateWall("Obstac2",  new Vector3(4f,1f,-3f),  new Vector3(2f,2f,4f),  obstacleLayer, matObs);

        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            AssetDatabase.CreateFolder("Assets", "Scenes");
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), "Assets/Scenes/NPC_Hibrido.unity");
        UnityEditor.AI.NavMeshBuilder.BuildNavMesh();

        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.transform.position = new Vector3(-6f, 1f, 0f);
        player.GetComponent<Renderer>().sharedMaterial = matPlayer;
        player.AddComponent<SimplePlayerController>();

        GameObject missionTarget = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        missionTarget.name = "MissionTarget";
        missionTarget.transform.position = new Vector3(8f, 0.5f, 8f);
        missionTarget.transform.localScale = new Vector3(1f, 0.1f, 1f);
        missionTarget.GetComponent<Renderer>().sharedMaterial = matGoal;

        GameObject npcObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        npcObj.name = "NPC_Aldric";
        npcObj.layer = npcLayer;
        npcObj.transform.position = new Vector3(2f, 1f, 0f);
        npcObj.GetComponent<Renderer>().sharedMaterial = matNPC;

        NavMeshAgent navAgent = npcObj.AddComponent<NavMeshAgent>();
        navAgent.speed = 3.5f;
        navAgent.stoppingDistance = 2f;

        NPCDialogue dialogue = npcObj.AddComponent<NPCDialogue>();
        NPCMovement movement = npcObj.AddComponent<NPCMovement>();

        SerializedObject movSo = new SerializedObject(movement);
        movSo.FindProperty("player").objectReferenceValue = player.transform;
        movSo.FindProperty("missionTarget").objectReferenceValue = missionTarget.transform;
        movSo.ApplyModifiedPropertiesWithoutUndo();

        BuildUI(dialogue, movement);

        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            AssetDatabase.CreateFolder("Assets", "Scenes");

        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), "Assets/Scenes/NPC_Hibrido.unity");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Cena NPC_Hibrido.unity criada com sucesso.");
    }

    static void BuildUI(NPCDialogue dialogue, NPCMovement movement)
    {
        GameObject canvas = new GameObject("UICanvas");
        Canvas c = canvas.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.AddComponent<CanvasScaler>();
        canvas.AddComponent<GraphicRaycaster>();

        GameObject vignette = new GameObject("Vignette");
        vignette.transform.SetParent(canvas.transform, false);
        Image vigImg = vignette.AddComponent<Image>();
        vigImg.color = Color.clear;
        RectTransform vigRect = vigImg.rectTransform;
        vigRect.anchorMin = Vector2.zero;
        vigRect.anchorMax = Vector2.one;
        vigRect.offsetMin = vigRect.offsetMax = Vector2.zero;
        vigImg.raycastTarget = false;

        GameObject panel = new GameObject("DialogPanel");
        panel.transform.SetParent(canvas.transform, false);
        Image panelImg = panel.AddComponent<Image>();
        panelImg.color = new Color(0f, 0f, 0f, 0.7f);
        RectTransform panelRect = panelImg.rectTransform;
        panelRect.anchorMin = new Vector2(0f, 0f);
        panelRect.anchorMax = new Vector2(1f, 0.3f);
        panelRect.offsetMin = panelRect.offsetMax = Vector2.zero;

        Text dialogText = CreateText(panel.transform, "DialogText",
            new Vector2(0f, 0.52f), new Vector2(1f, 0.96f), 18, "...");

        Text intentionLbl = CreateText(panel.transform, "IntentionLabel",
            new Vector2(0.05f, 0.35f), new Vector2(0.3f, 0.5f), 14, "NEUTRO");
        intentionLbl.color = Color.white;

        GameObject iconGO = new GameObject("IntentionIcon");
        iconGO.transform.SetParent(panel.transform, false);
        Image iconImg = iconGO.AddComponent<Image>();
        iconImg.color = Color.gray;
        RectTransform iconRect = iconImg.rectTransform;
        iconRect.anchorMin = new Vector2(0.01f, 0.37f);
        iconRect.anchorMax = new Vector2(0.04f, 0.48f);
        iconRect.offsetMin = iconRect.offsetMax = Vector2.zero;

        GameObject inputGO = new GameObject("InputField");
        inputGO.transform.SetParent(panel.transform, false);
        Image inputImg = inputGO.AddComponent<Image>();
        inputImg.color = new Color(0.15f, 0.15f, 0.15f, 1f);
        RectTransform inputRect = inputImg.rectTransform;
        inputRect.anchorMin = new Vector2(0f, 0f);
        inputRect.anchorMax = new Vector2(1f, 0.38f);
        inputRect.offsetMin = new Vector2(8f, 4f);
        inputRect.offsetMax = new Vector2(-8f, -4f);

        InputField inputField = inputGO.AddComponent<InputField>();

        GameObject placeholder = new GameObject("Placeholder");
        placeholder.transform.SetParent(inputGO.transform, false);
        Text placeholderText = placeholder.AddComponent<Text>();
        placeholderText.text = "Digite aqui...";
        placeholderText.color = new Color(1f, 1f, 1f, 0.4f);
        placeholderText.fontSize = 16;
        placeholderText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        RectTransform phRect = placeholderText.rectTransform;
        phRect.anchorMin = Vector2.zero;
        phRect.anchorMax = Vector2.one;
        phRect.offsetMin = new Vector2(4f, 0f);
        phRect.offsetMax = new Vector2(-4f, 0f);

        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(inputGO.transform, false);
        Text inputText = textGO.AddComponent<Text>();
        inputText.color = Color.white;
        inputText.fontSize = 16;
        inputText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        RectTransform textRect = inputText.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(4f, 0f);
        textRect.offsetMax = new Vector2(-4f, 0f);

        inputField.targetGraphic = inputImg;
        inputField.textComponent = inputText;
        inputField.placeholder = placeholderText;

        SerializedObject dlgSo = new SerializedObject(dialogue);
        dlgSo.FindProperty("inputField").objectReferenceValue = inputField;
        dlgSo.FindProperty("dialogueText").objectReferenceValue = dialogText;
        dlgSo.FindProperty("intentionLabel").objectReferenceValue = intentionLbl;
        dlgSo.FindProperty("intentionIcon").objectReferenceValue = iconImg;
        dlgSo.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject movSo = new SerializedObject(movement);
        movSo.FindProperty("cameraVignette").objectReferenceValue = vigImg;
        movSo.ApplyModifiedPropertiesWithoutUndo();

        GameObject es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();
    }

    static Text CreateText(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, int size, string text)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.text = text;
        t.fontSize = size;
        t.color = Color.white;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.supportRichText = false;
        RectTransform r = t.rectTransform;
        r.anchorMin = anchorMin;
        r.anchorMax = anchorMax;
        r.offsetMin = new Vector2(8f, 4f);
        r.offsetMax = new Vector2(-8f, -4f);
        return t;
    }

    static Material CreateMat(string path, Shader shader, Color color)
    {
        if (AssetDatabase.LoadAssetAtPath<Material>(path) != null)
            AssetDatabase.DeleteAsset(path);
        var mat = new Material(shader) { color = color };
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    static void CreateWall(string name, Vector3 pos, Vector3 scale, int layer, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.layer = layer;
        go.transform.position = pos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        go.isStatic = true;
    }

    static int EnsureLayer(string layerName)
    {
        var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layers = tagManager.FindProperty("layers");
        for (int i = 8; i < layers.arraySize; i++)
        {
            var sp = layers.GetArrayElementAtIndex(i);
            if (sp.stringValue == layerName) return i;
        }
        for (int i = 8; i < layers.arraySize; i++)
        {
            var sp = layers.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(sp.stringValue))
            {
                sp.stringValue = layerName;
                tagManager.ApplyModifiedProperties();
                return i;
            }
        }
        return 0;
    }
}
