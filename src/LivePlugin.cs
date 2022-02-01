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
        public string PluginDir { get; set; }

        private FileSearch _fileSearch;
        private Atom _atom;

        private MVRScript _plugin;
        private string _pluginStoreId;
        private Button _reloadButton;

        private Dictionary<string, byte[]> _files;

        public InputField headerText;
        public JSONStorableBool logReloads;
        public JSONStorableBool monitoringOn;
        public JSONStorableString statusText;

        public bool WaitingForUIOpened { get; set; }

        public LivePlugin(string pluginFullPath, string atomUid)
        {
            _pluginFullPath = pluginFullPath;
            var arr = _pluginFullPath.Split('/');
            _pluginPath = string.Join(@"\", arr).Replace($@"\{arr[arr.Length - 1]}", "");
            PluginDir = arr[arr.Length - 2];

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

        public void CheckDiff()
        {
            List<string> paths = new List<string>(_files.Keys);
            bool reload = false;

            foreach(var path in paths)
            {
                byte[] contents = FileManagerSecure.ReadAllBytes(path);
                if(!contents.SequenceEqual(_files[path]))
                {
                    _files[path] = contents;
                    statusText.val =
                        $"{path.Replace(_pluginPath, "").TrimStart('\\')} changed\n" +
                        $"{statusText.val}";
                    if(logReloads.val)
                    {
                        SuperController.LogMessage($"{PluginDir} reloading: {path.Replace(_pluginPath, "").TrimStart('\\')} changed");
                    }
                    reload = true;
                }
            }

            if(reload)
            {
                try
                {
                    if(_plugin == null)
                    {
                        TryFindPlugin();
                    }
                    _reloadButton.onClick.Invoke();
                    TryFindPlugin();
                }
                catch(Exception e)
                {
                    LogError($"Error reloading plugin {_plugin.storeId} on atom {_atom.uid}: {e}");
                }
            }
        }

        public void TryFindPlugin()
        {
            try
            {
                if(_pluginStoreId == null)
                {
                    _plugin = GetFromPluginManager();
                    if(_plugin != null)
                    {
                        _pluginStoreId = _plugin.storeId;
                        _reloadButton = FindReloadButton();
                        _files = _fileSearch.GetFiles();
                        if(WaitingForUIOpened)
                        {
                            LogMessage($"Enabled for {_pluginFullPath}.");
                            WaitingForUIOpened = false;
                            statusText.val = "";
                        }
                    }
                }
                else
                {
                    _plugin = (MVRScript) _atom.GetStorableByID(_pluginStoreId);
                }
            }
            catch(Exception e)
            {
                LogError($"Unable to find plugin: {e}");
            }
        }

        private MVRScript GetFromPluginManager()
        {
            //assume plugins on atom are in the same order as plugins in manager json
            var plugins = FindPluginsOnAtom(_atom);
            for(int i = 0; i < plugins.Count; i++)
            {
                var plugin = plugins[i];
                var pluginListPanel = plugin.manager.pluginListPanel;
                if(pluginListPanel == null)
                {
                    if(!WaitingForUIOpened)
                    {
                        LogMessage($"Open the UI of atom '{_atom.uid}' once to enable live reloading for {_pluginFullPath}.");
                        statusText.val = UI.Color($"<b><size=32>Disabled.\nOpen UI of atom '{_atom.uid}'</size></b>", new Color(0.5f, 0, 0));
                        WaitingForUIOpened = true;
                    }

                    break;
                }

                var managerJson = plugin.manager.GetJSON();
                var pluginsObj = managerJson["plugins"].AsObject;
                var key = pluginsObj.Keys.ToList()[i];
                var pluginFullPath = pluginsObj[key].Value;
                if(pluginFullPath == _pluginFullPath)
                {
                    return plugin;
                }
            }

            return null;
        }

        private Button FindReloadButton()
        {
            var pluginListPanel = _plugin.manager.pluginListPanel;
            Button button = null;
            foreach(Transform pluginPanel in pluginListPanel)
            {
                var pluginPanelContent = pluginPanel.Find("Content");
                foreach(Transform scriptPanel in pluginPanelContent)
                {
                    var uidTransform = scriptPanel.Find("UID");
                    if(_pluginStoreId == uidTransform.GetComponent<Text>().text)
                    {
                        var buttonTransform = pluginPanel.Find("ReloadButton");
                        button = pluginPanel.Find("ReloadButton").GetComponent<Button>();
                    }
                }
            }

            return button;
        }
    }
}
