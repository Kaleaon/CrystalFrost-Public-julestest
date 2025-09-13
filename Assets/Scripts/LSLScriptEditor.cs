using OpenMetaverse;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text.RegularExpressions;
using System.IO;
using System.Linq;

public class LSLScriptEditor : MonoBehaviour
{
    [Header("Editor Window")]
    public GameObject editorWindow;
    public Button closeButton;
    public Button saveButton;
    public Button saveAsButton;
    public Button compileButton;
    public Button runButton;
    public Button stopButton;
    public Button newScriptButton;
    public Button openScriptButton;
    
    [Header("Script Editor")]
    public TMP_InputField scriptNameField;
    public TMP_InputField scriptEditor;
    public ScrollRect editorScrollRect;
    public TMP_Text lineNumbers;
    public TMP_Text statusText;
    public TMP_Text errorText;
    
    [Header("Script Library")]
    public Transform scriptListRoot;
    public GameObject scriptItemPrefab;
    public TMP_InputField searchField;
    public Button searchButton;
    
    [Header("Syntax Highlighting")]
    public Color keywordColor = Color.blue;
    public Color functionColor = Color.green;
    public Color commentColor = Color.gray;
    public Color stringColor = Color.red;
    public Color numberColor = Color.magenta;
    
    [Header("Code Completion")]
    public GameObject completionPopup;
    public Transform completionRoot;
    public GameObject completionItemPrefab;
    
    private GridClient client;
    private Dictionary<string, ScriptData> scripts = new();
    private ScriptData currentScript;
    private List<string> lslKeywords = new();
    private List<string> lslFunctions = new();
    private List<string> lslConstants = new();
    private List<CompletionItem> completionItems = new();
    private bool isCompiling = false;
    
    public class ScriptData
    {
        public string name;
        public string content;
        public UUID scriptID;
        public UUID itemID;
        public string filePath;
        public bool isModified;
        public System.DateTime lastModified;
        public List<ScriptError> errors = new();
    }
    
    public class ScriptError
    {
        public int line;
        public int column;
        public string message;
        public ErrorType type;
    }
    
    public enum ErrorType
    {
        Error,
        Warning,
        Info
    }
    
    public class CompletionItem
    {
        public string text;
        public string description;
        public CompletionType type;
    }
    
    public enum CompletionType
    {
        Keyword,
        Function,
        Variable,
        Constant,
        Event
    }

    void Awake()
    {
        editorWindow.SetActive(false);
        InitializeLSLData();
        SetupUI();
    }

    void InitializeLSLData()
    {
        // LSL Keywords
        lslKeywords.AddRange(new[]
        {
            "default", "state", "event", "if", "else", "for", "while", "do", "return", "jump",
            "integer", "float", "string", "key", "vector", "rotation", "list",
            "TRUE", "FALSE", "NULL_KEY", "EOF", "AGENT", "ACTIVE", "PASSIVE", "SCRIPTED"
        });
        
        // LSL Functions (subset)
        lslFunctions.AddRange(new[]
        {
            "llSay", "llWhisper", "llShout", "llListen", "llSensor", "llDetectedName",
            "llGetPos", "llSetPos", "llGetRot", "llSetRot", "llGetScale", "llSetScale",
            "llGetOwner", "llGetKey", "llGetObjectName", "llSetObjectName",
            "llMoveToTarget", "llStopMoveToTarget", "llSetText", "llSetSitText",
            "llSitTarget", "llSetTouchText", "llGiveInventory", "llRemoveInventory",
            "llSetTimerEvent", "llSleep", "llGetTime", "llResetTime",
            "llHTTPRequest", "llEmail", "llInstantMessage", "llDialog",
            "llTeleportAgentHome", "llEjectFromLand", "llParticleSystem",
            "llSetPrimitiveParams", "llGetPrimitiveParams", "llSetLinkPrimitiveParams"
        });
        
        // LSL Constants (subset)
        lslConstants.AddRange(new[]
        {
            "PI", "TWO_PI", "PI_BY_TWO", "DEG_TO_RAD", "RAD_TO_DEG",
            "ZERO_VECTOR", "ZERO_ROTATION", "DEBUG_CHANNEL", "PUBLIC_CHANNEL",
            "PRIM_TYPE", "PRIM_MATERIAL", "PRIM_PHYSICS", "PRIM_TEMP_ON_REZ",
            "CHANGED_INVENTORY", "CHANGED_TOUCH", "CHANGED_SCALE", "CHANGED_LINK"
        });
        
        // Build completion items
        BuildCompletionItems();
    }

