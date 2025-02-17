using HarmonyLib;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using CharacterCreation;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UniRx;
using UniRx.Triggers;
using Cysharp.Threading.Tasks;
using ILLGames.Unity.Component;           
using CCPoseLoader;

namespace CCCAPStateGui
{
    public static class Util
    {
        internal static readonly Il2CppSystem.Threading.CancellationTokenSource Canceler = new();
        static Action AwaitDestroy<T>(Action onSetup, Action onDestroy) where T : SingletonInitializer<T> =>
            () => SingletonInitializer<T>.Instance.gameObject
                    .GetComponentInChildren<ObservableDestroyTrigger>()
                    .AddDisposableOnDestroy(Disposable.Create(onDestroy + AwaitSetup<T>(onSetup, onDestroy)));
        static Action AwaitSetup<T>(Action onSetup, Action onDestroy) where T : SingletonInitializer<T> =>
            () => UniTask.NextFrame().ContinueWith((Action)(() => Hook<T>(onSetup, onDestroy)));
        public static void Hook<T>(Action onSetup, Action onDestroy) where T : SingletonInitializer<T> =>
            SingletonInitializer<T>.WaitUntilSetup(Canceler.Token)
                .ContinueWith(onSetup + AwaitDestroy<T>(onSetup, onDestroy));
    }
    // reference UI component
    internal static class UIRef
    {
        // window base
        internal static GameObject Window =>
            HumanCustom.Instance.CustomCharaFile.FileWindow.gameObject.transform.Find("BasePanel").gameObject;
        // pose control object
        internal static Button PoseNext =>
            HumanCustom.Instance.StateMiniSelection._posePack.PosePtn._btnNext;
        internal static Button PosePrev =>
            HumanCustom.Instance.StateMiniSelection._posePack.PosePtn._btnPrev;
        internal static Toggle PoseToggle =>
            HumanCustom.Instance.StateMiniSelection._posePack.TglPosePouse;
        // outlined text component
        internal static GameObject Text =>
            HumanCustom.Instance._txtFullName.gameObject.transform.Find("NameTitle").Find("T02-1").gameObject;
        internal static Transform PoseLayout =>
            HumanCustom.Instance.StateMiniSelection._posePack.PosePtn.gameObject.transform.parent;
        // button
        internal static GameObject Button =>
            SV.Config.ConfigWindow.Instance.transform.Find("Canvas").Find("Background").Find("MainWindow")
                .Find("Settings").Find("Scroll View").Find("Viewport").Find("Content")
                .Find("CameraSetting").Find("Content").Find("SensitivityX").Find("btnReset").gameObject;
    }
    // abstraction of labeled (categorized) status control
    internal class StateControl<TLabels, TStatus>
    {
        internal string Name;
        // internal label value to display representation
        internal Func<TLabels, string> TranslateLabel;
        // internal status value to display representation
        internal Func<TStatus, string> TranslateState;
        // next label status action
        internal Action<TLabels> Toggle;
        // current label status function
        internal Func<TLabels, TStatus> Get;
        // full list of labels
        internal IEnumerable<TLabels> Labels;
        // compose toggle button action
        // perform next label action then
        // apply current status to text component.
        internal Action ComposeAction(TLabels label, TextMeshProUGUI ui) => () =>
        {
            Toggle(label);
            ui.SetText(TranslateState(Get(label)));
        };
        internal GameObject ComposeControl(GameObject go) =>
            new GameObject(Name).With(go.transform.Wrap)
                .With(UIFactory.HorizontalLayout).With(HorizontalGroup(Labels.Count()));
        internal Action<GameObject> HorizontalGroup(int count) => go => 
            HorizontalGroup(
                new GameObject("Labels").With(go.transform.Wrap).With(VerticalLayout(count)).transform,
                new GameObject("Buttons").With(go.transform.Wrap).With(VerticalLayout(count)).transform,
                new GameObject("Status").With(go.transform.Wrap).With(VerticalLayout(count)).transform);
        internal Action<GameObject> VerticalLayout(int count) => go =>
        {
            go.AddComponent<RectTransform>();
            go.AddComponent<LayoutElement>().preferredHeight = 25 * count;
            go.AddComponent<VerticalLayoutGroup>().With(ui =>
            {
                ui.spacing = 1;
                ui.childAlignment = TextAnchor.LowerCenter;
                ui.childControlWidth = true;
                ui.childControlHeight = true;
            });
            go.AddComponent<ContentSizeFitter>().With(ui =>
            {
                ui.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                ui.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            });
        };
        // compose each label to {labels, buttons, status} control
        internal void HorizontalGroup(Transform labels, Transform buttons, Transform status) =>
            Labels.Do(label => labels.With(() => buttons.ToggleButton((() => Toggle(label)) + status.State(() => TranslateState(Get(label))))).Label(TranslateLabel(label)));

