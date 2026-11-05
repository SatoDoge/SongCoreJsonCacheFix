using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using BeatSaberMarkupLanguage.Settings;
using BeatSaberMarkupLanguage.Util;
using HarmonyLib;
using HMUI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SongCoreJsonCacheFix
{
    public class PluginConfig
    {
        public static PluginConfig Instance { get; set; } = new PluginConfig();

        public virtual bool ShowStatusText { get; set; } = true;
    }

    public class SettingsHost
    {
        public static readonly SettingsHost Instance = new SettingsHost();

        public bool ShowStatusText
        {
            get { return PluginConfig.Instance.ShowStatusText; }
            set
            {
                PluginConfig.Instance.ShowStatusText = value;
                StatusBanner.Refresh();
            }
        }
    }

    internal static class StatusBanner
    {
        internal const string Text = "SongCore JSON Cache Fixed!!";

        private static readonly FieldInfo TextField = typeof(TitleViewController).GetField("_text", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly Dictionary<int, TextMeshProUGUI> Labels = new Dictionary<int, TextMeshProUGUI>();

        internal static bool Visible
        {
            get { return JsonCache.Installed && PluginConfig.Instance.ShowStatusText; }
        }

        internal static void Install(Harmony harmony)
        {
            harmony.Patch(
                AccessTools.Method(typeof(TitleViewController), nameof(TitleViewController.SetText)),
                postfix: new HarmonyMethod(typeof(StatusBanner).GetMethod(nameof(AfterTitleSet), BindingFlags.Static | BindingFlags.NonPublic)));
        }

        internal static void RegisterSettings()
        {
            MainMenuAwaiter.MainMenuInitializing -= OnMainMenu;
            MainMenuAwaiter.MainMenuInitializing += OnMainMenu;
            if (MainMenuAwaiter.WaitForMainMenuAsync().IsCompleted)
            {
                OnMainMenu();
            }
        }

        private static async void OnMainMenu()
        {
            for (int i = 0; i < 60; i++)
            {
                try
                {
                    BSMLSettings settings = BSMLSettings.Instance;
                    if (settings == null)
                    {
                        await Task.Delay(500);
                        continue;
                    }
                    settings.AddSettingsMenu("SongCore JSON Cache Fix", "SongCoreJsonCacheFix.settings.bsml", SettingsHost.Instance);
                    return;
                }
                catch (InvalidOperationException)
                {
                    await Task.Delay(500);
                }
            }
        }

        internal static void Refresh()
        {
            TitleViewController[] views = Resources.FindObjectsOfTypeAll<TitleViewController>();
            for (int i = 0; i < views.Length; i++)
            {
                TextMeshProUGUI title = TitleOf(views[i]);
                Update(views[i], title != null ? title.text : null);
            }
        }

        private static void AfterTitleSet(TitleViewController __instance, string text)
        {
            Update(__instance, text);
        }

        private static void Update(TitleViewController view, string text)
        {
            if (view == null)
            {
                return;
            }
            bool show = Visible && IsRoomTitle(text);
            TextMeshProUGUI label = FindLabel(view);
            if (!show)
            {
                if (label != null)
                {
                    label.gameObject.SetActive(false);
                }
                return;
            }
            TextMeshProUGUI title = TitleOf(view);
            if (title == null)
            {
                return;
            }
            if (label == null)
            {
                label = Create(view, title);
            }
            label.gameObject.SetActive(true);
            PlaceAbove(label, view, title);
        }

        private static bool IsRoomTitle(string text)
        {
            return !string.IsNullOrEmpty(text) && text.StartsWith("MP+", StringComparison.Ordinal);
        }

        private static TextMeshProUGUI Create(TitleViewController view, TextMeshProUGUI title)
        {
            RectTransform bar = view.transform as RectTransform;
            GameObject go = new GameObject("SongCoreJsonCacheFixStatus");
            go.layer = title.gameObject.layer;
            go.transform.SetParent(bar.parent, false);
            LayoutElement layout = go.AddComponent<LayoutElement>();
            layout.ignoreLayout = true;
            TextMeshProUGUI label = go.AddComponent<TextMeshProUGUI>();
            label.text = Text;
            label.font = title.font;
            label.fontSharedMaterial = title.fontSharedMaterial;
            label.fontSize = title.fontSize;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(0.55f, 0.95f, 0.75f, 1f);
            label.raycastTarget = false;
            label.richText = false;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
            Labels[view.GetInstanceID()] = label;
            return label;
        }

        private static void PlaceAbove(TextMeshProUGUI label, TitleViewController view, TextMeshProUGUI title)
        {
            RectTransform bar = view.transform as RectTransform;
            RectTransform target = label.rectTransform;
            target.anchorMin = bar.anchorMin;
            target.anchorMax = bar.anchorMax;
            target.pivot = bar.pivot;
            target.sizeDelta = bar.sizeDelta;
            target.localRotation = bar.localRotation;
            target.localScale = bar.localScale;
            float height = bar.rect.height;
            if (height < 1f)
            {
                height = Mathf.Max(title.fontSize * 1.4f, 8f);
            }
            target.anchoredPosition = bar.anchoredPosition + new Vector2(0f, height);
        }

        private static TextMeshProUGUI FindLabel(TitleViewController view)
        {
            TextMeshProUGUI label;
            if (!Labels.TryGetValue(view.GetInstanceID(), out label))
            {
                return null;
            }
            if (label == null)
            {
                Labels.Remove(view.GetInstanceID());
                return null;
            }
            return label;
        }

        private static TextMeshProUGUI TitleOf(TitleViewController view)
        {
            return TextField.GetValue(view) as TextMeshProUGUI;
        }
    }
}