    void BuildCompletionItems()
    {
        completionItems.Clear();
        
        foreach (var keyword in lslKeywords)
        {
            completionItems.Add(new CompletionItem
            {
                text = keyword,
                description = $"LSL keyword: {keyword}",
                type = CompletionType.Keyword
            });
        }
        
        foreach (var function in lslFunctions)
        {
            completionItems.Add(new CompletionItem
            {
                text = function,
                description = $"LSL function: {function}",
                type = CompletionType.Function
            });
        }
        
        foreach (var constant in lslConstants)
        {
            completionItems.Add(new CompletionItem
            {
                text = constant,
                description = $"LSL constant: {constant}",
                type = CompletionType.Constant
            });
        }
    }

    void SetupUI()
    {
        if (closeButton) closeButton.onClick.AddListener(() => editorWindow.SetActive(false));
        if (saveButton) saveButton.onClick.AddListener(SaveScript);
        if (saveAsButton) saveAsButton.onClick.AddListener(SaveScriptAs);
        if (compileButton) compileButton.onClick.AddListener(CompileScript);
        if (runButton) runButton.onClick.AddListener(RunScript);
        if (stopButton) stopButton.onClick.AddListener(StopScript);
        if (newScriptButton) newScriptButton.onClick.AddListener(NewScript);
        if (openScriptButton) openScriptButton.onClick.AddListener(OpenScript);
        
        if (searchButton) searchButton.onClick.AddListener(SearchScripts);
        if (searchField) searchField.onEndEdit.AddListener((text) => { if (Input.GetKeyDown(KeyCode.Return)) SearchScripts(); });
        
        // Setup script editor events
        if (scriptEditor)
        {
            scriptEditor.onValueChanged.AddListener(OnScriptContentChanged);
            scriptEditor.onSelect.AddListener(OnEditorSelected);
        }
        
        // Initialize with default script
        NewScript();
    }

    void Start()
    {
        client = ClientManager.client;
        LoadScriptLibrary();
    }

    public void ShowScriptEditor()
    {
        editorWindow.SetActive(true);
        RefreshScriptList();
    }

    void NewScript()
    {
        var newScript = new ScriptData
        {
            name = "New Script",
            content = GetDefaultScriptTemplate(),
            scriptID = UUID.Random(),
            isModified = false,
            lastModified = System.DateTime.Now
        };
        
        currentScript = newScript;
        DisplayScript(newScript);
        
        if (scriptNameField) scriptNameField.text = newScript.name;
        if (statusText) statusText.text = "New script created";
    }

    string GetDefaultScriptTemplate()
    {
        return @"default
{
    state_entry()
    {
        llSay(0, ""Hello, avatar!"");
    }

    touch_start(integer total_number)
    {
        llSay(0, ""Touched."");
    }
}";
    }

    void DisplayScript(ScriptData script)
    {
        if (scriptEditor) scriptEditor.text = script.content;
        UpdateLineNumbers();
        ApplySyntaxHighlighting();
    }

    void OnScriptContentChanged(string content)
    {
        if (currentScript != null)
        {
            currentScript.content = content;
            currentScript.isModified = true;
            currentScript.lastModified = System.DateTime.Now;
            
            UpdateLineNumbers();
            ApplySyntaxHighlighting();
            
            // Update status
            if (statusText) statusText.text = "Modified";
        }
    }

    void OnEditorSelected(string content)
    {
        // Handle cursor position for code completion
        int caretPosition = scriptEditor.caretPosition;
        string textBeforeCursor = content.Substring(0, caretPosition);
        
        // Check if we should show code completion
        if (ShouldShowCompletion(textBeforeCursor))
        {
            ShowCodeCompletion(textBeforeCursor);
        }
        else
        {
            HideCodeCompletion();
        }
    }