        internal void UpdateState()
        {
            if (HumanCustom.Instance.transform.Find("UI").Find("Root").Find(Plugin.Name) != null)
            {
                UpdateState(HumanCustom.Instance.transform.Find("UI")
                    .Find("Root").Find(Plugin.Name).GetChild(0).Find(Name).Find("Status")
                    .GetComponentsInChildren<TextMeshProUGUI>());
            }
        }
        internal void UpdateState(TextMeshProUGUI[] status)
        {
            for (int index = Labels.Count(); index >= 0; --index)
            {
                Enumerable.Range(0, Labels.Count()).Zip(Labels)
                    .Do(tuple => status[tuple.Item1].SetText(TranslateState(Get(tuple.Item2))));
            }
        }
    }
    // UI generation helpers
    internal static class UIFactory
    {
        internal static ConfigEntry<bool> Visibility { get; set; }
        internal static ConfigEntry<string> Translations { get; set; }
        internal static ConfigEntry<KeyboardShortcut> Toggle { get; set; }
        internal static Func<string, string> GuiTranslation;
        internal static Func<string, string> PoseTranslation;
        internal static void Wrap(this Transform tf, GameObject go) => go.transform.SetParent(tf);
        internal static void HorizontalLayout(this GameObject go)
        {
            go.AddComponent<RectTransform>();
            go.AddComponent<LayoutElement>();
            go.AddComponent<HorizontalLayoutGroup>().With(ui =>
            {
                ui.padding = new(0, 0, 10, 10);
                ui.spacing = 5;
                ui.childAlignment = TextAnchor.MiddleCenter;
                ui.childControlWidth = true;
                ui.childControlHeight = true;
            });
            go.AddComponent<ContentSizeFitter>().With(ui =>
            {
                ui.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                ui.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            });
        }
        // add button to open clothes and accessory status window
        internal static void UI(this GameObject go)
        {
            GuiTranslation = JsonSerializer.Deserialize<Dictionary<string, string>>
                (File.OpenRead(Path.Combine(Translations.Value, "gui.json").ConfigPath().ConfigPath())).GuiTranslation();
            PoseTranslation = JsonSerializer.Deserialize<Dictionary<string, string>>
                (File.OpenRead(Path.Combine(Translations.Value, "names.json").ConfigPath().ConfigPath())).PoseTranslation();
            go.GetComponent<Button>().onClick.AddListener(new GameObject(Plugin.Name).Base());
        }
        // create clothes and accessory status canvas and return show/hide action
        internal static Action Base(this GameObject go)
        {
            go.transform.SetParent(HumanCustom.Instance.transform.Find("UI").Find("Root"));
            go.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            go.AddComponent<CanvasScaler>().With(ui =>
            {
                ui.referenceResolution = new(1920, 1080);
                ui.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                ui.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            });
            go.AddComponent<GraphicRaycaster>();
            go.GetComponent<RectTransform>();
            return UnityEngine.Object.Instantiate(UIRef.Window, go.transform)
                .With(Window).With(go => go.SetActive(Visibility.Value))
                .ToggleAction();
        }
        internal static Action ToggleAction(this GameObject go) => () => go.SetActive(!go.active);
        // create draggable window
        internal static void Window(this GameObject go)
        {
            UnityEngine.Object.Destroy(go.transform.Find("Header").gameObject);
            UnityEngine.Object.Destroy(go.transform.Find("WinRect").gameObject);
            go.GetComponent<RectTransform>().With(ui =>
            {
                ui.anchorMin = new(0.0f, 1.0f);
                ui.anchorMax = new(0.0f, 1.0f);
                ui.pivot = new(0.0f, 1.0f);
                ui.sizeDelta = new(180 / 1920, 900 / 1080);
                ui.anchoredPosition = new(1600, -120);
            });
            go.GetComponent<VerticalLayoutGroup>().With(ui =>
            {
                ui.padding = new(20, 20, 0, 0);
                ui.childControlWidth = true;
                ui.childControlHeight = true;
            });
            go.AddComponent<ContentSizeFitter>().With(ui =>
            {
                ui.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                ui.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            });
            go.AddComponent<UI_DragWindow>().rtMove = go.GetComponent<RectTransform>();
            new GameObject("Title").With(go.transform.Wrap).With(HorizontalLayout).With(title => {
                title.AddComponent<CanvasRenderer>();
                title.AddComponent<RectTransform>();
                title.AddComponent<LayoutElement>().minHeight = 25;
                UnityEngine.Object.Instantiate(UIRef.PosePrev, title.transform).GetComponent<Button>().With(ui => {
                    ui.onClick.AddListener((Action)(() => UIRef.PosePrev.onClick.Invoke()));
                });
                UnityEngine.Object.Instantiate(UIRef.PoseNext, title.transform).With(ui => {
                    ui.onClick.AddListener((Action)(() => UIRef.PoseNext.onClick.Invoke()));
                });
                UnityEngine.Object.Instantiate(UIRef.PoseToggle, title.transform).With(ui => {
                    ui.onValueChanged.AddListener((Action<bool>)(value => UIRef.PoseToggle.onValueChanged.Invoke(value)));
                });
                UnityEngine.Object.Instantiate(UIRef.Text, title.transform).GetComponent<TextMeshProUGUI>().With(ui => {
                    ui.alignment = TextAlignmentOptions.Center;
                    ui.overflowMode = TextOverflowModes.Overflow;
                    go.AddComponent<ObservableDestroyTrigger>()
                        .AddDisposableOnDestroy(Disposable.Create(ui.PoseUpdateHook().ToDisposable()));
                });
            });
            ClothesControl.ComposeControl(go);
            AccessoryControl.ComposeControl(go);
        }
        internal static Action<string> PoseUpdateHook(this TextMeshProUGUI ui) =>
            (input) => ui.SetText(PoseTranslation(input));
        internal static Action ToDisposable(this Action<string> action)
        {
            Event.OnPoseUpdate += action;
            return () => { Event.OnPoseUpdate -= action; };
        }
        // clothes status control
        internal static StateControl<ChaFileDefine.ClothesKind, ChaFileDefine.ClothesState> ClothesControl =
             new StateControl<ChaFileDefine.ClothesKind, ChaFileDefine.ClothesState>()
             {
                 Name = "ClothesControl",
                 TranslateLabel = (input) => GuiTranslation(Enum.GetName(input)),
                 TranslateState = (input) => GuiTranslation(Enum.GetName(input)),
                 Toggle = (input) => HumanCustom.Instance?.Human?.cloth?.SetClothesStateNext(input),
                 Get = (input) => HumanCustom.Instance?.Human?.cloth?.GetClothesStateType(input) ?? ChaFileDefine.ClothesState.Naked,
                 Labels = Enum.GetValues<ChaFileDefine.ClothesKind>()
             };

