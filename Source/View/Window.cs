﻿using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using HyperEdit.Source.View;

namespace HyperEdit.View
{
    public static class WindowHelper
    {
        public static void Prompt(string prompt, Action<string> complete, ViewOptionalOptions viewOptionalOptions = null)
        {
            var str = "";

            Window.Create(prompt,
                ViewOptionalOptions.Merge(
                    new ViewOptionalOptions
                    { 
                        Width = 200,
                        Height = 100
                    },
                    viewOptionalOptions
                ),
                w =>
                {
                    str = GUILayout.TextField(str);
                    if (GUILayout.Button("OK"))
                    {
                        complete(str);
                        w.Close();
                    }
                });
        }

        public static void Error(string message, ViewOptionalOptions viewOptionalOptions = null)
        {
            Window.Create("Error",
                ViewOptionalOptions.Merge(
                    new ViewOptionalOptions
                    {
                        Width = 400,
                    },
                    viewOptionalOptions
                ),
                w =>
                {
                    GUILayout.Label(message);
                    if (GUILayout.Button("OK"))
                    {
                        w.Close();
                    }
            });
        }

        public static void Selector<T>(string title, IEnumerable<T> elements, Func<T, string> nameSelector,
            Action<T> onSelect)
        {
            var collection = elements.Select(t => new {value = t, name = nameSelector(t)}).ToList();
            var scrollPos = new Vector2();
            Window.Create(title,
                new ViewOptionalOptions
                {
                    Width = 300,
                    Height = 500
                },
                w =>
                {
                    scrollPos = GUILayout.BeginScrollView(scrollPos);
                    foreach (var item in collection)
                    {
                        if (GUILayout.Button(item.name))
                        {
                            onSelect(item.value);
                            w.Close();
                            return;
                        }
                    }
                    GUILayout.EndScrollView();
            });
        }
    }

    public class Window : MonoBehaviour
    {
        private static GameObject _gameObject;

        internal static GameObject GameObject
        {
            get
            {
                if (_gameObject == null)
                {
                    _gameObject = new GameObject("HyperEditWindowManager");
                    DontDestroyOnLoad(_gameObject);
                }
                return _gameObject;
            }
        }

        private static ConfigNode _windowPos;

        private static ConfigNode WindowPos
        {
            get
            {
                if (_windowPos != null)
                    return _windowPos;
                var fp = IoExt.GetPath("windowpos.cfg");
                if (System.IO.File.Exists(fp))
                {
                    _windowPos = ConfigNode.Load(fp);
                    _windowPos.name = "windowpos";
                }
                else
                    _windowPos = new ConfigNode("windowpos");
                return _windowPos;
            }
        }

        private static void SaveWindowPos()
        {
            WindowPos.Save();
        }

        public static event Action<bool> AreWindowsOpenChange;

        private string _tempTooltip;
        private string _oldTooltip;
        internal string Title;
        internal string UniqueId;
        private bool _shrinkHeight;
        private Rect _windowRect;
        private Action<Window> _drawFunc;
        private bool _isOpen;

        public static void Create(string title, ViewOptionalOptions optionalOptions, Action<Window> drawFunc)
        {
            var allOpenWindows = GameObject.GetComponents<Window>();
            if (!string.IsNullOrEmpty(optionalOptions.UniqueId) && allOpenWindows.Any(w => w.UniqueId == optionalOptions.UniqueId))
            {
                Extensions.Log("Not opening window \"" + title + "\", already open");
                return;
            }

            var winx = 100;
            var winy = 100;

            var width = optionalOptions.Width ?? -1;
            var height = optionalOptions.Height ?? -1;

            if (optionalOptions.SavePosition ?? false)
            {
                var winposNode = WindowPos.GetNode(title.Replace(' ', '_'));
                if (winposNode != null)
                {
                    winposNode.TryGetValue("x", ref winx, int.TryParse);
                    winposNode.TryGetValue("y", ref winy, int.TryParse);
                }
                else
                {
                    Extensions.Log("No winpos found for \"" + title + "\", defaulting to " + winx + "," + winy);
                }
                if (winx >= Screen.width - width)
                    winx = Screen.width - width;
                if (height == -1)
                {
                    if (winy >= Screen.height - 100)
                        winy = (Screen.height - 100) / 2;
                }
                else
                {
                    if (winy > Screen.height - height)
                        winy = Screen.height - height;
                }
                Extensions.Log("Screen.width: " + Screen.width.ToString() + " width: " + width.ToString() + "   winx: " + winx.ToString());
                Extensions.Log("Screen.height: " + Screen.height.ToString() + " height: " + height.ToString() + "   winy: " + winy.ToString());
            }
            else
            {
                winx = (Screen.width - width)/2;
                winy = (Screen.height - height)/2;
            }

            var window = GameObject.AddComponent<Window>();
            window._isOpen = true;
            window._shrinkHeight = height == -1;
            if (window._shrinkHeight)
                height = 5;
            window.Title = title;
            window.UniqueId = optionalOptions.UniqueId;
            window._windowRect = new Rect(winx, winy, width, height);
            window._drawFunc = drawFunc;
            if (allOpenWindows.Length == 0)
                AreWindowsOpenChange?.Invoke(true);
        }

        private Window()
        {
            GameEvents.onScreenResolutionModified.Add(OnScreenResolutionModified);
        }

        void OnScreenResolutionModified(int x, int y)
        {
            if (this._windowRect.y >= Screen.height)
                _windowRect.y = Screen.height - _windowRect.height;
            if (this._windowRect.x >= Screen.width)
                _windowRect.x = Screen.width - _windowRect.width;

        }

        public void Update()
        {
            if (_shrinkHeight)
                _windowRect.height = 5;
            _oldTooltip = _tempTooltip;
        }

        public void OnGUI()
        {
            GUI.skin = HighLogic.Skin;
            _windowRect = GUILayout.Window(GetInstanceID(), _windowRect, DrawWindow, Title, GUILayout.ExpandHeight(true));

            if (string.IsNullOrEmpty(_oldTooltip))
                return;
            var rect = new Rect(_windowRect.xMin, _windowRect.yMax, _windowRect.width, 50);
            GUI.Label(rect, _oldTooltip);
        }

        private void DrawWindow(int windowId)
        {
            GUILayout.BeginVertical();
            if (GUI.Button(new Rect(_windowRect.width - 18, 2, 16, 16), "X")) // X button from mechjeb
                Close();
            _drawFunc(this);

            _tempTooltip = GUI.tooltip;

            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        public void Close()
        {
            var node = new ConfigNode(Title.Replace(' ', '_'));
            node.AddValue("x", (int) _windowRect.x);
            node.AddValue("y", (int) _windowRect.y);
            if (WindowPos.SetNode(node.name, node) == false)
                WindowPos.AddNode(node);
            SaveWindowPos();
            _isOpen = false;
            GameEvents.onScreenResolutionModified.Remove(OnScreenResolutionModified);
            Destroy(this);
            if (GameObject.GetComponents<Window>().Any(w => w._isOpen) == false)
                AreWindowsOpenChange?.Invoke(false);
        }

        internal static void CloseAll()
        {
            foreach (var window in GameObject.GetComponents<Window>())
                window.Close();
        }
    }
}