    bool ShouldShowCompletion(string textBeforeCursor)
    {
        // Show completion after typing letters or dots
        if (textBeforeCursor.Length == 0) return false;
        
        char lastChar = textBeforeCursor[textBeforeCursor.Length - 1];
        return char.IsLetter(lastChar) || lastChar == '.';
    }

    void ShowCodeCompletion(string textBeforeCursor)
    {
        if (completionPopup == null) return;
        
        // Extract the word being typed
        string currentWord = ExtractCurrentWord(textBeforeCursor);
        
        // Filter completion items
        var filteredItems = completionItems
            .Where(item => item.text.StartsWith(currentWord, System.StringComparison.OrdinalIgnoreCase))
            .OrderBy(item => item.text)
            .Take(10)
            .ToList();
        
        if (filteredItems.Count == 0)
        {
            HideCodeCompletion();
            return;
        }
        
        // Clear existing completion items
        foreach (Transform child in completionRoot)
        {
            Destroy(child.gameObject);
        }
        
        // Create completion items
        foreach (var item in filteredItems)
        {
            CreateCompletionItem(item);
        }
        
        completionPopup.SetActive(true);
    }

    void CreateCompletionItem(CompletionItem item)
    {
        if (completionItemPrefab == null) return;
        
        var itemObj = Instantiate(completionItemPrefab, completionRoot);
        var nameText = itemObj.transform.Find("NameText")?.GetComponent<TMP_Text>();
        var descText = itemObj.transform.Find("DescriptionText")?.GetComponent<TMP_Text>();
        var button = itemObj.GetComponent<Button>();
        
        if (nameText) nameText.text = item.text;
        if (descText) descText.text = item.description;
        
        // Set color based on type
        if (nameText)
        {
            switch (item.type)
            {
                case CompletionType.Keyword:
                    nameText.color = keywordColor;
                    break;
                case CompletionType.Function:
                    nameText.color = functionColor;
                    break;
                case CompletionType.Constant:
                    nameText.color = numberColor;
                    break;
            }
        }
        
        if (button)
        {
            button.onClick.AddListener(() => InsertCompletion(item.text));
        }
    }

    void InsertCompletion(string completionText)
    {
        if (scriptEditor == null) return;
        
        int caretPos = scriptEditor.caretPosition;
        string content = scriptEditor.text;
        
        // Find the start of the current word
        int wordStart = caretPos;
        while (wordStart > 0 && char.IsLetterOrDigit(content[wordStart - 1]))
        {
            wordStart--;
        }
        
        // Replace the partial word with the completion
        string before = content.Substring(0, wordStart);
        string after = content.Substring(caretPos);
        string newContent = before + completionText + after;
        
        scriptEditor.text = newContent;
        scriptEditor.caretPosition = wordStart + completionText.Length;
        
        HideCodeCompletion();
    }

    void HideCodeCompletion()
    {
        if (completionPopup) completionPopup.SetActive(false);
    }

    string ExtractCurrentWord(string text)
    {
        int end = text.Length;
        int start = end;
        
        while (start > 0 && (char.IsLetterOrDigit(text[start - 1]) || text[start - 1] == '_'))
        {
            start--;
        }
        
        return text.Substring(start, end - start);
    }

    void UpdateLineNumbers()
    {
        if (lineNumbers == null || scriptEditor == null) return;
        
        string[] lines = scriptEditor.text.Split('\n');
        string lineNumberText = "";
        
        for (int i = 1; i <= lines.Length; i++)
        {
            lineNumberText += i.ToString() + "\n";
        }
        
        lineNumbers.text = lineNumberText;
    }

