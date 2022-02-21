using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using MVR.FileManagementSecure;
using static LiveReload.Utils;

namespace LiveReload
{
    internal class LivePlugin
    {
        private string _pluginFullPath;
        private string _pluginPath;
        private string _pluginDir;

        private FileSearch _fileSearch;
        private Atom _atom;
        private string _pluginStoreId;
        private Button _reloadButton;

        private Dictionary<string, byte[]> _files;

        private UIDynamic _upperLeftSpacer;
        private UIDynamic _upperRightSpacer;
        private JSONStorableString _headerText;
        public InputField headerTextField;
        private JSONStorableBool _logReloads;
        public JSONStorableBool monitoringOn;
        private JSONStorableString _statusText;
        private UIDynamic _lowerLeftSpacer;

        public bool WaitingForUIOpened { get; set; }

        public LivePlugin(string pluginFullPath, string atomUid)
        {
            _pluginFullPath = pluginFullPath;
            var arr = _pluginFullPath.Split('/');
            _pluginPath = string.Join(@"\", arr).Replace($@"\{arr[arr.Length - 1]}", "");
            _pluginDir = arr[arr.Length - 2];

            _atom = SuperController.singleton.GetAtoms().Find(x => x.uid == atomUid);
            if(_atom == null)
            {
                LogError($"atom '{atomUid}' not found in scene!");
                return;
            }

            _fileSearch = new FileSearch(
                _pluginPath,
                //todo configurable
                new List<string>
                {
                    { "*.cs" },
                    { "*.cslist" },
                    { "*.json" }
                },
                //todo from gitignore
                new List<string>
                {
                    { $@"{_pluginPath}\.git" },
                    { $@"{_pluginPath}\.vscode" },
                    { $@"{_pluginPath}\bin" },
                    { $@"{_pluginPath}\obj" }
                }
            );

            WaitingForUIOpened = false;
        }

        public void AddToUI(MVRScript script)
        {
            _upperLeftSpacer = script.NewSpacer(10);
            _upperRightSpacer = script.NewSpacer(10, true);

            string header = _atom.name == "CoreControl"
                ? _pluginDir
                : $"{_atom.uid}: {_pluginDir}";

            _headerText = script.NewTextField("Plugin", header, 36, 50);
            _headerText.dynamicText.textColor = Color.black;
            _headerText.dynamicText.backgroundColor = Color.white;

            headerTextField = UI.NewInputField(_headerText.dynamicText);
            headerTextField.interactable = false;

            monitoringOn = script.NewToggle("Monitoring on", true);
            _logReloads = script.NewToggle("Output changes to Message Log", false);
            _statusText = script.NewTextField("Status", "", 24, 255, true);

            _lowerLeftSpacer = script.NewSpacer(60);
        }

        public void RemoveFromUI(MVRScript script)
        {
            script.RemoveSpacer(_upperLeftSpacer);
            script.RemoveSpacer(_upperRightSpacer);
            script.RemoveTextField(_headerText);
            script.RemoveToggle(monitoringOn);
            script.RemoveToggle(_logReloads);
            script.RemoveTextField(_statusText);
            script.RemoveSpacer(_lowerLeftSpacer);
        }

        public bool Present()
        {
            var plugin = (MVRScript) _atom.GetStorableByID(_pluginStoreId);
            return plugin != null;
        }

        public void CheckDiff()
        {
            List<string> paths = new List<string>(_files.Keys);
            bool reload = false;

            foreach(var path in paths)
            {
                byte[] contents = null;
                try
                {
                    contents = FileManagerSecure.ReadAllBytes(path);
                }
                catch(Exception)
                {
                }

                if(contents != null && !contents.SequenceEqual(_files[path]))
                {
                    _files[path] = contents;
                    _statusText.val =
                        $"{path.Replace(_pluginPath, "").TrimStart('\\')} changed\n" +
                        $"{_statusText.val}";
                    if(_logReloads.val)
                    {
                        SuperController.LogMessage($"{_pluginDir} reloading: {path.Replace(_pluginPath, "").TrimStart('\\')} changed");
                    }
                    reload = true;
                }
            }

            if(reload)
            {
                Reload();
            }
        }

        public void Reload()
        {
            try
            {
                _fileSearch.FindFiles(_files); //ensure any new files added before this reload will be monitored for changes
                _reloadButton.onClick.Invoke();
                _reloadButton = FindReloadButton(); // ensure reload button is correct after reloading plugin
            }
            catch(Exception e)
            {
                //todo actual name parsing
                LogError($"Error reloading plugin {_pluginStoreId} on atom {_atom.uid}: {e}");
            }
        }

        public void TryFindReloadButton()
        {
            try
            {
                _reloadButton = FindReloadButton();
                if(_reloadButton != null && WaitingForUIOpened)
                {
                    LogMessage($"Enabled for {_pluginFullPath}.");
                    WaitingForUIOpened = false;
                    _statusText.val = "";
                }
            }
            catch(Exception e)
            {
                LogError($"Unable to find reload button for plugin: {e}");
            }
        }

        public void FindFiles()
        {
            if(_files == null)
            {
                _files = new Dictionary<string, byte[]>();
            }
            _fileSearch.FindFiles(_files);
        }

        private Button FindReloadButton()
        {
            var pluginManager = FindPluginManagerAndSetStoreId();
            if(pluginManager == null)
            {
                return null;
            }
            var pluginListPanel = pluginManager.pluginListPanel;

            foreach(Transform pluginPanel in pluginListPanel)
            {
                var pluginPanelContent = pluginPanel.Find("Content");
                foreach(Transform scriptPanel in pluginPanelContent)
                {
                    var uidTransform = scriptPanel.Find("UID");
                    if(_pluginStoreId == uidTransform.GetComponent<Text>().text)
                    {
                        var buttonTransform = pluginPanel.Find("ReloadButton");
                        Button button = pluginPanel.Find("ReloadButton").GetComponent<Button>();
                        if(button != null)
                        {
                            return button;
                        }
                    }
                }
            }

            return null;
        }

        private MVRPluginManager FindPluginManagerAndSetStoreId()
        {
            var plugins = FindPluginsOnAtom(_atom);
            var firstPluginOfEachGroup = new List<MVRScript>();
            foreach(var plugin in plugins)
            {
                string id = plugin.storeId.Substring(0, plugin.storeId.IndexOf('_'));
                if(!firstPluginOfEachGroup.Exists(x => x.storeId.StartsWith(id)))
                {
                    firstPluginOfEachGroup.Add(plugin);
                }
            }

            for(int i = 0; i < firstPluginOfEachGroup.Count; i++)
            {
                var plugin = firstPluginOfEachGroup[i];
                var manager = plugin.manager;
                var pluginListPanel = manager.pluginListPanel;
                if(pluginListPanel == null)
                {
                    if(!WaitingForUIOpened)
                    {
                        LogMessage($"Open the UI of atom '{_atom.uid}' once to enable live reloading for {_pluginFullPath}.");
                        _statusText.val = UI.Color($"<b><size=32>Disabled.\nOpen UI of atom '{_atom.uid}'</size></b>", new Color(0.5f, 0, 0));
                        WaitingForUIOpened = true;
                    }

                    break;
                }

                var pluginsObj = manager.GetJSON()["plugins"].AsObject;
                var sortedKeys = new List<string>(pluginsObj.Keys.ToList());
                sortedKeys.Sort();
                var pluginFullPath = pluginsObj[sortedKeys[i]].Value;
                if(pluginFullPath == _pluginFullPath)
                {
                    _pluginStoreId = plugin.storeId;
                    return manager;
                }
            }

            return null;
        }
    }
}
