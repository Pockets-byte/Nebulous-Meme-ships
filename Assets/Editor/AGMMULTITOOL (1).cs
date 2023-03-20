using UnityEngine;
using UnityEditor;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine.Rendering;
//using SOHolder;



[InitializeOnLoad]
public class AGMMULTITOOL : EditorWindow
{

    static AssetBundle stock;
    string abloc = "";


    string path = "C:/Steam/steamapps/common/Nebulous";
    bool groupEnabled;

    [MenuItem("AGM's Toolkit/Quick Build AssetBundles")]
    static void QuickBuildAllAssetBundles()
    {
        CreateFolders();
        string assetBundleDirectory = "Assets/Tool/AssetBundles";
        BuildPipeline.BuildAssetBundles(assetBundleDirectory, BuildAssetBundleOptions.UncompressedAssetBundle, EditorUserBuildSettings.activeBuildTarget);
    }
    [MenuItem("AGM's Toolkit/Build AssetBundles")]
    static void BuildAllAssetBundles()
    {
        CreateFolders();
        string assetBundleDirectory = "Assets/Tool/AssetBundles";
        BuildPipeline.BuildAssetBundles(assetBundleDirectory, BuildAssetBundleOptions.None, EditorUserBuildSettings.activeBuildTarget);
    }


    [MenuItem("AGM's Toolkit/Setup")]
    static void ShowWindow()
    {
        EditorWindow window = EditorWindow.GetWindow(typeof(AGMMULTITOOL));
        window.Show();
    }


    [MenuItem("AGM's Toolkit/Generate Load Asset Menu")]
    static void LoadAssetMenu()
    {
        CreateFolders();
        string scriptFile = Application.dataPath + "/Tool/Editor/GeneratedMenuItems.cs";
        File.AppendAllText(scriptFile, "\n");

        // The class string
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("// This class is Auto-Generated");
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("using UnityEditor;");
        sb.AppendLine("using System.IO;");
        sb.AppendLine("");
        sb.AppendLine("public class Holder : MonoBehaviour");
        sb.AppendLine("{");
        sb.AppendLine("    public ScriptableObject a;");
        sb.AppendLine("}");
        sb.AppendLine("public class GeneratedMenuItems : MonoBehaviour {");
        sb.AppendLine("    static AssetBundle stock; ");
        sb.AppendLine("    ");

        int i = 0;
        string fullstring = "";
        foreach (string line in System.IO.File.ReadLines(Application.dataPath + "/Tool/AssetBundles/stock.manifest"))
        {
            if (line.Contains(":"))
                continue;
            if (line[0] == '-')
                fullstring = line;
            else
                fullstring += line.Substring(1);


            if (line.Contains(".prefab")  || line.Contains(".asset"))
            {
                Debug.Log(line);
                string shortline = fullstring.Substring(18);
                string assetname = Path.GetFileNameWithoutExtension(shortline);
                
                sb.AppendLine("    [MenuItem(\"Load Asset/" + shortline + "\")]");
                sb.AppendLine("    private static void MenuItem" + i.ToString() + "() {");
                sb.AppendLine("        Debug.Log(\"Selected item: " + assetname + "\");");
                sb.AppendLine("        if (stock == null)");
                sb.AppendLine("            stock = AssetBundle.LoadFromFile(Path.Combine(Application.dataPath + \"/Tool/AssetBundles/stock\"));");
                if (line.Contains(".prefab"))
                    sb.AppendLine("        Instantiate(stock.LoadAsset<GameObject>(\"" + assetname + "\"));");
                if (line.Contains(".asset"))
                {
                    sb.AppendLine("        GameObject goh = new GameObject(\"" + assetname + " holder" + "\");");
                    sb.AppendLine("        goh.AddComponent<Holder>().a = stock.LoadAsset<ScriptableObject>(\"" + assetname + "\"); ");
                }
                sb.AppendLine("        ");
                sb.AppendLine("    }");
                sb.AppendLine("");
                i++;
            }

        }


        sb.AppendLine("");
        sb.AppendLine("}");

        // writes the class and imports it so it is visible in the Project window
        System.IO.File.Delete(scriptFile);
        System.IO.File.WriteAllText(scriptFile, sb.ToString(), System.Text.Encoding.UTF8);
        AssetDatabase.ImportAsset("Assets/Tool/Editor/GeneratedMenuItems.cs");
    }


