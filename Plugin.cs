using BepInEx;
using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PlayerBestSave
{
    [BepInPlugin(GUID, Name, version)]
    public class Plugin : BaseUnityPlugin
    {
        private const string GUID = "radsi.ultrakill.playerbestsave";
        private const string Name = "PlayerBestSave";
        private const string version = "1.0.0.0";

        private Harmony harmony = new Harmony(GUID);

        private void Awake()
        {
            Logger.LogMessage($"Plugin {Name} is loaded!");

            harmony.PatchAll();

            SceneManager.LoadScene(1);

            if(!File.Exists(Application.dataPath.Replace("ULTRAKILL_Data", "BepInEx/plugins/playerbests.json")))
            {
                File.CreateText(Application.dataPath.Replace("ULTRAKILL_Data", "BepInEx/plugins/playerbests.json"));
            }
        }

        void Update()
        {
            
        }
    }

    [HarmonyPatch(typeof(FinalPit), "OnTriggerEnter")]
    class PatchedClass
    {
        static void Postfix()
        {
            StatsManager sm = GameObject.Find("StatsManager").GetComponent<StatsManager>();

            JObject jsonExistingData = new();

            string fullData = "";
            string existingData = "";
            string file = Application.dataPath.Replace("ULTRAKILL_Data", "BepInEx/plugins/playerbests.json");

            using (StreamReader r = new StreamReader(file))
            {
                existingData = r.ReadToEnd();
                r.Close();
            }
             
            if(existingData != "")
            {
                jsonExistingData = JObject.Parse(existingData);
            }

            if (!jsonExistingData.ContainsKey(sm.levelNumber.ToString()))
            {
                JObject data = new JObject(
                    new JProperty("Time", sm.seconds),
                    new JProperty("Rank", sm.fr.totalRank.text),
                    new JProperty("Kills", sm.kills),
                    new JProperty("Style", sm.stylePoints)
                );

                JObject dataToWrite = new JObject(
                    new JProperty(sm.levelNumber.ToString(), data)
                );

                if (existingData.Length > 5)
                {  
                    fullData = "{" + existingData.TrimStart('{').TrimEnd('}') + ", " + dataToWrite.ToString().TrimStart('{').TrimEnd('}') + "}";
                }
                else
                {
                    fullData = dataToWrite.ToString();
                }
            }
            else
            {
                if ((int)jsonExistingData[sm.levelNumber.ToString()]["Time"] > sm.seconds)
                {
                    jsonExistingData[sm.levelNumber.ToString()]["Time"] = sm.seconds;
                    jsonExistingData[sm.levelNumber.ToString()]["Rank"] = sm.fr.totalRank.text;
                    jsonExistingData[sm.levelNumber.ToString()]["Kills"] = sm.kills;
                    jsonExistingData[sm.levelNumber.ToString()]["Style"] = sm.stylePoints;
                }

                fullData = jsonExistingData.ToString();
            }

            File.WriteAllText(file, fullData);
        }
    }
}