        // accessory status control
        internal static StateControl<int, bool> AccessoryControl =
             new StateControl<int, bool>()
             {
                 Name = "AccessoryControl",
                 TranslateLabel = (input) => $"{GuiTranslation("Slot")}{input + 1}",
                 TranslateState = (input) => GuiTranslation(input ? "ON" : "OFF"),
                 Toggle = (input) => HumanCustom.Instance?.Human?.acs?.SetAccessoryState(input, !HumanCustom.Instance?.Human?.acs?.IsAccessory(input) ?? false),
                 Get = (input) => HumanCustom.Instance?.Human?.acs?.IsAccessory(input) ?? false,
                 Labels = Enumerable.Range(0, 20)
             };

        // create label component
        internal static void Label(this Transform tf, string label) =>
            UnityEngine.Object.Instantiate(UIRef.Text, tf).With(go => {
                go.AddComponent<LayoutElement>();
                go.GetComponent<TextMeshProUGUI>().With(ui => {
                    ui.fontSize = 15;
                    ui.autoSizeTextContainer = true;
                    ui.alignment = TextAlignmentOptions.Right;
                    ui.SetText(label);
                });
            });

        // create button component
        internal static void ToggleButton(this Transform tf, Action action) =>
            UnityEngine.Object.Instantiate(UIRef.Button, tf).With(go => {
                go.AddComponent<LayoutElement>().With(ui => {
                    ui.preferredWidth = 24;
                    ui.preferredHeight = 24;
                });
                go.GetComponent<Button>().onClick.AddListener(action);
            });