    void ApplySyntaxHighlighting()
    {
        // This is a simplified syntax highlighting implementation
        // In a full implementation, you'd use rich text tags
        
        if (scriptEditor == null) return;
        
        string content = scriptEditor.text;
        
        // Apply syntax highlighting using rich text
        content = HighlightKeywords(content);
        content = HighlightFunctions(content);
        content = HighlightComments(content);
        content = HighlightStrings(content);
        
        // Note: TMP_InputField doesn't support rich text well
        // A full implementation would use a custom text component
    }

    string HighlightKeywords(string content)
    {
        foreach (var keyword in lslKeywords)
        {
            string pattern = @"\b" + Regex.Escape(keyword) + @"\b";
            content = Regex.Replace(content, pattern, 
                $"<color=#{ColorUtility.ToHtmlStringRGB(keywordColor)}>{keyword}</color>");
        }
        return content;
    }

    string HighlightFunctions(string content)
    {
        foreach (var function in lslFunctions)
        {
            string pattern = @"\b" + Regex.Escape(function) + @"\b";
            content = Regex.Replace(content, pattern, 
                $"<color=#{ColorUtility.ToHtmlStringRGB(functionColor)}>{function}</color>");
        }
        return content;
    }

    string HighlightComments(string content)
    {
        // Single line comments
        string pattern = @"//.*$";
        content = Regex.Replace(content, pattern, 
            $"<color=#{ColorUtility.ToHtmlStringRGB(commentColor)}>$0</color>", 
            RegexOptions.Multiline);
        
        // Multi-line comments
        pattern = @"/\*.*?\*/";
        content = Regex.Replace(content, pattern, 
            $"<color=#{ColorUtility.ToHtmlStringRGB(commentColor)}>$0</color>", 
            RegexOptions.Singleline);
        
        return content;
    }

    string HighlightStrings(string content)
    {
        string pattern = @""".*?""";
        content = Regex.Replace(content, pattern, 
            $"<color=#{ColorUtility.ToHtmlStringRGB(stringColor)}>$0</color>");
        return content;
    }

    void SaveScript()
    {
        if (currentScript == null) return;
        
        // Update script name from field
        if (scriptNameField) currentScript.name = scriptNameField.text;
        
        // Save to file if it has a path
        if (!string.IsNullOrEmpty(currentScript.filePath))
        {
            SaveScriptToFile(currentScript);
        }
        else
        {
            SaveScriptAs();
        }
        
        // Add to scripts dictionary
        scripts[currentScript.name] = currentScript;
        currentScript.isModified = false;
        
        if (statusText) statusText.text = "Script saved";
        RefreshScriptList();
    }

    void SaveScriptAs()
    {
        if (currentScript == null) return;
        
        // This would show a file dialog in a full implementation
        string fileName = currentScript.name + ".lsl";
        string filePath = Path.Combine(Application.persistentDataPath, "Scripts", fileName);
        
        Directory.CreateDirectory(Path.GetDirectoryName(filePath));
        
        currentScript.filePath = filePath;
        SaveScriptToFile(currentScript);
        
        if (statusText) statusText.text = $"Script saved as {fileName}";
    }

    void SaveScriptToFile(ScriptData script)
    {
        try
        {
            File.WriteAllText(script.filePath, script.content);
            script.lastModified = System.DateTime.Now;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to save script: {ex.Message}");
            if (statusText) statusText.text = "Save failed: " + ex.Message;
        }
    }

    void OpenScript()
    {
        // This would show a file dialog in a full implementation
        // For now, just show the script library
        RefreshScriptList();
    }

    void CompileScript()
    {
        if (currentScript == null || isCompiling) return;
        
        isCompiling = true;
        if (statusText) statusText.text = "Compiling...";
        if (errorText) errorText.text = "";
        
        StartCoroutine(CompileScriptCoroutine());
    }

    System.Collections.IEnumerator CompileScriptCoroutine()
    {
        yield return new WaitForSeconds(1.0f); // Simulate compilation time
        
        // Basic syntax checking
        List<ScriptError> errors = ValidateScript(currentScript.content);
        
        currentScript.errors = errors;
        
        if (errors.Count == 0)
        {
            if (statusText) statusText.text = "Compilation successful";
            if (errorText) errorText.text = "No errors found";
        }
        else
        {
            if (statusText) statusText.text = $"Compilation failed: {errors.Count} errors";
            DisplayErrors(errors);
        }
        
        isCompiling = false;
    }

