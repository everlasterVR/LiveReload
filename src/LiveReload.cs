using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace everlaster
{
    sealed class LiveReload : Script
    {
        public override bool ShouldIgnore() => false;
        public override string className => nameof(LiveReload);

        protected override void CreateUI()
        {
            var textField = CreateTextField(new JSONStorableString("title", $"\n{nameof(LiveReload)} {VERSION}"));
            textField.UItext.fontSize = 36;
            textField.height = 120;
            textField.backgroundColor = Color.clear;
            textField.DisableScroll();

            CreateSpacer().height = 10;
            CreateToggle(logChangesJsb).label = logChangesJsb.label;
        }

        bool _isSessionPlugin;
        List<LivePlugin> _livePlugins;

        string _mainDir;
        readonly Clock _clock = new Clock(1);

        public StorableBool logChangesJsb { get; private set; }

        protected override void OnInit()
        {
            string creatorName = UserPreferences.singleton.creatorName;
            if(creatorName == null || creatorName.Trim().Length == 0)
            {
                OnInitError("Set your creator name in user preferences and reload the plugin.", false);
                return;
            }

            _isSessionPlugin = containingAtom.type == "SessionPluginManager";
            _mainDir = $@"Custom\Scripts\{creatorName}";

            logChangesJsb = new StorableBool("logDetectedChanges", "Log Detected Changes", false);
            RegisterBool(logChangesJsb);

            _livePlugins = new List<LivePlugin>();
            BuildLivePluginsList();
            initialized = true;
        }

        bool _isFocused;
        void OnApplicationFocus(bool isFocused)
        {
            _isFocused = isFocused;
        }

        Dictionary<string, List<string>> FindPluginsInSceneJSON()
        {
            try
            {
                var result = new Dictionary<string, List<string>>();
                var atomsJSONArray = SuperController.singleton.GetSaveJSON().AsObject["atoms"].AsArray;
                foreach(JSONClass atomJSON in atomsJSONArray)
                {
                    // skip other atoms if not added as scene/session plugin
                    if(containingAtom.name != "CoreControl" && containingAtom.uid != atomJSON["id"].Value)
                    {
                        continue;
                    }

                    var plugins = FindPluginsInAtomJson(atomJSON);
                    if(plugins != null && plugins.Any())
                    {
                        result[atomJSON["id"]] = plugins;
                    }
                }

                return result;
            }
            catch(Exception e)
            {
                logBuilder.Exception(e);
            }

            return null;
        }

        List<string> FindPluginsInAtomJson(JSONClass atomJSON)
        {
            var managerJSON = FindPluginManagerJSON(atomJSON["storables"].AsArray);
            if(managerJSON != null)
            {
                return FindPluginsInManagerJSON(managerJSON);
            }

            return null;
        }

        static JSONClass FindPluginManagerJSON(JSONArray storablesJSONArray)
        {
            foreach(JSONClass storableJSON in storablesJSONArray)
            {
                if(storableJSON["id"].Value == "PluginManager")
                {
                    return storableJSON;
                }
            }

            return null;
        }

        List<string> FindPluginsInManagerJSON(JSONClass managerJson)
        {
            var result = new List<string>();
            var pluginsObj = managerJson["plugins"].AsObject;
            foreach(string key in pluginsObj.Keys)
            {
                string pluginFullPath = pluginsObj[key].Value;
                if(
                    !pluginFullPath.Contains(nameof(LiveReload)) &&
                    pluginFullPath.StartsWith(_mainDir.Replace(@"\", "/"))
                )
                {
                    result.Add(pluginFullPath);
                }
            }

            return result;
        }

        MVRPluginManager FindManager(string atomUid)
        {
            MVRPluginManager result = null;
            if(_isSessionPlugin && atomUid == "Session")
            {
                result = manager;
            }
            else if(atomUid == "CoreControl")
            {
                var foundAtom = SuperController.singleton.GetAtoms().Find(atom => atom.uid == atomUid);
                var managers = foundAtom.GetComponentsInChildren<MVRPluginManager>();
                foreach(var mgr in managers)
                {
                    if(mgr.name == "ScenePluginManager")
                    {
                        result = mgr;
                    }
                }
            }
            else
            {
                var foundAtom = SuperController.singleton.GetAtoms().Find(atom => atom.uid == atomUid);
                result = foundAtom.GetComponentInChildren<MVRPluginManager>();
            }

            return result;
        }

        void BuildLivePluginsList()
        {
            try
            {
                var pluginsByAtom = FindPluginsInSceneJSON();
                if(_isSessionPlugin)
                {
                    var sessionPlugins = FindPluginsInManagerJSON(manager.GetJSON());
                    if(sessionPlugins.Any())
                    {
                        pluginsByAtom["Session"] = sessionPlugins;
                    }
                }

                foreach(var kvp in pluginsByAtom)
                {
                    string atomUid = kvp.Key;
                    var atomPlugins = kvp.Value;
                    foreach(string pluginFullPath in atomPlugins)
                    {
                        var existingLivePlugin = _livePlugins.Find(existing => existing.Uid() == $"{atomUid}:{pluginFullPath}");
                        if(existingLivePlugin == null)
                        {
                            _livePlugins.Add(new LivePlugin(this, atomUid, pluginFullPath, FindManager(atomUid)));
                        }
                    }
                }
            }
            catch(Exception e)
            {
                logBuilder.Exception(e);
            }
        }
        void Update()
        {
            if(!initialized || !_isFocused)
            {
                return;
            }

            try
            {
                if(_clock.AtInterval())
                {
                    CheckAllPlugins();
                    StartCoroutine(DeferBuildLivePluginsList());
                }
            }
            catch(Exception e)
            {
                logBuilder.Exception(e);
                enabled = false;
            }
        }

        void CheckAllPlugins()
        {
            for(int i = _livePlugins.Count - 1; i >= 0; i--)
            {
                var livePlugin = _livePlugins[i];
                if(livePlugin.waitingForUIOpened)
                {
                    livePlugin.TryFindReloadButton();
                    livePlugin.FindFiles();
                }
                else if(!livePlugin.Present())
                {
                    livePlugin.RemoveFromUI();
                    _livePlugins.RemoveAt(i);
                }
                else if(livePlugin.monitorJsb.val)
                {
                    livePlugin.CheckDiff();
                }
            }
        }

        IEnumerator DeferBuildLivePluginsList()
        {
            yield return new WaitForSecondsRealtime(0.67f);
            BuildLivePluginsList();
        }
    }
}
