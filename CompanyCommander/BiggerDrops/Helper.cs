using BattleTech;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace DropManagement
{
    public class SaveFields
    {
        public List<string> callsigns = new List<string>();

        public SaveFields(List<string> callsigns)
        {
            this.callsigns = callsigns;
        }
    }

    public class Helper
    {

        public static void SaveState(string instanceGUID, DateTime saveTime)
        {
            try
            {
                int unixTimestamp = (int)(saveTime.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;

                DirectoryInfo modsDir = Directory.GetParent(DropManagement.ModDirectory);
                DirectoryInfo battletechDir = modsDir.Parent;
                // We want to write to Battletech/ModSaves/PersistentMapClient directory
                DirectoryInfo modSavesDir = battletechDir.CreateSubdirectory("ModSaves");
                string filePath = battletechDir + "/ModSaves/DropManagement/" + instanceGUID + "-" + unixTimestamp + ".json";
                (new FileInfo(filePath)).Directory.Create();
                using (StreamWriter writer = new StreamWriter(filePath, true))
                {
                    SaveFields fields = new SaveFields(Fields.callsigns);
                    string json = JsonConvert.SerializeObject(fields);
                    writer.Write(json);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
        }

        public static void LoadState(string instanceGUID, DateTime saveTime)
        {
            try
            {
                int unixTimestamp = (int)(saveTime.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                DirectoryInfo modsDir = Directory.GetParent(DropManagement.ModDirectory);
                DirectoryInfo battletechDir = modsDir.Parent;
                string filePath = battletechDir + "/ModSaves/DropManagement/" + instanceGUID + "-" + unixTimestamp + ".json";
                if (File.Exists(filePath))
                {
                    using (StreamReader r = new StreamReader(filePath))
                    {
                        string json = r.ReadToEnd();
                        SaveFields save = JsonConvert.DeserializeObject<SaveFields>(json);
                        Fields.callsigns = save.callsigns;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
        }
    }
}