        // create state comoponent
        internal static Action State(this Transform tf, Func<string> state) => 
            UnityEngine.Object.Instantiate(UIRef.Text, tf).With(go => {
                go.AddComponent<LayoutElement>();
            }).GetComponent<TextMeshProUGUI>().With(ui => {
                ui.fontSize = 15;
                ui.autoSizeTextContainer = true;
                ui.alignment = TextAlignmentOptions.Left;
                ui.SetText(state());
           }).ComposeAction(state);
        internal static Action ComposeAction(this TextMeshProUGUI ui, Func<string> state) => () => ui.SetText(state());
        internal static Action InputCheck = () =>
            HumanCustom.Instance.transform.Find("UI").Find("Root").Find(Plugin.Name)
                .GetChild(0).gameObject.SetActive(Toggle.Value.IsDown() ? Visibility.Value = !Visibility.Value : Visibility.Value);

        internal static void Setup() =>
            UnityEngine.Object.Instantiate(UIRef.Button, UIRef.PoseLayout.With(layout => {
                layout.Find("ptnSelect").Find("InputField_Integer").With(input =>
                 {
                    input.GetComponent<RectTransform>().sizeDelta = new(80, 26);
                    input.GetComponent<TMP_InputField>().characterLimit = 5;
                });
            })).With(() => Canvas.preWillRenderCanvases += InputCheck).UI();
        internal static void Dispose() =>
            Canvas.preWillRenderCanvases -= InputCheck;
   }
    internal static class Extension
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(HumanCustom), nameof(HumanCustom.UpdateClothesState))]
        [HarmonyPatch(typeof(HumanCustom), nameof(HumanCustom.SetClothesState), typeof(HumanCustom.ClothStateSeter.State))]
        internal static void UpdateClothesStatePostfix() =>
            UIFactory.ClothesControl.UpdateState();
        [HarmonyPostfix]
        [HarmonyPatch(typeof(HumanCustom), nameof(HumanCustom.UpdateAccessoryState))]
        internal static void UpdateAccessoryStatePostfix() =>
            UIFactory.AccessoryControl.UpdateState();
        internal static string ConfigPath(this string value) =>
            Path.Combine(Paths.ConfigPath, Plugin.Name, value);
        internal static Func<string, string> GuiTranslation(this Dictionary<string, string> names) =>
            input => names.GetValueOrDefault(input) ?? input;
        internal static Func<string, string> PoseTranslation(this Dictionary<string, string> names) =>
            input => names.GetValueOrDefault(input) ?? input.Split(":")[^1];
    }
    [BepInProcess(Process)]
    [BepInDependency(CCPoseLoader.Plugin.Guid, ">=" + CCPoseLoader.Plugin.Version)]
    [BepInPlugin(Guid, Name, Version)]
    public class Plugin : BasePlugin
    {
        public const string Process = "SamabakeScramble";
        public const string Name = "CCCAPStateGui";
        public const string Guid = $"{Process}.{Name}";
        public const string Version = "1.0.1";
        private Harmony Patch;
        public override void Load()
        {
            UIFactory.Visibility = Config.Bind(new ConfigDefinition("General", "Visibility"), true);
            UIFactory.Translations = Config.Bind(new ConfigDefinition("General", "TranslationDir"), "C#");
            UIFactory.Toggle = Config.Bind("General", "Toggle CCCAPState GUI", new KeyboardShortcut(KeyCode.Tab, KeyCode.LeftShift));
            Util.Hook<HumanCustom>(UIFactory.Setup, UIFactory.Dispose);
            Patch = Harmony.CreateAndPatchAll(typeof(Extension), $"{Name}.Hooks");
        }
        public override bool Unload() => true.With(Patch.UnpatchSelf) && base.Unload();
    }
}