    List<ScriptError> ValidateScript(string content)
    {
        var errors = new List<ScriptError>();
        string[] lines = content.Split('\n');
        
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            
            // Basic validation checks
            if (line.Contains("llSay") && !line.Contains(";"))
            {
                errors.Add(new ScriptError
                {
                    line = i + 1,
                    message = "Missing semicolon",
                    type = ErrorType.Error
                });
            }
            
            // Check for unmatched braces
            int openBraces = line.Count(c => c == '{');
            int closeBraces = line.Count(c => c == '}');
            
            if (openBraces != closeBraces && line.Contains("{") && line.Contains("}"))
            {
                errors.Add(new ScriptError
                {
                    line = i + 1,
                    message = "Unmatched braces",
                    type = ErrorType.Warning
                });
            }
        }
        
        return errors;
    }

    void DisplayErrors(List<ScriptError> errors)
    {
        if (errorText == null) return;
        
        string errorDisplay = "";
        foreach (var error in errors)
        {
            errorDisplay += $"Line {error.line}: {error.message}\n";
        }
        
        errorText.text = errorDisplay;
    }

    void RunScript()
    {
        if (currentScript == null) return;
        
        // In a full implementation, this would upload and run the script
        // For now, just show a message
        if (statusText) statusText.text = "Script would be uploaded and run";
        
        Debug.Log($"Running script: {currentScript.name}");
    }

    void StopScript()
    {
        if (statusText) statusText.text = "Script stopped";
        Debug.Log("Script execution stopped");
    }

    void LoadScriptLibrary()
    {
        string scriptsPath = Path.Combine(Application.persistentDataPath, "Scripts");
        
        if (!Directory.Exists(scriptsPath)) return;
        
        string[] scriptFiles = Directory.GetFiles(scriptsPath, "*.lsl");
        
        foreach (string filePath in scriptFiles)
        {
            try
            {
                string content = File.ReadAllText(filePath);
                string name = Path.GetFileNameWithoutExtension(filePath);
                
                var script = new ScriptData
                {
                    name = name,
                    content = content,
                    filePath = filePath,
                    lastModified = File.GetLastWriteTime(filePath),
                    isModified = false
                };
                
                scripts[name] = script;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to load script {filePath}: {ex.Message}");
            }
        }
    }

    void RefreshScriptList()
    {
        if (scriptListRoot == null) return;
        
        // Clear existing items
        foreach (Transform child in scriptListRoot)
        {
            Destroy(child.gameObject);
        }
        
        // Create script items
        foreach (var script in scripts.Values)
        {
            CreateScriptListItem(script);
        }
    }

    void CreateScriptListItem(ScriptData script)
    {
        if (scriptItemPrefab == null) return;
        
        var itemObj = Instantiate(scriptItemPrefab, scriptListRoot);
        var nameText = itemObj.GetComponentInChildren<TMP_Text>();
        var button = itemObj.GetComponent<Button>();
        
        if (nameText)
        {
            string displayName = script.name;
            if (script.isModified) displayName += "*";
            nameText.text = displayName;
        }
        
        if (button)
        {
            button.onClick.AddListener(() => LoadScript(script));
        }
    }

    void LoadScript(ScriptData script)
    {
        currentScript = script;
        DisplayScript(script);
        
        if (scriptNameField) scriptNameField.text = script.name;
        if (statusText) statusText.text = $"Loaded script: {script.name}";
    }

    void SearchScripts()
    {
        if (searchField == null || string.IsNullOrEmpty(searchField.text)) return;
        
        string searchTerm = searchField.text.ToLower();
        
        // Filter scripts by name or content
        var filteredScripts = scripts.Values
            .Where(s => s.name.ToLower().Contains(searchTerm) || 
                       s.content.ToLower().Contains(searchTerm))
            .ToList();
        
        // Clear and populate filtered results
        foreach (Transform child in scriptListRoot)
        {
            Destroy(child.gameObject);
        }
        
        foreach (var script in filteredScripts)
        {
            CreateScriptListItem(script);
        }
    }

    // Public methods for external integration
    public void OpenScriptByName(string scriptName)
    {
        if (scripts.ContainsKey(scriptName))
        {
            LoadScript(scripts[scriptName]);
            ShowScriptEditor();
        }
    }

    public void CreateNewScriptWithTemplate(string templateName)
    {
        string template = GetScriptTemplate(templateName);
        
        var newScript = new ScriptData
        {
            name = $"New {templateName} Script",
            content = template,
            scriptID = UUID.Random(),
            isModified = false,
            lastModified = System.DateTime.Now
        };
        
        currentScript = newScript;
        DisplayScript(newScript);
        ShowScriptEditor();
    }

    string GetScriptTemplate(string templateName)
    {
        switch (templateName.ToLower())
        {
            case "door":
                return GetDoorScriptTemplate();
            case "vendor":
                return GetVendorScriptTemplate();
            case "particle":
                return GetParticleScriptTemplate();
            default:
                return GetDefaultScriptTemplate();
        }
    }

    string GetDoorScriptTemplate()
    {
        return @"// Simple door script
float TIMER_CLOSE = 5.0; // seconds to auto close
integer isOpen = FALSE;

default
{
    state_entry()
    {
        llSetText(""Click to open"", <1,1,1>, 1.0);
    }
    
    touch_start(integer total_number)
    {
        if (isOpen)
        {
            // Close door
            llSetRot(llEuler2Rot(<0, 0, 0>));
            llSetText(""Click to open"", <1,1,1>, 1.0);
            isOpen = FALSE;
        }
        else
        {
            // Open door
            llSetRot(llEuler2Rot(<0, 0, PI_BY_TWO>));
            llSetText(""Click to close"", <1,1,1>, 1.0);
            llSetTimerEvent(TIMER_CLOSE);
            isOpen = TRUE;
        }
    }
    
    timer()
    {
        // Auto close
        llSetRot(llEuler2Rot(<0, 0, 0>));
        llSetText(""Click to open"", <1,1,1>, 1.0);
        llSetTimerEvent(0);
        isOpen = FALSE;
    }
}";
    }

    string GetVendorScriptTemplate()
    {
        return @"// Simple vendor script
integer PRICE = 10; // Price in L$
string PRODUCT = ""My Product"";

default
{
    state_entry()
    {
        llSetText(PRODUCT + ""\nL$"" + (string)PRICE, <1,1,0>, 1.0);
    }
    
    touch_start(integer total_number)
    {
        key buyer = llDetectedKey(0);
        llGiveInventory(buyer, PRODUCT);
        
        // In a real vendor, you'd handle payment here
        llSay(0, ""Thank you for your purchase!"");
    }
}";
    }

    string GetParticleScriptTemplate()
    {
        return @"// Particle system example
default
{
    state_entry()
    {
        llParticleSystem([
            PSYS_SRC_PATTERN, PSYS_SRC_PATTERN_EXPLODE,
            PSYS_SRC_BURST_RADIUS, 1.0,
            PSYS_SRC_ANGLE_BEGIN, 0.0,
            PSYS_SRC_ANGLE_END, PI,
            PSYS_PART_START_COLOR, <1.0, 0.0, 0.0>,
            PSYS_PART_END_COLOR, <0.0, 0.0, 1.0>,
            PSYS_PART_START_ALPHA, 1.0,
            PSYS_PART_END_ALPHA, 0.0,
            PSYS_PART_START_SCALE, <0.1, 0.1, 0.0>,
            PSYS_PART_END_SCALE, <1.0, 1.0, 0.0>,
            PSYS_PART_MAX_AGE, 2.0,
            PSYS_SRC_MAX_AGE, 0.0,
            PSYS_SRC_BURST_RATE, 0.1,
            PSYS_SRC_BURST_PART_COUNT, 10
        ]);
    }
    
    touch_start(integer total_number)
    {
        llParticleSystem([]);
        llSay(0, ""Particles stopped"");
    }
}";
    }
}