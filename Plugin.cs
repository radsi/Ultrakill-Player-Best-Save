using BepInEx;
using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PlayerBestSave
{
    [BepInPlugin(GUID, Name, version)]
    public class Plugin : BaseUnityPlugin
    {
        private const string GUID = "radsi.ultrakill.playerbestsave";
        private const string Name = "PlayerBestSave";
        private const string version = "1.0.0.0";

        private Harmony harmony = new Harmony(GUID);

        string existingData;

        JObject pbdata;

        GameObject newPanel;

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
            using (StreamReader r = new StreamReader(Application.dataPath.Replace("ULTRAKILL_Data", "BepInEx/plugins/playerbests.json")))
            {
                existingData = r.ReadToEnd();
                r.Close();
            }

            if (SceneManager.GetActiveScene().name != "Main Menu")
            {
                if (existingData != "")
                {
                    pbdata = JObject.Parse(existingData);
                }
                else
                {
                    return;
                }

                StatsManager sm = GameObject.Find("StatsManager").GetComponent<StatsManager>();

                if (!GameObject.Find("Canvas/Level Stats Controller/Level Stats (1)(Clone)") && pbdata.ContainsKey(sm.levelNumber.ToString()))
                {
                    newPanel = Instantiate(GameObject.Find("Canvas/Level Stats Controller/Level Stats (1)"));
                    newPanel.transform.SetParent(GameObject.Find("Canvas/Level Stats Controller").transform);
                    newPanel.GetComponent<Image>().rectTransform.sizeDelta = new Vector2(285, 220);
                    newPanel.transform.localPosition = new Vector2(300, 0);
                    newPanel.transform.localScale = new Vector3(1f, 1f, 1f);

                    Destroy(newPanel.GetComponent<LevelStats>());
                    newPanel.transform.GetChild(7).gameObject.SetActive(false);
                    newPanel.transform.GetChild(6).gameObject.SetActive(false);
                    newPanel.transform.GetChild(5).gameObject.SetActive(false);

                    newPanel.transform.GetChild(0).GetComponent<Text>().text = "PLAYER BEST";
                }

                newPanel.SetActive(GameObject.Find("Canvas/Level Stats Controller/Level Stats (1)").activeSelf);

                newPanel.transform.GetChild(2).GetChild(0).GetComponent<Text>().text = (string)pbdata[sm.levelNumber.ToString()]["Time"];
                newPanel.transform.GetChild(2).GetChild(1).GetComponent<Text>().text = getRankWithColor((string)pbdata[sm.levelNumber.ToString()]["TimeRank"]);
                newPanel.transform.GetChild(3).GetChild(0).GetComponent<Text>().text = (string)pbdata[sm.levelNumber.ToString()]["Kills"];
                newPanel.transform.GetChild(3).GetChild(1).GetComponent<Text>().text = getRankWithColor((string)pbdata[sm.levelNumber.ToString()]["KillsRank"]);
                newPanel.transform.GetChild(4).GetChild(0).GetComponent<Text>().text = (string)pbdata[sm.levelNumber.ToString()]["Style"];
                newPanel.transform.GetChild(4).GetChild(1).GetComponent<Text>().text = getRankWithColor((string)pbdata[sm.levelNumber.ToString()]["StyleRank"]);
            }
        }

        static string getRankWithColor(string rank)
        {
            var fr = "";
            switch (rank)
            {
                case "C":
                    fr = "<color=#4CFF00>C</color>";
                    break;
                case "B":
                    fr = "<color=#FFD800>B</color>";
                    break;
                case "A":
                    fr = "<color=#FF6A00>A</color>";
                    break;
                case "S":
                    fr = "<color=#FF0000>S</color>";
                    break;
                case "D":
                    fr = "<color=#0094FF>D</color>";
                    break;
            }
            return fr;
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
                    new JProperty("RankPoints", sm.rankScore),
                    new JProperty("Time", sm.seconds),
                    new JProperty("Kills", sm.kills),
                    new JProperty("Style", sm.stylePoints),
                    new JProperty("TimeRank", sm.fr.timeRank.text),
                    new JProperty("KillsRank", sm.fr.killsRank.text),
                    new JProperty("StyleRank", sm.fr.styleRank.text)
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
                if ((int)jsonExistingData[sm.levelNumber.ToString()]["RankPoints"] < sm.rankScore || (int)jsonExistingData[sm.levelNumber.ToString()]["RankPoints"] == sm.rankScore && (int)jsonExistingData[sm.levelNumber.ToString()]["Time"] > sm.seconds)
                {
                    jsonExistingData[sm.levelNumber.ToString()]["RankPoints"] = sm.rankScore;
                    jsonExistingData[sm.levelNumber.ToString()]["Time"] = sm.seconds;
                    jsonExistingData[sm.levelNumber.ToString()]["Kills"] = sm.kills;
                    jsonExistingData[sm.levelNumber.ToString()]["Style"] = sm.stylePoints;
                    jsonExistingData[sm.levelNumber.ToString()]["TimeRank"] = sm.fr.timeRank.text;
                    jsonExistingData[sm.levelNumber.ToString()]["KillsRank"] = sm.fr.killsRank.text;
                    jsonExistingData[sm.levelNumber.ToString()]["StyleRank"] = sm.fr.styleRank.text;
                }

                fullData = jsonExistingData.ToString();
            }

            File.WriteAllText(file, fullData);

            JObject pbdata = JObject.Parse(fullData);
            GameObject newPanel = GameObject.Find("Canvas/Level Stats Controller/Level Stats (1)(Clone)");
            newPanel.transform.GetChild(2).GetChild(0).GetComponent<Text>().text = (string)pbdata[sm.levelNumber.ToString()]["Time"];
            newPanel.transform.GetChild(2).GetChild(1).GetComponent<Text>().text = getRankWithColor((string)pbdata[sm.levelNumber.ToString()]["TimeRank"]);
            newPanel.transform.GetChild(3).GetChild(0).GetComponent<Text>().text = (string)pbdata[sm.levelNumber.ToString()]["Kills"];
            newPanel.transform.GetChild(3).GetChild(1).GetComponent<Text>().text = getRankWithColor((string)pbdata[sm.levelNumber.ToString()]["KillsRank"]);
            newPanel.transform.GetChild(4).GetChild(0).GetComponent<Text>().text = (string)pbdata[sm.levelNumber.ToString()]["Style"];
            newPanel.transform.GetChild(4).GetChild(1).GetComponent<Text>().text = getRankWithColor((string)pbdata[sm.levelNumber.ToString()]["StyleRank"]);
        }

        static string getRankWithColor(string rank)
        {
            var fr = "";
            switch (rank)
            {
                case "C":
                    fr = "<color=#4CFF00>C</color>";
                    break;
                case "B":
                    fr = "<color=#FFD800>B</color>";
                    break;
                case "A":
                    fr = "<color=#FF6A00>A</color>";
                    break;
                case "S":
                    fr = "<color=#FF0000>S</color>";
                    break;
                case "D":
                    fr = "<color=#0094FF>D</color>";
                    break;
            }
            return fr;
        }
    }
}
