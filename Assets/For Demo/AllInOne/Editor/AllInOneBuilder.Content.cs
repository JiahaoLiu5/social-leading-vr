using System;
using System.Collections.Generic;
using DemoKit;
using SocialLeading.AllInOne;
using UnityEditor;
using UnityEngine;
using Motion = SimulatedRig.MotionKind;

/// <summary>The content assets: catalogs, the steps of the session, and all texts.</summary>
public static partial class AllInOneBuilder
{
    private const string ClipFolder = "Assets/For Study/Sound/Instruction Recording/";

    private const string BoardHeading = "<u><smallcaps>Idea Generation and Debate</smallcaps></u>";

    private static AppConfig BuildContent()
    {
        AppConfig config = LoadOrCreate<AppConfig>(Content + "App Config.asset");
        config.lobbyDesktopYaw = 62f;
        // The original monitor camera stands in the wall at x = -3.18, behind the edge of a board frame.
        config.operatorDesktopPosition = new Vector3(-3.05f, 0f, 0f);
        config.operatorDesktopYaw = 77f;
        config.operatorDesktopPanelPosition = new Vector3(-2.30f, 1.18f, 0.87f);
        config.operatorPanelPosition = new Vector3(-1.83f, 1.35f, 0.92f);
        config.roleNote = "In the actual study, participants did not know that there were different roles or that movement was modulated. Roles were assigned at random.";
        config.keyHint = "Online rooms need a Normcore app key. See docs/VR_SETUP.md.";
        config.comfortNote = "Your viewpoint also follows a share of the Leader's steps. Take the headset off if you feel unwell.";
        config.finishNote = "In the study, the offers and replies in the last task were scripted by the program. Your answers are only on this device.";
        config.roles = BuildRoles();
        config.avatars = BuildAvatars();
        config.topologies = BuildTopologies();
        config.script = BuildScript();
        config.studyProfile = BuildProfile("Study Profile", ModulationProfile.HeadMode.Superimpose);
        config.playProfile = BuildProfile("Free Play Profile", ModulationProfile.HeadMode.Off);
        LoadOrCreate<DyadTaskConfig>(Content + "Dyad Task Config.asset");
        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();
        return config;
    }

    private static RoleCatalog BuildRoles()
    {
        RoleCatalog catalog = LoadOrCreate<RoleCatalog>(Content + "Role Catalog.asset");
        catalog.roles = new[]
        {
            new RoleCatalog.Entry
            {
                id = "leader", label = "Leader", tag = "LeaderTag", seatPosition = new Vector3(0f, 0f, -1.5f), seatYaw = 0f,
                sentence = "Your avatar moves exactly as you do. A share of your movement also moves the other person's avatar."
            },
            new RoleCatalog.Entry
            {
                id = "follower", label = "Follower", tag = "FollowerTag", seatPosition = new Vector3(0f, 0f, 1.5f), seatYaw = 180f,
                sentence = "Your avatar is mostly your own movement, plus a share of the Leader's movement."
            }
        };
        EditorUtility.SetDirty(catalog);
        return catalog;
    }

    private static AvatarCatalog BuildAvatars()
    {
        AvatarCatalog catalog = LoadOrCreate<AvatarCatalog>(Content + "Avatar Catalog.asset");
        catalog.avatars = new AvatarCatalog.Entry[AvatarCount];
        for (int k = 1; k <= AvatarCount; k++)
        {
            catalog.avatars[k - 1] = new AvatarCatalog.Entry
            {
                id = "avatar-" + k,
                label = "Avatar " + k,
                resourcePath = "AllInOne/Characters/Demo Character " + k,
                thumbnail = AssetDatabase.LoadAssetAtPath<Texture2D>(Content + "Thumbnails/avatar-" + k + ".png")
            };
        }
        EditorUtility.SetDirty(catalog);
        return catalog;
    }

