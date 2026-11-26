using System;
using System.Reflection;
using DemoKit;
using Normal.Realtime;
using SocialLeading.AllInOne;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.Rendering;
using UnityEngine.UI;
using NetworkPlayer = SocialLeading.AllInOne.NetworkPlayer;
using Object = UnityEngine.Object;

/// <summary>The prefabs: characters, the networked player, its offline copies, and the task panels.</summary>
public static partial class AllInOneBuilder
{
    private const string FollowerSource = DemoRoot + "Prefabs/Network/Avatars/P4/Resources/Demo VR Player Complete Prefab Follower P4.prefab";

    // Order is part of the contract of GetPartnerTransform: children 0 to 2 are copied into 3 to 5.
    private static readonly string[] BodyChildren =
    {
        "Head", "Left Hand", "Right Hand", "Head Partner", "Left Hand Partner", "Right Hand Partner",
        "Left Hand Partner Slow", "Right Hand Partner Slow", "Head Modulated", "Left Hand Modulated", "Right Hand Modulated"
    };

    // Wrist calibration of the controller rig.
    private static readonly Quaternion LeftWrist = new Quaternion(0.53567934f, 0.46157086f, 0.53567934f, 0.46157086f);
    private static readonly Quaternion RightWrist = new Quaternion(-0.53567934f, 0.4615709f, 0.53567934f, -0.4615709f);

    // Characters ----------------------------------------------------------------------------------------------

    /// <summary>Variants of the IK characters that differ only where the integrated demo needs it.</summary>
    private static void BuildCharacters()
    {
        for (int k = 1; k <= AvatarCount; k++)
        {
            var source = Load<GameObject>(DemoRoot + "Characters/IK Prefabs/VR Character IK " + k + ".prefab");
            var character = (GameObject)PrefabUtility.InstantiatePrefab(source);
            character.name = "Demo Character " + k;
            character.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            foreach (BoneRenderer bones in character.GetComponentsInChildren<BoneRenderer>(true)) bones.enabled = false;
            foreach (Collider collider in character.GetComponentsInChildren<Collider>(true)) collider.enabled = false;

            var rig = character.GetComponentInChildren<IKTargetFollowVRRig>(true);
            // Some characters stop their rig while no camera sees them and come back in a wrong pose for a frame.
            rig.GetComponent<Animator>().cullingMode = AnimatorCullingMode.AlwaysAnimate;
            foreach (IKFootSolver foot in character.GetComponentsInChildren<IKFootSolver>(true)) Set(foot, ("terrainLayer.m_Bits", 1));
            // In one character the head constraint reads the head bone itself, so the head would not turn.
            var head = character.GetComponentInChildren<MultiParentConstraint>(true);
            Set(head, ("m_Data.m_SourceObjects.m_Item0.transform", rig.head.ikTarget));
            foreach (SkinnedMeshRenderer part in character.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                part.sharedMaterials = Array.ConvertAll(part.sharedMaterials, DemoMaterial);
                PrefabUtility.RecordPrefabInstancePropertyModifications(part);
            }

            PrefabUtility.SaveAsPrefabAsset(character, CharacterFolder + character.name + ".prefab");
            Object.DestroyImmediate(character);
        }
    }

