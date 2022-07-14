using System;
using System.Collections.Generic;
using System.Linq;
using MVR.FileManagementSecure;
using UnityEngine;
using UnityEngine.UI;
using static LiveReload.Utils;

namespace LiveReload
{
    public class LivePlugin
    {
        private readonly Atom _atom;
        private readonly MVRPluginManager _manager;

        private readonly FileSearch _fileSearch;
        private readonly string _pluginDir;
        private readonly string _pluginFullPath;
        private readonly string _pluginPath;

        private Dictionary<string, byte[]> _files;
        private JSONStorableString _headerText;
        private JSONStorableBool _logReloads;
        private UIDynamic _lowerLeftSpacer;
        private string _pluginStoreId;
        private Button _reloadButton;
        private JSONStorableString _statusText;

        private UIDynamic _upperLeftSpacer;
        private UIDynamic _upperRightSpacer;
        public InputField headerTextField;
        public JSONStorableBool monitoringOn;

        public LivePlugin(string pluginFullPath, string atomUid)
        {
            _pluginFullPath = pluginFullPath;
            string[] arr = _pluginFullPath.Split('/');
            _pluginPath = string.Join(@"\", arr).Replace($@"\{arr[arr.Length - 1]}", "");
            _pluginDir = arr[arr.Length - 2];

            _atom = SuperController.singleton.GetAtoms().Find(x => x.uid == atomUid);
            if(_atom == null)
            {
                LogError($"atom '{atomUid}' not found in scene!");
                return;
            }

            _manager = _atom.GetComponentInChildren<MVRPluginManager>();
            _fileSearch = new FileSearch(
                _pluginPath,
                //todo configurable
                new List<string>
                {
                    "*.cs",
                    "*.cslist",
                    "*.json",
                },
                //todo from gitignore
                new List<string>
                {
                    $@"{_pluginPath}\.git",
                    $@"{_pluginPath}\.vscode",
                    $@"{_pluginPath}\bin",
                    $@"{_pluginPath}\obj",
                }
            );

            waitingForUIOpened = false;
        }

        public bool waitingForUIOpened { get; private set; }

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
            var pluginsObj = _manager.GetJSON()["plugins"].AsObject;
            bool present = pluginsObj.Keys
                .Select(key => pluginsObj[key].Value)
                .Any(path => path == _pluginFullPath);

            if(!present)
            {
                // ensure reload button is correct if plugin in error state
                _reloadButton = FindReloadButton();
            }

            return present;
        }

        public void CheckDiff()
        {
            var paths = new List<string>(_files.Keys);
            bool reload = false;

            foreach(string path in paths)
            {
                byte[] contents = null;
                try
                {
                    contents = FileManagerSecure.ReadAllBytes(path);
                }
                catch(Exception)
                {
                    // ignored
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

        private void Reload()
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
                if(_reloadButton != null && waitingForUIOpened)
                {
                    LogMessage($"Enabled for {_pluginFullPath}.");
                    waitingForUIOpened = false;
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
            foreach(Transform transform in _manager.pluginListPanel)
            {
                var urlTransform = transform.Find("Panel").Find("URL");
                if(urlTransform.GetComponent<Text>().text == _pluginFullPath)
                {
                    var buttonTransform = transform.Find("ReloadButton");
                    return buttonTransform.GetComponent<Button>();
                }
            }

            return null;
        }
    }
}