    private static TopologyCatalog BuildTopologies()
    {
        TopologyCatalog catalog = LoadOrCreate<TopologyCatalog>(Content + "Topology Catalog.asset");
        catalog.topologies = new[]
        {
            new TopologyCatalog.Entry { id = "leader-follower", label = "Leader to Follower", sourceOfRole = new[] { -1, 0 } },
            new TopologyCatalog.Entry { id = "follower-leader", label = "Follower to Leader", sourceOfRole = new[] { 1, -1 } },
            new TopologyCatalog.Entry { id = "both", label = "Both ways", sourceOfRole = new[] { 1, 0 } },
            new TopologyCatalog.Entry { id = "off", label = "Nobody", sourceOfRole = new[] { -1, -1 } }
        };
        EditorUtility.SetDirty(catalog);
        return catalog;
    }

    /// <summary>The values of the study prefabs. Free play starts without the position channel.</summary>
    private static ModulationProfile BuildProfile(string name, ModulationProfile.HeadMode position)
    {
        ModulationProfile profile = LoadOrCreate<ModulationProfile>(Content + name + ".asset");
        profile.hands = ModulationProfile.HandMode.Blend;
        profile.handRatio = 0.3f;
        profile.handAxisRatio = Vector3.one;
        profile.position = position;
        profile.positionRatio = 0.3f;
        profile.maxPositionOffset = 0f;
        profile.partnerSmoothing = 0.2f;
        EditorUtility.SetDirty(profile);
        return profile;
    }

    // Steps ---------------------------------------------------------------------------------------------------

