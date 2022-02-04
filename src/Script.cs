using System;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;
using static LiveReload.Utils;

namespace LiveReload
{
    internal class Script : MVRScript
    {
        public static readonly Version version = new Version("0.0.0");

        private float _time = 0;
        private JSONStorableString pluginVersionStorable;
        private JSONStorableFloat _checkInterval;
        private List<LivePlugin> _livePlugins = new List<LivePlugin>();

        public static string mainDir;

        public override void Init()
        {
            try
            {
                enabled = false;

                var creatorName = UserPreferences.singleton.creatorName;
                if(creatorName == null || creatorName.Trim().Length == 0)
                {
                    LogError($"Set your creator name in user preferences and reload the plugin.");
                    return;
                }
                mainDir = $@"Custom\Scripts\{creatorName}";

                var title = this.NewTextField("Title", $"\n{nameof(LiveReload)} v{version}-alpha2", 36);
                title.dynamicText.backgroundColor = Color.clear;
                title.dynamicText.textColor = Color.white;
                _checkInterval = this.NewFloatSlider("Check interval (sec)", 1f, 0.5f, 2f, "F1", true);

                StartCoroutine(BuildUserPluginsList());
            }
            catch(Exception e)
            {
                LogError($"Init: {e}");
            }
        }

        private Dictionary<string, List<string>> FindPluginsInSceneJson(JSONArray atoms)
        {
            var result = new Dictionary<string, List<string>>();
            try
            {
                foreach(JSONClass atom in atoms)
                {
                    // skip other atoms if not added as scene/session plugin
                    if(containingAtom.name != "CoreControl" && containingAtom.uid != atom["id"].Value)
                    {
                        continue;
                    }
                    var storables = atom["storables"].AsArray;
                    foreach(JSONClass storable in storables)
                    {
                        if(storable["id"].Value != "PluginManager")
                        {
                            continue;
                        }

                        var pluginsObj = storable["plugins"].AsObject;
                        foreach(var key in pluginsObj.Keys)
                        {
                            var pluginFullPath = pluginsObj[key].Value;
                            //dev
                            if(pluginFullPath.Contains("LiveReload"))
                            {
                                continue;
                            }

                            if(pluginFullPath.StartsWith(mainDir.Replace(@"\", "/")))
                            {
                                if(result.ContainsKey(atom["id"]))
                                {
                                    result[atom["id"]].Add(pluginFullPath);
                                }
                                else
                                {
                                    result.Add(atom["id"].Value, new List<string> { { pluginFullPath } });
                                }
                            }
                        }
                    }
                }
                return result;
            }
            catch(Exception e)
            {
                LogError($"Failed reading scene JSON for plugin paths: {e}");
            }

            return null;
        }

        private IEnumerator BuildUserPluginsList()
        {
            yield return new WaitForEndOfFrame();

            var sceneJson = SuperController.singleton.GetSaveJSON().AsObject;
            var pluginsByAtomInJson = FindPluginsInSceneJson(sceneJson["atoms"].AsArray);

            if(pluginsByAtomInJson == null)
            {
                yield break;
            }

            foreach(var atomPlugins in pluginsByAtomInJson)
            {
                foreach(var pluginPath in atomPlugins.Value)
                {
                    var livePlugin = new LivePlugin(pluginPath, atomPlugins.Key);
                    BuildUISection(livePlugin);
                    if(livePlugin.monitoringOn.val)
                    {
                        livePlugin.TryFindPlugin();
                    }
                    _livePlugins.Add(livePlugin);
                }
            }

            enabled = true;
        }

        public void BuildUISection(LivePlugin livePlugin)
        {
            this.NewSpacer(10f);
            this.NewSpacer(10f, true);

            var pathStorable = this.NewTextField("Plugin", livePlugin.BuildHeader(containingAtom.name == "CoreControl"), 36, 50);
            pathStorable.dynamicText.textColor = Color.black;
            pathStorable.dynamicText.backgroundColor = Color.white;

            livePlugin.headerText = UI.NewInputField(pathStorable.dynamicText);
            livePlugin.headerText.interactable = false;

            livePlugin.monitoringOn = this.NewToggle("Monitoring on", true);
            livePlugin.logReloads = this.NewToggle("Output changes to Message Log", false);
            livePlugin.statusText = this.NewTextField("Status", "", 24, 255, true);

            this.NewSpacer(60f);
        }

        public void Update()
        {
            try
            {
                _time += Time.deltaTime;
                if(_time >= _checkInterval.val)
                {
                    _time -= _checkInterval.val;
                    CheckAllPlugins();
                }
            }
            catch(Exception e)
            {
                LogError($"Update: {e}");
            }
        }

        private void CheckAllPlugins()
        {
            foreach(var livePlugin in _livePlugins)
            {
                if(!livePlugin.monitoringOn.val)
                {
                    continue;
                }
                if(livePlugin.WaitingForUIOpened)
                {
                    livePlugin.TryFindPlugin();
                    continue;
                }

                livePlugin.CheckDiff();
            }
        }

        private void OnDestroy()
        {
            foreach(var livePlugin in _livePlugins)
            {
                Destroy(livePlugin.headerText);
            }
        }
    }
}
