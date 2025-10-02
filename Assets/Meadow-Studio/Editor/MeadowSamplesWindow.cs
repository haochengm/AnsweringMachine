using System;
using System.Linq;
using Meadow.Studio;
using Newtonsoft.Json.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class MeadowSamplesWindow : EditorWindow
{
    private JObject metadata;
    readonly SampleService samplesService = new();
    readonly PluginUtils pluginUtil = new();

    [MenuItem("Meadow/Samples", false, 102)]
    private static void OpenSamplesWindow()
    {
        MeadowSamplesWindow window = CreateWindow("Meadow Samples", 613, 613);
        window.UpdateSamplesList();
    }

    private void OnEnable()
    {
        UpdateSamplesList();
    }

    private static MeadowSamplesWindow CreateWindow(string title, float width = 800, float height = 400)
    {
        MeadowSamplesWindow wnd = GetWindow<MeadowSamplesWindow>(true, title, true);

        // Center in Unity Editor main window
        var main = EditorGUIUtility.GetMainWindowPosition();
        float centerX = main.x + (main.width - width) / 2;
        float centerY = main.y + (main.height - height) / 2;

        // Set window position and size
        wnd.position = new Rect(centerX, centerY, width, height);

        wnd.minSize = new Vector2(width, height);
        wnd.maxSize = new Vector2(width, height);

        return wnd;
    }

    private void CreateSamplesUI(JObject metadata)
    {
        rootVisualElement.Clear();

        VisualTreeAsset loading = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Meadow-Studio/UI/Samples/samples-browser.uxml");
        loading.CloneTree(rootVisualElement);

        //Create the samples list
        CreateSamplesList(metadata, rootVisualElement);
    }

    private void CreateSamplesList(JObject metadata, VisualElement root)
    {
        VisualTreeAsset experienceCardAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(pluginUtil.GetPluginDir(true)+"/UI/Samples/sample-card.uxml");
        Func<VisualElement> makeItem = () => experienceCardAsset.CloneTree();
        Action<VisualElement, int> bindItem = (e, i) => 
        {
            var property = metadata.Properties().ElementAt(i);
            SetExperiencePost(e, property.Name, property.Value as JObject);
        };

        //set the refresh button
        Button refreshButton = root.Q<Button>("refresh-button");
        refreshButton.clicked += () =>
        {
            // Debug.Log("Refreshing samples list...");
            UpdateSamplesList();
        };

        //create a scrollview from the metadata
        ScrollView scrollView = new ScrollView();
        scrollView.name = "experience-list";
        scrollView.style.width = new StyleLength(new Length(100, LengthUnit.Percent));
        scrollView.style.height = new StyleLength(new Length(100, LengthUnit.Percent));
        scrollView.style.flexGrow = 0;
        // scrollView.Q<VisualElement>("unity-content-container").style.paddingBottom = 5;
        scrollView.Q<VisualElement>("unity-content-container").style.paddingLeft = 10;
        scrollView.Q<VisualElement>("unity-content-container").style.paddingRight = 10;
        scrollView.Q<VisualElement>("unity-content-container").style.paddingTop = 10;
        // scrollView.Q<VisualElement>("unity-content-container").style.flexDirection = FlexDirection.Row;
        // scrollView.Q<VisualElement>("unity-content-container").style.flexWrap = Wrap.Wrap;

        for (int i = 0; i < metadata.Properties().Count(); i++)
        {
            //check if

            VisualElement element = makeItem();
            bindItem(element, i);

            //on click event
            element.Q<VisualElement>("download-icon").RegisterCallback<ClickEvent>(evt =>
            {
                // Debug.Log($"Clicked on sample with id: " + element.name + " + package key: " + metadata[element.name]["packageKey"]);
                if (metadata[element.name]["packageKey"] == null)
                {
                    // Debug.LogError("No package key found for sample: " + element.name);
                    return;
                }
                samplesService.DownloadSampleUnityPackage(element.name, metadata[element.name]["packageKey"].ToString(), metadata[element.name]["titles"]?["en"]?.ToString() ?? metadata[element.name]["titles"]?["English"]?.ToString() ?? "Sample");
            });

            if (metadata[element.name] != null  && metadata[element.name]["packageKey"] != null)
            {
                scrollView.Add(element);
            }
        }

        //add the scrollview to the root
        VisualElement contentContainer = root.Query<VisualElement>("content-container");
        contentContainer.Add(scrollView);
    }

    private async void SetExperiencePost(VisualElement element, string id, JObject metadata)
    {
        element.name = id;
        Label titleLabel = element.Q<Label>("sample-name");
        titleLabel.text = metadata["titles"]?["en"]?.ToString() ?? metadata["titles"]?["English"]?.ToString() ?? "Untitled";
        titleLabel.text = "<u>" + titleLabel.text + "</u>";

        Label descriptionLabel = element.Q<Label>("sample-description");
        descriptionLabel.text = metadata["shortDescriptions"]?["en"]?.ToString() ?? metadata["shortDescriptions"]?["English"]?.ToString() ?? "";

        //set read more link to the meadow website formatted as "https://app.meadow.space/e/" + id
        // Label readMoreLabel = element.Q<Label>("sample-read-more");
        string readMoreUrl = "https://app.meadow.space/e/" + id;
        titleLabel.RegisterCallback<ClickEvent>(evt =>
        {
            Application.OpenURL(readMoreUrl);
        });

        //set experience thumbnail
        VisualElement thumbnail = element.Query<VisualElement>("sample-image");
        string url = $"https://storage.googleapis.com/xref-client.appspot.com/artworkdata%2F{id}%2Fimages%2Fthumbs%2Fcover_256x193";
        var imgResp = await ImageService.GetImage(url);
        if (imgResp.success)
        {
            // Debug.Log("Experience thumbnail loaded successfully for " + id);
            thumbnail.style.backgroundImage = imgResp.data as Texture2D;
        }
        else
        {
            // Debug.LogError("Error getting experience thumbnail: " + imgResp.message);
            thumbnail.style.backgroundImage = null;
        }

        //when element is hovered, change the background color
        element.RegisterCallback<MouseEnterEvent>(evt =>
        {
            element.Q<VisualElement>("download-button-container").style.display = DisplayStyle.Flex;
        });
        element.RegisterCallback<MouseLeaveEvent>(evt =>
        {
            element.Q<VisualElement>("download-button-container").style.display = DisplayStyle.None;
        });
    }

    async void UpdateSamplesList()
    {
        try
        {
            var res = await samplesService.GetAllSamplesMetadata();

            if (res.success)
            {
                metadata = res.data as JObject;
                CreateSamplesUI(metadata);
            }
            else
            {
                Debug.LogError("Error:" + res.message);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Error:" + e.Message);
        }
    }
}