    private static StudyScript BuildScript()
    {
        StudyScript script = LoadOrCreate<StudyScript>(Content + "Dyad Study Script.asset");
        var steps = new List<StudyStep>
        {
            Step("wave", "Wave and say hi", "Wave to your partner.", 10f, AdvanceRule.TimeOrAllDone,
                Say(
                    (0f, "Welcome to the social chatting room."),
                    (2.5f, "In front of you, you can see your partner."),
                    (5.3f, "Please wave and say hi to your partner.")),
                At(0f, BotCue.Place.Seat, Motion.Idle), At(5.2f, BotCue.Place.Stay, Motion.Wave)),

            Step("handshake", "Shake hands", "Walk closer and shake hands.", 15f, AdvanceRule.TimeOrAllDone,
                Say(
                    (0f, "Great! Please come closer to each other"),
                    (3.5f, "and shake hands with your partner.")),
                At(0f, BotCue.Place.Stay, Motion.Idle), At(2.5f, BotCue.Place.NearUser, Motion.Idle), After(0f, BotCue.Place.NearUser, Motion.Handshake)),

            Step("highfive", "High five", "High five.", 10f, AdvanceRule.TimeOrAllDone,
                Say(
                    (0f, "Excellent! Today, you will be completing several interaction tasks with your partner."),
                    (6.2f, "Please high-five with your partner to mark the beginning of today's journey.")),
                Near(At(0f, BotCue.Place.NearUser, Motion.Idle), 0.75f), Near(At(7.4f, BotCue.Place.NearUser, Motion.HighFive), 0.75f)),

            // The clip names the movements one after the other, so the partner moves after the words.
            Step("mirror", "Movements at the mirror", "Turn to the mirror and follow the movements.", 15f, AdvanceRule.TimeOrAllDone,
                Say(
                    (0f, "Great! On one side of the room, there is a mirror."),
                    (4.5f, "Please turn to the mirror side and perform the following movements."),
                    (10.7f, "Tilt and nod your head."),
                    (15.4f, "Raise your left hand above your head and then lower it down."),
                    (22.2f, "Raise your right hand above your head and then lower it down."),
                    (29.2f, "Raise your two hands above your head and then lower them down.")),
                At(0f, BotCue.Place.Stay, Motion.Idle), At(5f, BotCue.Place.MirrorSide, Motion.Idle), At(12.2f, BotCue.Place.MirrorSide, Motion.Nod),
                At(14.7f, BotCue.Place.MirrorSide, Motion.Idle), At(17f, BotCue.Place.MirrorSide, Motion.RaiseLeft),
                At(21f, BotCue.Place.MirrorSide, Motion.Idle), At(23.5f, BotCue.Place.MirrorSide, Motion.RaiseRight),
                At(27.5f, BotCue.Place.MirrorSide, Motion.Idle), At(30.5f, BotCue.Place.MirrorSide, Motion.RaiseBoth),
                After(3f, BotCue.Place.MirrorSide, Motion.Idle)),

            Timed(Step("free", "Free movement", "Move freely.", 15f, AdvanceRule.Time,
                Say(
                    (0f, "Fantastic! Now for the next 15 seconds."),
                    (4.4f, "Please relax and you can perform any movements you want"),
                    (8.1f, "to familiarize with yourself, your partner, and the environment.")),
                At(0f, BotCue.Place.Stay, Motion.Idle), Alternating(After(0f, BotCue.Place.Stay, Motion.Step), Motion.Reach, 4f))),

            Step("intro", "Introduce yourself", "Introduce yourself.", 45f, AdvanceRule.TimeOrAllDone,
                Say(
                    (0f, "Excellent! Now, please turn and face your partner."),
                    (4.5f, "You can do a self-introduction to your partner."),
                    (7.5f, "Whoever of you can start first.")),
                At(2f, BotCue.Place.Seat, Motion.Idle), Alternating(After(0f, BotCue.Place.Seat, Motion.Talk), Motion.Idle, 8f)),

            Step("intro-more", "Say more about yourself", "Say a bit more about yourself.", 60f, AdvanceRule.TimeOrAllDone,
                Say(
                    (0f, "Great! Can both of you say a little bit more about yourself?")),
                At(0f, BotCue.Place.Seat, Motion.Idle), Alternating(After(0f, BotCue.Place.Seat, Motion.Talk), Motion.Idle, 8f)),

            Cue(Step("topic", "Debate topic", "Walk to one of the two boards on the wall.", 40f, AdvanceRule.TimeOrAllDone,
                Say(
                    (0f, "Fantastic! Next, you will be doing an idea generation and debate task with your partner."),
                    (7.1f, "The topic that you will be debating is the impact of social media on social interactions."),
                    (12.8f, "Specifically, does social media bring people together or drive them further apart?"),
                    (18.1f, "There are two instruction boards on one side of the room."),
                    (21.4f, "Each of you please come to one of the boards and you can see the context information there.")),
                At(0f, BotCue.Place.Seat, Motion.Idle), At(4f, BotCue.Place.OwnBoard, Motion.Idle)), "boards"),

            Cue(Timed(Step("standpoint", "Think about your position", "The board you stand at shows your position. Think about your arguments.", 60f, AdvanceRule.Time,
                Say(
                    (0f, "Now, the point of view that you will be debating for is shown to you."),
                    (4.6f, "You will now have one minute to think about your argument"),
                    (8.2f, "and supporting evidence before we come to the debate.")),
                At(0f, BotCue.Place.OwnBoard, Motion.Idle), After(0f, BotCue.Place.Stay, Motion.Idle))), "standpoint"),

            Timed(Step("debate", "Debate", "Debate with your partner.", 180f, AdvanceRule.Time,
                Say(
                    (0f, "Great! For the next three minutes,"),
                    (3.3f, "please propose your point of view and debate with your partner."),
                    (6.9f, "You can start now.")),
                Near(At(0f, BotCue.Place.NearUser, Motion.Idle), 1.2f), Near(Alternating(After(0f, BotCue.Place.NearUser, Motion.Talk), Motion.Idle, 6f), 1.2f))),

            Task(Cue(Step("ratings", "Two questions", "Answer the two questions at the white board.", 120f, AdvanceRule.AllDone,
                Say(
                    (0f, "Great! Thanks for the exciting debate."),
                    (3.7f, "Now, you will be shown two quick questions,"),
                    (6.8f, "please come to the front of the white board that is shown in the room,"),
                    (10.8f, "and answer based on your experience."),
                    (13.7f, "Please indicate that you have finished by raising your left hand.")),
                At(0f, BotCue.Place.Seat, Motion.Idle)), "ratings", "raise-hand"), 45f, 8f),

            Step("split-brief", "Split task instruction", "Listen to the instruction.", 5f, AdvanceRule.Time,
                Say(
                    (0f, "Excellent! Please give yourself a round of applause for completing the debate task."),
                    (7.8f, "Next, you will come to the final task, which is a money-splitting task with your partner."),
                    (14.1f, "Here is the instruction for the task. Please listen carefully."),
                    (18.6f, "A pool of $100 will be split between you two."),
                    (23.3f, "There will be two rounds of splitting."),
                    (25.8f, "In each round, both of you will propose splits."),
                    (29.4f, "Then both of your splits will either be accepted or rejected by your partner."),
                    (34.1f, "You will also know your partner's decision on your proposed split later."),
                    (38.6f, "If the split is accepted, the money will be shared as it is."),
                    (43.2f, "While if the split is rejected, then neither of you would receive the money.")),
                At(0f, BotCue.Place.Seat, Motion.Idle), At(4f, BotCue.Place.Seat, Motion.Clap), At(7.5f, BotCue.Place.Seat, Motion.Idle)),

            Task(Cue(Step("split", "Split task", "Propose splits and decide on your partner's splits.", 180f, AdvanceRule.AllDone,
                Say(
                    (0f, "When you are ready, please stay in front of the whiteboard."),
                    (3.8f, "Remember that you only stand for your own benefit in this task"),
                    (7.7f, "and will provide your own split and decision."),
                    (10.8f, "So please do not talk and discuss with your partner.")),
                At(0f, BotCue.Place.Seat, Motion.Idle)), "split"), 75f, 12f)
        };
        for (int i = 0; i < steps.Count; i++) steps[i].clip = Load<AudioClip>(ClipFolder + (i + 1) + ".mp3");
        script.steps = steps.ToArray();
        script.countdownStudy = 5f;
        script.countdownQuick = 3f;
        script.gapStudy = 1f;
        script.gapQuick = 0.5f;
        EditorUtility.SetDirty(script);
        return script;
    }