    void OnGUI()
    {
        CreateFolders();
        GUILayout.Label("Asset Loading", EditorStyles.boldLabel);
        GUILayout.Label("Base Settings", EditorStyles.boldLabel);
        if (GUILayout.Button("Set Current Nebulous Directory"))
        {
            //Debug.Log("Clicked the image");
            path = EditorUtility.OpenFolderPanel("Set Neb Directory", path, "");
        }
   
        path = EditorGUILayout.TextField("Nebulous Install", path);
        abloc = path + "/Assets/AssetBundles/stock";
        if (File.Exists(abloc))
            groupEnabled = true;
        else
            groupEnabled = false;
        groupEnabled = EditorGUILayout.BeginToggleGroup("Install Valid", groupEnabled);
        if (GUILayout.Button("Grab Assetbundle"))
        {
            FileUtil.ReplaceFile(abloc, Application.dataPath + "/Tool/AssetBundles/stock");
            FileUtil.ReplaceFile(abloc + ".manifest", Application.dataPath + "/Tool/AssetBundles/stock.manifest");

        }
        if (GUILayout.Button("Grab DLLS"))
        {
            Directory.CreateDirectory(Application.dataPath + "/Plugins");
            string[] dlls = {
            "Facepunch.Steamworks.Win64","kcp2k","Mirror","Nebulous","Priority Queue","QFSW.QC","QuickGraph.All","QuickGraph.Core","QuickGraph.Serialization","RSG.Promise","ShapesRuntime","Telepathy","UIExtensions","Unity.Addressables","Unity.ResourceManager","Vectrosity","where-allocations","XNode"};
            foreach(string dllname in dlls)
            {
                FileUtil.ReplaceFile(path + "\\Nebulous_Data\\Managed\\" + dllname + ".dll", Application.dataPath + "\\Tool\\Plugins\\" + dllname + ".dll");
            }

        }
        EditorGUILayout.EndToggleGroup();
    }
    static void CreateFolders()
    {
        Directory.CreateDirectory(Application.dataPath + "/Tool");
        Directory.CreateDirectory(Application.dataPath + "/Tool/Plugins");
        Directory.CreateDirectory(Application.dataPath + "/Tool/Editor");
        Directory.CreateDirectory(Application.dataPath + "/Tool/AssetBundles");
    }

    void Start()
    {

        //string localPath = "Assets/" + gameObject.name + ".prefab";
        //var anotherGo = PrefabUtility.InstantiatePrefab(prefab) as GameObject;

        //PrefabUtility.SaveAsPrefabAssetAndConnect(anotherGo, localPath, InteractionMode.AutomatedAction);
    }
    void OnEnable()
    {
        CreateFolders();
        if (GraphicsSettings.currentRenderPipeline)
        {
            if (GraphicsSettings.currentRenderPipeline.GetType().ToString().Contains("HighDefinition"))
            {
                Debug.Log("HDRP active");
                //EditorUtility.DisplayDialog("Render Pipeline Error", "You are using the Built in render pipeline when you should be using the High Definetion Render Pipeline", "Ok");
            }
            else
            {
                Debug.LogError("URP active");
                EditorUtility.DisplayDialog("Render Pipeline Error", "You are using the Universal Render Pipeline when you should be using the High Definetion Render Pipeline", "Ok");
            }
        }
        else
        {
            Debug.LogError("Built-in RP active");
            EditorUtility.DisplayDialog("Render Pipeline Error", "You are using the Built in render pipeline when you should be using the High Definetion Render Pipeline", "Ok");
        }
    }
    static AGMMULTITOOL()
    {
        return;
    }
}

