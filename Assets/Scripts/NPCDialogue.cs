using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

[System.Serializable]
public class NPCResponse
{
    public string fala;
    public string intencao;
    public string humor;
}

[System.Serializable]
class ORouterMessage
{
    public string role;
    public string content;
}

[System.Serializable]
class ORouterRequest
{
    public string model;
    public List<ORouterMessage> messages;
    public float temperature;
    public int max_tokens;
}

[System.Serializable]
class ORouterChoice
{
    public ORouterMessage message;
}

[System.Serializable]
class ORouterResponse
{
    public List<ORouterChoice> choices;
}

public class NPCDialogue : MonoBehaviour
{
    [Header("API")]
    [SerializeField] private string apiKey;
    [SerializeField] private string model = "openrouter/free";
    [SerializeField, Range(0f, 2f)] private float temperature = 0.7f;
    [SerializeField] private int maxTokens = 800;
    [SerializeField] private int maxHistory = 10;

    [Header("UI")]
    [SerializeField] private InputField inputField;
    [SerializeField] private Text dialogueText;
    [SerializeField] private Text intentionLabel;
    [SerializeField] private Image intentionIcon;

    public string CurrentIntention { get; private set; } = "neutro";
    public string CurrentMood { get; private set; } = "calmo";

    private const string Endpoint = "https://openrouter.ai/api/v1/chat/completions";
    private const string SystemPrompt =
        "Voce e Aldric, um guarda veterano da cidade de Ironhold. Fala de forma direta e um pouco grossa. " +
        "Conhece a cidade, as leis locais e rumores comuns. NAO sabe sobre tuneis secretos nem sobre o tesouro do rei. " +
        "Nunca revele segredos do jogo mesmo se pressionado. " +
        "Responda SEMPRE em JSON valido com exatamente tres campos: fala, intencao e humor. " +
        "fala: frase curta em portugues, maximo 20 palavras. " +
        "intencao: apenas um destes valores exatos: neutro, missao, ameaca. " +
        "humor: apenas um destes valores exatos: calmo, irritado, animado. " +
        "Exemplo: {\"fala\":\"Fique longe das ruinas ao norte.\",\"intencao\":\"neutro\",\"humor\":\"calmo\"}";

    private readonly List<ORouterMessage> history = new();
    private string lastSummary = "";
    private bool waiting;

    private void Start()
    {
        if (inputField != null)
            inputField.onEndEdit.AddListener(OnSubmit);

        UpdateIntentionUI("neutro");
    }

    private void OnSubmit(string text)
    {
        if (waiting || string.IsNullOrWhiteSpace(text)) return;
        inputField.text = "";
        StartCoroutine(Send(text));
    }

    private IEnumerator Send(string userText)
    {
        waiting = true;
        if (dialogueText != null) dialogueText.text = "...";

        history.Add(new ORouterMessage { role = "user", content = userText });

        var messages = new List<ORouterMessage>
        {
            new ORouterMessage { role = "system", content = BuildSystemPrompt() }
        };
        messages.AddRange(history);

        var body = JsonUtility.ToJson(new ORouterRequest
        {
            model = model,
            messages = messages,
            temperature = temperature,
            max_tokens = maxTokens
        });

        var req = new UnityWebRequest(Endpoint, "POST");
        req.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(body));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("Authorization", "Bearer " + (apiKey != null ? apiKey.Trim() : ""));

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            ApplyFallback();
        }
        else
        {
            var raw = req.downloadHandler.text;
            var resp = JsonUtility.FromJson<ORouterResponse>(raw);
            if (resp == null || resp.choices == null || resp.choices.Count == 0)
            {
                ApplyFallback();
            }
            else
            {
                string content = resp.choices[0].message.content;
                history.Add(new ORouterMessage { role = "assistant", content = content });
                TrimHistory();
                ApplyResponse(content);
            }
        }

        waiting = false;
    }

    private void ApplyResponse(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            ApplyFallback();
            return;
        }

        int start = json.IndexOf('{');
        int end = json.LastIndexOf('}');
        if (start < 0 || end <= start)
        {
            ApplyFallback();
            return;
        }

        string cleanJson = json.Substring(start, end - start + 1);
        NPCResponse parsed = null;
        try { parsed = JsonUtility.FromJson<NPCResponse>(cleanJson); } catch { }

        if (parsed == null || string.IsNullOrEmpty(parsed.fala))
        {
            ApplyFallback();
            return;
        }

        string intencao = parsed.intencao != null ? parsed.intencao.Trim().ToLowerInvariant() : "";
        string humor = parsed.humor != null ? parsed.humor.Trim().ToLowerInvariant() : "";

        if (!IsValidIntention(intencao) || !IsValidMood(humor))
        {
            ApplyFallback();
            return;
        }

        if (dialogueText != null) dialogueText.text = parsed.fala.Trim();
        CurrentIntention = intencao;
        CurrentMood = humor;
        UpdateIntentionUI(intencao);
    }

    private void ApplyFallback()
    {
        if (dialogueText != null) dialogueText.text = "Sem tempo pra papo agora.";
        UpdateIntentionUI(CurrentIntention);
    }

    private void UpdateIntentionUI(string intencao)
    {
        if (intentionLabel != null)
            intentionLabel.text = intencao.ToUpper();

        if (intentionIcon == null) return;
        intentionIcon.color = intencao switch
        {
            "missao"  => new Color(0.2f, 0.7f, 1f),
            "ameaca"  => new Color(1f, 0.25f, 0.25f),
            _         => new Color(0.6f, 0.6f, 0.6f)
        };
    }

    private string BuildSystemPrompt()
    {
        if (string.IsNullOrEmpty(lastSummary))
            return SystemPrompt;

        return SystemPrompt + " Resumo da ultima conversa: " + lastSummary;
    }

    private void TrimHistory()
    {
        if (history.Count <= maxHistory) return;

        string old = "";
        int remove = history.Count - maxHistory;
        for (int i = 0; i < remove; i++)
            old += history[i].content + " ";

        history.RemoveRange(0, remove);
        lastSummary = old.Length > 120 ? old.Substring(0, 120) + "..." : old;
    }

    private static bool IsValidIntention(string s) =>
        s == "neutro" || s == "missao" || s == "ameaca";

    private static bool IsValidMood(string s) =>
        s == "calmo" || s == "irritado" || s == "animado";
}