    private static StudyStep Step(string id, string title, string hint, float seconds, AdvanceRule rule, DemoCaption[] captions, params BotCue[] bots) =>
        new StudyStep { id = id, title = title, hint = hint, responseSeconds = seconds, rule = rule, captions = captions, bots = bots };

    /// <summary>The words of a clip, cut at the pauses of the recording.</summary>
    private static DemoCaption[] Say(params (float start, string text)[] pieces) => Array.ConvertAll(pieces, p => new DemoCaption(p.start, p.text));

    private static StudyStep Timed(StudyStep step)
    {
        step.showTimer = true;
        return step;
    }

    private static StudyStep Cue(StudyStep step, params string[] cues)
    {
        step.cues = cues;
        return step;
    }

    private static StudyStep Task(StudyStep step, float quickSeconds, float botSeconds)
    {
        step.quickSeconds = quickSeconds;
        step.botDoneSeconds = botSeconds;
        step.usualShare = 0.42f;
        return step;
    }

    private static BotCue At(float seconds, BotCue.Place place, Motion motion) =>
        new BotCue { clock = BotCue.Clock.Step, at = seconds, place = place, motion = motion };

    private static BotCue After(float seconds, BotCue.Place place, Motion motion) =>
        new BotCue { clock = BotCue.Clock.Response, at = seconds, place = place, motion = motion };

    private static BotCue Alternating(BotCue cue, Motion second, float interval)
    {
        cue.alternate = second;
        cue.interval = interval;
        return cue;
    }

    private static BotCue Near(BotCue cue, float distance)
    {
        cue.distance = distance;
        return cue;
    }
}