    /// <summary>
    /// A copy of a character material for this demo. The imported ones are as smooth as plastic, and the highlights on hair
    /// and lashes glitter white because they ignore the transparency.
    /// </summary>
    private static Material DemoMaterial(Material source)
    {
        if (source == null) return null;
        string path = Materials + source.name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(source);
            AssetDatabase.CreateAsset(material, path);
        }
        else material.CopyPropertiesFromMaterial(source);
        if (source.renderQueue < (int)RenderQueue.Transparent) material.SetFloat("_Glossiness", 0.12f);
        else
        {
            material.SetFloat("_SpecularHighlights", 0f);
            material.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
        }
        EditorUtility.SetDirty(material);
        return material;
    }

    // Player --------------------------------------------------------------------------------------------------

    /// <summary>One networked player for every role and avatar, made from the follower prefab to keep its Normcore ids.</summary>
    private static void BuildPlayer(AppConfig config)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(FollowerSource);
        try
        {
            root.name = PlayerName;
            root.tag = "Untagged";
            foreach (IKTargetFollowVRRig rig in root.GetComponentsInChildren<IKTargetFollowVRRig>(true)) Object.DestroyImmediate(rig.gameObject);
            foreach (SetCameraParent camera in root.GetComponentsInChildren<SetCameraParent>(true)) Object.DestroyImmediate(camera.gameObject);
            foreach (string name in new[] { "Head Modulated", "Left Hand Modulated", "Right Hand Modulated" })
            {
                Transform part = Child(root.transform, name);
                foreach (LocalFollowSystem c in part.GetComponents<LocalFollowSystem>()) Object.DestroyImmediate(c);
                foreach (SlowFollowSystem c in part.GetComponents<SlowFollowSystem>()) Object.DestroyImmediate(c);
                foreach (HandAddedFollowSystem c in part.GetComponents<HandAddedFollowSystem>()) c.maxOffset = 0.5f;
                foreach (HeadAddedFollowSystem c in part.GetComponents<HeadAddedFollowSystem>())
                {
                    c.releaseTime = 0.4f;
                    c.ignoreAboveSpeed = 6f;
                }
            }
            CheckBodyChildren(root.transform);

            var voice = root.GetComponentInChildren<RealtimeAvatarVoice>(true);
            voice.enabled = false;

            var applier = root.AddComponent<ModulationProfileApplier>();
            var body = root.AddComponent<AvatarBody>();
            var dresser = root.AddComponent<HumanoidDresser>();
            root.AddComponent<HandPoseDriver>();
            var player = root.AddComponent<NetworkPlayer>();
            FillBody(body, config, applier, dresser);
            Set(player, ("config", config), ("voice", voice));

            RebuildRealtimeGraph(root.GetComponent<RealtimeView>());
            CheckView(root.GetComponent<RealtimeView>(), root.transform);
            PrefabUtility.SaveAsPrefabAsset(root, PlayerPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void FillBody(AvatarBody body, AppConfig config, ModulationProfileApplier applier, AvatarDresser dresser)
    {
        Transform root = body.transform;
        body.head = Child(root, "Head");
        body.leftHand = Child(root, "Left Hand");
        body.rightHand = Child(root, "Right Hand");
        body.headPartner = Child(root, "Head Partner");
        body.leftPartner = Child(root, "Left Hand Partner");
        body.rightPartner = Child(root, "Right Hand Partner");
        body.leftPartnerSlow = Child(root, "Left Hand Partner Slow");
        body.rightPartnerSlow = Child(root, "Right Hand Partner Slow");
        body.headOut = Child(root, "Head Modulated");
        body.leftOut = Child(root, "Left Hand Modulated");
        body.rightOut = Child(root, "Right Hand Modulated");
        body.link = body.GetComponent<GetPartnerTransform>();
        body.applier = applier;
        body.dresser = dresser;
        Set(body, ("config", config));
    }

    /// <summary>The same body without any network component, for the preview and for simulated partners.</summary>
    private static void BuildBody()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PlayerPath);
        try
        {
            root.name = "Avatar Body";
            foreach (NetworkPlayer c in root.GetComponentsInChildren<NetworkPlayer>(true)) Object.DestroyImmediate(c);
            foreach (RealtimeAvatar c in root.GetComponentsInChildren<RealtimeAvatar>(true)) Object.DestroyImmediate(c);
            foreach (RealtimeAvatarVoice c in root.GetComponentsInChildren<RealtimeAvatarVoice>(true)) Object.DestroyImmediate(c);
            foreach (RealtimeTransform c in root.GetComponentsInChildren<RealtimeTransform>(true)) Object.DestroyImmediate(c);
            foreach (RealtimeView c in root.GetComponentsInChildren<RealtimeView>(true)) Object.DestroyImmediate(c);
            PrefabUtility.SaveAsPrefabAsset(root, BodyPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void BuildBot()
    {
        var bot = (GameObject)PrefabUtility.InstantiatePrefab(Load<GameObject>(BodyPath));
        bot.name = "Simulated Partner";
        var body = bot.GetComponent<AvatarBody>();
        var rig = bot.AddComponent<SimulatedRig>();
        rig.head = body.head;
        rig.leftHand = body.leftHand;
        rig.rightHand = body.rightHand;
        rig.eyeHeight = 1.6f;
        rig.leftWristOffset = LeftWrist;
        rig.rightWristOffset = RightWrist;
        bot.AddComponent<BotMover>();
        bot.AddComponent<BotPartner>();
        PrefabUtility.SaveAsPrefabAsset(bot, BotPath);
        Object.DestroyImmediate(bot);
    }

    /// <summary>Normcore keeps a list of the components and child views. Adding components from code does not update it.</summary>
    private static void RebuildRealtimeGraph(RealtimeView view)
    {
        MethodInfo rebuild = typeof(RealtimeView).GetMethod("RebuildRealtimeGraph", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert(rebuild != null, "RealtimeView.RebuildRealtimeGraph was not found.");
        rebuild.Invoke(view, null);
        EditorUtility.SetDirty(view);
    }

    private static void CheckBodyChildren(Transform root)
    {
        for (int i = 0; i < BodyChildren.Length; i++)
            Assert(root.childCount > i && root.GetChild(i).name == BodyChildren[i], "child " + i + " of the player must be " + BodyChildren[i]);
    }

    private static void CheckView(RealtimeView view, Transform root)
    {
        var serialized = new SerializedObject(view);
        SerializedProperty components = serialized.FindProperty("_components");
        var expected = new Component[] { root.GetComponent<RealtimeAvatar>(), root.GetComponent<RealtimeTransform>(), root.GetComponent<NetworkPlayer>() };
        Assert(components.arraySize == expected.Length, "the player view lists " + components.arraySize + " components");
        for (int i = 0; i < expected.Length; i++)
        {
            SerializedProperty entry = components.GetArrayElementAtIndex(i);
            Assert(entry.FindPropertyRelative("componentID").intValue == i + 1 && entry.FindPropertyRelative("component").objectReferenceValue == expected[i],
                "component " + (i + 1) + " of the player view is not " + expected[i].GetType().Name);
        }
        Assert(serialized.FindProperty("_childViews").arraySize == 3, "the player view must list three child views");
    }

    // Task panels ---------------------------------------------------------------------------------------------

    private static void BuildTaskPanels()
    {
        var root = new GameObject("Dyad Task Panels");
        root.transform.SetPositionAndRotation(new Vector3(0f, 1.398f, -0.014f), Quaternion.identity);

        Page(root, "Question 1", SharedRoot + "UI/Prefabs/Question1Transition.prefab");
        GameObject rating1 = Page(root, "Rating 1", DemoRoot + "UI/Prefabs/RatingPanel1.prefab");
        SetText(rating1, "Question", "To what extent has your opinion changed after hearing your partner's arguments?");
        SetText(rating1, "Description 1", "[ No changed at all ]");
        SetText(rating1, "Description 2", "[ Very significantly changed ]");
        Page(root, "Question 2", SharedRoot + "UI/Prefabs/Question2Transition.prefab");
        GameObject rating2 = Page(root, "Rating 2", DemoRoot + "UI/Prefabs/RatingPanel2.prefab");
        SetText(rating2, "Question", "How much has your understanding of the topic increased as a result of the debate?");
        SetText(rating2, "Description 1", "[ Not increased at all ]");
        SetText(rating2, "Description 2", "[ Greatly Increased ]");
        GameObject split1 = Page(root, "Split 1", DemoRoot + "UI/Prefabs/SplitPanel1.prefab");
        SetText(split1, "Title", "Money Splitting Round 1");
        Page(root, "Offer 1", SharedRoot + "UI/Prefabs/FirstPartnerSplit.prefab");
        Page(root, "Waiting", DemoRoot + "UI/Prefabs/WaitingScreen.prefab");
        Page(root, "Reply", SharedRoot + "UI/Prefabs/PartnerResponse.prefab");
        GameObject split2 = Page(root, "Split 2", DemoRoot + "UI/Prefabs/SplitPanel2.prefab");
        SetText(split2, "Title", "Money Splitting Round 2");
        Page(root, "Offer 2", SharedRoot + "UI/Prefabs/SecondPartnerSplit.prefab");
        GameObject final = Page(root, "Final", DemoRoot + "UI/Prefabs/FinalCanvas.prefab");
        SetText(final, "PlayerSplit", "Your response has been recorded and\nyou have successfully finished the VR part!\n\nPlease inform the experimenters and they can help you take off the headset and show you the survey");

        foreach (Transform page in root.transform)
        {
            AddHotspot(page, "Confirm", DyadTaskFlow.ConfirmId);
            AddHotspot(page, "AcceptButton", DyadTaskFlow.AcceptId);
            AddHotspot(page, "RejectButton", DyadTaskFlow.RejectId);
            Slider slider = page.GetComponentInChildren<Slider>(true);
            if (slider != null) DemoHotspot.Add((RectTransform)slider.transform, DyadTaskFlow.SliderId);
            page.gameObject.SetActive(false);
        }
        PrefabUtility.SaveAsPrefabAsset(root, TaskPanelsPath);
        Object.DestroyImmediate(root);
    }

    private static GameObject Page(GameObject root, string name, string path)
    {
        var page = (GameObject)PrefabUtility.InstantiatePrefab(Load<GameObject>(path), root.transform);
        page.name = name;
        page.transform.localPosition = Vector3.zero;
        page.transform.localRotation = Quaternion.identity;
        page.transform.localScale = new Vector3(0.8f, 0.8f, 1f);
        return page;
    }

    private static void SetText(GameObject page, string child, string text)
    {
        foreach (TMP_Text label in page.GetComponentsInChildren<TMP_Text>(true))
        {
            if (label.name != child) continue;
            label.text = text;
            EditorUtility.SetDirty(label);
            return;
        }
        Assert(false, page.name + " has no text " + child);
    }

    private static void AddHotspot(Transform page, string child, string id)
    {
        foreach (Image image in page.GetComponentsInChildren<Image>(true))
            if (image.name == child) DemoHotspot.Add(image.rectTransform, id, image);
    }
}
