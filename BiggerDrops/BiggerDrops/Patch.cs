using BattleTech;
using BattleTech.Save;
using BattleTech.Save.SaveGameStructure;
using BattleTech.UI;
using Harmony;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using SVGImporter;

namespace DropManagement
{

    [HarmonyPatch(typeof(GameInstanceSave), MethodType.Constructor)]
    [HarmonyPatch(new Type[] { typeof(GameInstance), typeof(SaveReason) })]
    public static class GameInstanceSave_Constructor_Patch
    {
        static void Postfix(GameInstanceSave __instance)
        {
            Helper.SaveState(__instance.InstanceGUID, __instance.SaveTime);
        }
    }

    [HarmonyPatch(typeof(GameInstance), "Load")]
    public static class GameInstance_Load_Patch
    {
        static void Prefix(GameInstanceSave save)
        {
            Helper.LoadState(save.InstanceGUID, save.SaveTime);
        }
    }
    [HarmonyPatch(typeof(SGCmdCenterLanceConfigBG), "OnAddedToHierarchy")]
    public static class SGCmdCenterLanceConfigBG_OnAddedToHierarchy
    {
        static void Postfix(SGCmdCenterLanceConfigBG __instance)
        {
            DropManagement.baysAlreadyAdded = 0;
            __instance.LC.UpdateSlotsCount(Settings.MAX_ADDITINAL_MECH_SLOTS + DropManagement.settings.additinalMechSlots);
        }
    }
    [HarmonyPatch(typeof(LancePreviewPanel), "SaveLance")]
    public static class LancePreviewPanel_SaveLance
    {
        static void Prefix(LancePreviewPanel __instance, LanceDef lanceToSave)
        {
            Logger.M.TWL(0, "LancePreviewPanel.SaveLance", true);
            Logger.M.WL(0, lanceToSave.ToJSON(), true);
        }
    }
    [HarmonyPatch(typeof(LancePreviewPanel), "SetData")]
    public static class LancePreviewPanel_SetData
    {
        static void Prefix(LancePreviewPanel __instance, ref int maxUnits)
        {
            try
            {
                maxUnits = Settings.MAX_ADDITINAL_MECH_SLOTS + Settings.MAX_ADDITINAL_MECH_SLOTS;
                if (__instance.loadoutSlots.Length >= maxUnits) { return; }
                if (__instance.loadoutSlots.Length < 2) { maxUnits = __instance.loadoutSlots.Length; return; };
                float ydelta = __instance.loadoutSlots[1].GetComponent<RectTransform>().localPosition.y - __instance.loadoutSlots[0].GetComponent<RectTransform>().localPosition.y;
                int addUnits = maxUnits - __instance.loadoutSlots.Length;
                GameObject srcLayout = __instance.loadoutSlots[__instance.loadoutSlots.Length - 1].gameObject;
                List<LanceLoadoutSlot> slots = new List<LanceLoadoutSlot>();
                slots.AddRange(__instance.loadoutSlots);
                for (int t = 0; t < addUnits; ++t)
                {
                    GameObject nLayout = GameObject.Instantiate(srcLayout, srcLayout.transform.parent);
                    RectTransform rt = nLayout.GetComponent<RectTransform>();
                    Vector3 pos = rt.localPosition;
                    pos.y = srcLayout.GetComponent<RectTransform>().localPosition.y + (t + 1) * ydelta;
                    rt.localPosition = pos;
                    slots.Add(nLayout.GetComponent<LanceLoadoutSlot>());
                }
                __instance.loadoutSlots = slots.ToArray();
            }
            catch (Exception e)
            {
                Logger.M.TWL(0, e.ToString());
            }
        }
    }
    [HarmonyPatch(typeof(SkirmishMechBayPanel), "SelectLance")]
    public static class SkirmishMechBayPanel_SelectLance
    {
        static void Prefix(SkirmishMechBayPanel __instance, LanceDef lance)
        {
            try
            {
                int maxUnits = Settings.MAX_ADDITINAL_MECH_SLOTS + Settings.MAX_ADDITINAL_MECH_SLOTS;
                if (lance != null)
                {
                    maxUnits = lance.LanceUnits.Length;
                }
                if (__instance.loadoutSlots.Length >= maxUnits) { return; }
                if (__instance.loadoutSlots.Length < 2) { maxUnits = __instance.loadoutSlots.Length; return; };
                float ydelta = __instance.loadoutSlots[1].GetComponent<RectTransform>().localPosition.y - __instance.loadoutSlots[0].GetComponent<RectTransform>().localPosition.y;
                int addUnits = maxUnits - __instance.loadoutSlots.Length;
                GameObject srcLayout = __instance.loadoutSlots[__instance.loadoutSlots.Length - 1].gameObject;
                List<LanceLoadoutSlot> slots = new List<LanceLoadoutSlot>();
                slots.AddRange(__instance.loadoutSlots);
                for (int t = 0; t < addUnits; ++t)
                {
                    GameObject nLayout = GameObject.Instantiate(srcLayout, srcLayout.transform.parent);
                    RectTransform rt = nLayout.GetComponent<RectTransform>();
                    Vector3 pos = rt.localPosition;
                    pos.y = srcLayout.GetComponent<RectTransform>().localPosition.y + (t + 1) * ydelta;
                    rt.localPosition = pos;
                    slots.Add(nLayout.GetComponent<LanceLoadoutSlot>());
                }
                __instance.loadoutSlots = slots.ToArray();
            }
            catch (Exception e)
            {
                Logger.M.TWL(0, e.ToString());
            }
        }
    }

    [HarmonyPatch(typeof(LanceConfiguratorPanel), "SetData")]
    public static class LanceConfiguratorPanel_SetData
    {
        public static void UpdateSlotsCount(this LanceConfiguratorPanel panel, int maxUnits)
        {
            Logger.M.TWL(0, "LanceConfiguratorPanel.UpdateSlotsCount " + maxUnits);
            try
            {
                Logger.M.TWL(0, "maxUnits: " + maxUnits);
                LanceLoadoutSlot[] loadoutSlots = (LanceLoadoutSlot[])AccessTools.Field(typeof(LanceConfiguratorPanel), "loadoutSlots").GetValue(panel);
                
                if (maxUnits <= loadoutSlots.Length)
                {
                    Logger.M.TWL(1, "already fixed");
                    return;
                };
                GameObject primelayout = panel.transform.FindRecursive("uixPrfPanel_LC_LanceSlots-Widget-MANAGED").gameObject;
                
                //Setup Lance Count
                List<LanceLoadoutSlot> list = loadoutSlots.ToList();
                //Remove already present lance slots
                int addUnits = maxUnits - Settings.DEFAULT_MECH_SLOTS;
                Logger.M.TWL(0, "addUnits: " + addUnits);

                for (int i = 0; i < DropManagement.baysAlreadyAdded; i++)
                {
                    var slotNum = Settings.DEFAULT_MECH_SLOTS + i;
                    if (list[slotNum])
                    {
                        list.RemoveAt(slotNum);
                    }
                    else
                    {
                        Logger.M.TWL(0, "Error: index not found in UpdateSlotsCount: " + i.ToString());
                    }

                }

                //Create a copy of the lanceslots control
                var betaLayout = createNewLancePanel(primelayout, list, addUnits, "BetaLance");
                TextMeshProUGUI aiText = betaLayout.transform.FindRecursive("label-readyLanceHeading").gameObject.GetComponent<TextMeshProUGUI>();
                aiText.text = DropManagement.settings.secondLanceName;

                if (addUnits > 4)
                {
                    var charlieLayout = createNewLancePanel(primelayout, list, addUnits, "CharlieLance"); //TODO: add and handle the slots from Charlie lance
                    aiText = charlieLayout.transform.FindRecursive("label-readyLanceHeading").gameObject.GetComponent<TextMeshProUGUI>();
                    aiText.text = DropManagement.settings.thirdLanceName;
                    charlieLayout.transform.position = new Vector3(650, 55, primelayout.transform.position.z);
                    charlieLayout.transform.localScale = new Vector3(0.7f, 0.5f, 0.5f);
                }
                loadoutSlots = list.ToArray<LanceLoadoutSlot>();

                //shrink both slots to 70% and reposition
                primelayout.transform.position = new Vector3(650, 395, primelayout.transform.position.z);
                primelayout.transform.localScale = new Vector3(0.7f, 0.5f, 0.5f);
                betaLayout.transform.position = new Vector3(650, 200, primelayout.transform.position.z);
                betaLayout.transform.localScale = new Vector3(0.7f, 0.5f, 0.5f);

                GameObject deployButton = panel.transform.FindRecursive("DeployBttn-layout").gameObject;
                deployButton.transform.position = new Vector3(1675, 175, deployButton.transform.position.z);

                AccessTools.Field(typeof(LanceConfiguratorPanel), "loadoutSlots").SetValue(panel, loadoutSlots);

                float[] slotMaxTonnages = (float[])AccessTools.Field(typeof(LanceConfiguratorPanel), "slotMaxTonnages").GetValue(panel);
                float[] slotMinTonnages = (float[])AccessTools.Field(typeof(LanceConfiguratorPanel), "slotMinTonnages").GetValue(panel);
                List<float> listMaxTonnages = slotMaxTonnages.ToList();
                List<float> listMinTonnages = slotMinTonnages.ToList();

                //Logger.M.TWL(0, string.Format("Debug BiigerDrops.baysAlreadAdded: {0}", DropManagement.baysAlreadyAdded), true);
                //Logger.M.TWL(0, string.Format("Debug Settings.DEFAULT_MECH_SLOTS: {0}", Settings.DEFAULT_MECH_SLOTS), true);

                for (int i = 0; i < DropManagement.baysAlreadyAdded; i++)
                {
                    listMaxTonnages.RemoveAt(Settings.DEFAULT_MECH_SLOTS + i);
                    listMinTonnages.RemoveAt(Settings.DEFAULT_MECH_SLOTS + i);
                }
                if (addUnits > 0) { listMaxTonnages.Add(-1); }
                if (addUnits > 1) { listMaxTonnages.Add(-1); }
                if (addUnits > 2) { listMaxTonnages.Add(-1); }
                if (addUnits > 3) { listMaxTonnages.Add(-1); }
                if (addUnits > 4) { listMaxTonnages.Add(-1); }
                if (addUnits > 5) { listMaxTonnages.Add(-1); }
                if (addUnits > 6) { listMaxTonnages.Add(-1); }
                if (addUnits > 7) { listMaxTonnages.Add(-1); }
                if (addUnits > 0) { listMinTonnages.Add(-1); }
                if (addUnits > 1) { listMinTonnages.Add(-1); }
                if (addUnits > 2) { listMinTonnages.Add(-1); }
                if (addUnits > 3) { listMinTonnages.Add(-1); }
                if (addUnits > 4) { listMinTonnages.Add(-1); }
                if (addUnits > 5) { listMinTonnages.Add(-1); }
                if (addUnits > 6) { listMinTonnages.Add(-1); }
                if (addUnits > 7) { listMinTonnages.Add(-1); }
                slotMaxTonnages = listMaxTonnages.ToArray<float>();
                slotMinTonnages = listMinTonnages.ToArray<float>();
                AccessTools.Field(typeof(LanceConfiguratorPanel), "slotMaxTonnages").SetValue(panel, slotMaxTonnages);
                AccessTools.Field(typeof(LanceConfiguratorPanel), "slotMinTonnages").SetValue(panel, slotMinTonnages);
                DropManagement.baysAlreadyAdded = addUnits;
                Logger.M.TWL(0, "Skirmish UI fixed");
            }
            catch (Exception e)
            {
                Logger.M.TWL(0, e.ToString());
            }
        }

        private static GameObject createNewLancePanel(GameObject primelayout, List<LanceLoadoutSlot> list, int addUnits, string lanceName)
        {
            GameObject newLayout = GameObject.Instantiate(primelayout);
            newLayout.transform.SetParent(primelayout.transform.parent);
            newLayout.name = lanceName;
            GameObject slot1 = newLayout.transform.FindRecursive("lanceSlot1").gameObject;
            GameObject slot2 = newLayout.transform.FindRecursive("lanceSlot2").gameObject;
            GameObject slot3 = newLayout.transform.FindRecursive("lanceSlot3").gameObject;
            GameObject slot4 = newLayout.transform.FindRecursive("lanceSlot4").gameObject;
            primelayout.transform.FindRecursive("simbg").gameObject.SetActive(false);
            newLayout.transform.FindRecursive("simbg").gameObject.SetActive(false);
            newLayout.transform.FindRecursive("layout-lanceRating").gameObject.SetActive(false);
            newLayout.transform.FindRecursive("lanceSlotHeader-Campaign").gameObject.SetActive(true);
            newLayout.transform.FindRecursive("txt-unreadyLanceError").gameObject.SetActive(false);

            list.Add(slot1.GetComponent<LanceLoadoutSlot>()); 
            list.Add(slot2.GetComponent<LanceLoadoutSlot>()); 
            list.Add(slot3.GetComponent<LanceLoadoutSlot>()); 
            list.Add(slot4.GetComponent<LanceLoadoutSlot>());

            return newLayout;
        }

        static void Prefix(LanceConfiguratorPanel __instance, ref int maxUnits, Contract contract)
        {
            try
            {
                if (contract != null)
                {
                    maxUnits = Settings.MAX_ADDITINAL_MECH_SLOTS + DropManagement.settings.additinalMechSlots;
                    __instance.UpdateSlotsCount(maxUnits);
                    if (contract.Override != null)
                    {
                        if (contract.Override.maxNumberOfPlayerUnits < 4)
                        {
                            maxUnits = contract.Override.maxNumberOfPlayerUnits;
                        }
                    }
                }
                else
                {
                    maxUnits = Settings.MAX_ADDITINAL_MECH_SLOTS + Settings.MAX_ADDITINAL_MECH_SLOTS;
                    DropManagement.baysAlreadyAdded = 0;
                    __instance.UpdateSlotsCount(maxUnits);
                    //SkirmishUIFix(__instance,maxUnits);
                }
            }
            catch (Exception e)
            {
                Logger.M.TWL(0, e.ToString());
            }
        }

        static void Postfix(LanceConfiguratorPanel __instance, ref int maxUnits, Contract contract)
        {
            Logger.M.TWL(0, "LanceConfiguratorPanel.SetData postfix " + maxUnits);
            try
            {
                if (contract == null)
                {
                    Logger.M.WL(1, "contract is null");
                    //__instance.lanceMaxTonnage = DropManagement.settings.defaultMaxTonnage;
                    return;
                }
                else
                if (contract.Override.lanceMaxTonnage == -1)
                {
                    __instance.lanceMaxTonnage = DropManagement.settings.defaultMaxTonnage;
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }
    }
    [HarmonyPatch(typeof(LanceConfiguratorPanel), "CreateLanceDef")]
    public static class LanceConfiguratorPanel_CreateLanceDef
    {
        static void Prefix(LanceConfiguratorPanel __instance, string lanceId)
        {
            LanceLoadoutSlot[] loadoutSlots = (LanceLoadoutSlot[])AccessTools.Field(typeof(LanceConfiguratorPanel), "loadoutSlots").GetValue(__instance);
            Logger.M.TWL(0, "LanceConfiguratorPanel.CreateLanceDef " + lanceId + " slots:" + loadoutSlots.Length, true);
        }
        static void Postfix(LanceConfiguratorPanel __instance, string lanceId, ref LanceDef __result)
        {
            LanceLoadoutSlot[] loadoutSlots = (LanceLoadoutSlot[])AccessTools.Field(typeof(LanceConfiguratorPanel), "loadoutSlots").GetValue(__instance);
            Logger.M.TWL(0, "LanceConfiguratorPanel.CreateLanceDef result:", true);
            Logger.M.WL(0, __result.ToJSON());
        }
    }
    [HarmonyPatch(typeof(LancePreviewPanel), "OnLanceConfiguratorConfirm")]
    public static class LancePreviewPanel_OnLanceConfiguratorConfirm
    {
        static void Prefix(LancePreviewPanel __instance)
        {
            Logger.M.TWL(0, "LancePreviewPanel.OnLanceConfiguratorConfirm", true);
        }
    }
    [HarmonyPatch(typeof(LancePreviewPanel), "OnLanceConfiguratorCancel")]
    public static class LancePreviewPanel_OnLanceConfiguratorCancel
    {
        static void Prefix(LancePreviewPanel __instance)
        {
            Logger.M.TWL(0, "LancePreviewPanel.OnLanceConfiguratorCancel", true);
        }
    }

    [HarmonyPatch(typeof(Contract), "CompleteContract")]
    public static class Contract_CompleteContract
    {
        static void Prefix(Contract __instance)
        {
            try
            {
                CombatGameState combat = __instance.BattleTechGame.Combat;
                List<Mech> allMechs = combat.AllMechs;
                foreach (Mech mech in allMechs)
                {
                    if (Fields.callsigns.Contains(mech.pilot.Callsign))
                    {
                        AccessTools.Field(typeof(Mech), "_teamId").SetValue(mech, combat.LocalPlayerTeam.GUID);
                    }
                }

            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }
    }

    [HarmonyPatch(typeof(AAR_UnitsResult_Screen), "InitializeData")]
    public static class AAR_UnitsResult_Screen_InitializeData
    {
        static bool Prefix(AAR_UnitsResult_Screen __instance, MissionResults mission, SimGameState sim, Contract contract)
        {
            try
            {
                List<AAR_UnitStatusWidget> UnitWidgets = (List<AAR_UnitStatusWidget>)AccessTools.Field(typeof(AAR_UnitsResult_Screen), "UnitWidgets").GetValue(__instance);
                GameObject nextButton = __instance.transform.FindRecursive("buttonPanel").gameObject;
                nextButton.transform.localPosition = new Vector3(150, 400, 0);

                Transform parent = UnitWidgets[0].transform.parent;
                parent.localPosition = new Vector3(0, 115, 0);
                foreach (AAR_UnitStatusWidget oldWidget in UnitWidgets)
                {
                    oldWidget.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
                }

                GameObject newparent = GameObject.Instantiate(parent.gameObject);
                newparent.transform.parent = parent.parent;
                newparent.name = "newparent";
                newparent.transform.localPosition = new Vector3(0, -325, 0);

                GameObject thirdparent = GameObject.Instantiate(parent.gameObject);
                thirdparent.transform.parent = parent.parent;
                thirdparent.name = "thirdparent";
                thirdparent.transform.localPosition = new Vector3(0, -325, 0);

                foreach (Transform t in newparent.transform)
                {
                    UnitWidgets.Add(t.gameObject.GetComponent<AAR_UnitStatusWidget>());
                }

                foreach (Transform t in thirdparent.transform)
                {
                    UnitWidgets.Add(t.gameObject.GetComponent<AAR_UnitStatusWidget>());
                }
                AccessTools.Field(typeof(AAR_UnitsResult_Screen), "UnitWidgets").SetValue(__instance, UnitWidgets);

                List<UnitResult> UnitResults = new List<UnitResult>();
                for (int i = 0; i < 12; i++)
                {
                    if (i < contract.PlayerUnitResults.Count)
                    {
                        UnitResults.Add(contract.PlayerUnitResults[i]);
                    }
                    //else
                    //{
                    //    UnitResults.Add(null);
                    //}
                }
                Logger.M.TWL(0, sim.ToString());
                Logger.M.TWL(0, sim.CompanyName);
                Logger.M.TWL(0, mission.ToString());
                Logger.M.TWL(0, mission.name);
                Logger.M.TWL(0, contract.ToString());
                Logger.M.TWL(0, contract.Name);
                contract.PlayerUnitResults.ForEach(u=> Logger.M.TWL(0, u.mech.Name));
                Logger.M.TWL(0, contract.PlayerUnitResults.Count.ToString());
                UnitResults.ForEach(u=> Logger.M.TWL(0, u.mech.Name));
                Logger.M.TWL(0, UnitResults.Count.ToString());

                AccessTools.Field(typeof(AAR_UnitsResult_Screen), "simState").SetValue(__instance, sim);
                AccessTools.Field(typeof(AAR_UnitsResult_Screen), "missionResultParent").SetValue(__instance, mission);
                AccessTools.Field(typeof(AAR_UnitsResult_Screen), "theContract").SetValue(__instance, contract);
                AccessTools.Field(typeof(AAR_UnitsResult_Screen), "numUnits").SetValue(__instance, contract.PlayerUnitResults.Count);
                AccessTools.Field(typeof(AAR_UnitsResult_Screen), "UnitResults").SetValue(__instance, UnitResults);
                __instance.Visible = false;
                __instance.InitializeWidgets();
                return false;
            }
            catch (Exception e)
            {
                Logger.LogError(e);
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(AAR_UnitsResult_Screen), "FillInData")]
    public static class AAR_UnitsResult_Screen_FillInData
    {
        static bool Prefix(AAR_UnitsResult_Screen __instance)
        {
            try
            {
                Contract theContract = (Contract)AccessTools.Field(typeof(AAR_UnitsResult_Screen), "theContract").GetValue(__instance);
                List<AAR_UnitStatusWidget> UnitWidgets = (List<AAR_UnitStatusWidget>)AccessTools.Field(typeof(AAR_UnitsResult_Screen), "UnitWidgets").GetValue(__instance);
                List<UnitResult> UnitResults = (List<UnitResult>)AccessTools.Field(typeof(AAR_UnitsResult_Screen), "UnitResults").GetValue(__instance);
                int experienceEarned = theContract.ExperienceEarned;
                for (int i = 0; i < 8; i++)
                {
                    UnitWidgets[i].SetMechIconValueTextActive(false);
                    if (UnitResults[i] != null)
                    {
                        UnitWidgets[i].SetNoUnitDeployedOverlayActive(false);
                        UnitWidgets[i].FillInData(experienceEarned);
                    }
                    else
                    {
                        UnitWidgets[i].SetNoUnitDeployedOverlayActive(true);
                    }
                }
                AccessTools.Field(typeof(AAR_UnitsResult_Screen), "UnitWidgets").SetValue(__instance, UnitWidgets);
                return false;
            }
            catch (Exception e)
            {
                Logger.LogError(e);
                return true;
            }
        }
    }


    [HarmonyPatch(typeof(LanceConfiguratorPanel), "CreateLanceConfiguration")]
    public static class LanceConfiguratorPanel_CreateLanceConfiguration
    {
        static bool Prefix(LanceConfiguratorPanel __instance, ref LanceConfiguration __result)
        {
            try
            {
                return false;
            }
            catch (Exception e)
            {
                Logger.LogError(e);
                return false;
            }
        }

        static void Postfix(LanceConfiguratorPanel __instance, ref LanceConfiguration __result, LanceLoadoutSlot[] ___loadoutSlots)
        {
            try
            {
                Fields.callsigns.Clear();
                LanceConfiguration lanceConfiguration = new LanceConfiguration();
                for (int i = 0; i < ___loadoutSlots.Length; i++)
                {
                    LanceLoadoutSlot lanceLoadoutSlot = ___loadoutSlots[i];
                    MechDef mechDef = null;
                    PilotDef pilotDef = null;
                    if (lanceLoadoutSlot.SelectedMech != null)
                    {
                        mechDef = lanceLoadoutSlot.SelectedMech.MechDef;
                    }
                    if (lanceLoadoutSlot.SelectedPilot != null)
                    {
                        pilotDef = lanceLoadoutSlot.SelectedPilot.Pilot.pilotDef;
                    }
                    if (mechDef != null && pilotDef != null)
                    {
                        if (i < Settings.DEFAULT_MECH_SLOTS)
                        {
                            lanceConfiguration.AddUnit(__instance.playerGUID, mechDef, pilotDef);
                        }
                        else
                        {
                            //if (i >= DropManagement.settings.additinalMechSlots + Settings.DEFAULT_MECH_SLOTS) { break; }
                            Logger.M.TWL(0, "LanceConfiguratorPanel.CreateLanceConfiguration. Index:" + i + " additional slots border:" + (DropManagement.settings.additinalMechSlots + Settings.DEFAULT_MECH_SLOTS) + " player slots border:" + (DropManagement.settings.additinalPlayerMechSlots + Settings.DEFAULT_MECH_SLOTS));
                            if (i >= 12) //mechs beyond the 12 slots are contracted mechs
                            {
                                Fields.callsigns.Add(pilotDef.Description.Callsign);
                                //EMPLOYER ID
                                Logger.M.WL(1, "adding to employer lance " + Settings.EMPLOYER_LANCE_GUID + " mech:" + mechDef.Description.Id + " pilot:" + pilotDef.Description.Id);
                                lanceConfiguration.AddUnit(Settings.EMPLOYER_LANCE_GUID, mechDef, pilotDef);
                            }
                            else
                            {
                                Logger.M.WL(1, "adding to player lance " + __instance.playerGUID + " mech:" + mechDef.Description.Id + " pilot:" + pilotDef.Description.Id);
                                lanceConfiguration.AddUnit(__instance.playerGUID, mechDef, pilotDef);
                            }
                        }
                    }
                }
                __result = lanceConfiguration;
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }
    }

    [HarmonyPatch(typeof(SimGameState), "Rehydrate", typeof(GameInstanceSave))]
    class SimGameState_RehydratePatch
    {
        public static void Postfix(SimGameState __instance, GameInstanceSave gameInstanceSave)
        {
            if (DropManagement.settings.allowUpgrades)
            {
                DropManagement.settings.setCompanyStats(__instance.CompanyStats);
            }
        }
    }

    [HarmonyPatch(typeof(SimGameState), "InitCompanyStats")]
    class SimGameState_InitCompanyStatsPatch
    {
        public static void Postfix(SimGameState __instance)
        {
            if (DropManagement.settings.allowUpgrades)
            {
                DropManagement.settings.setCompanyStats(__instance.CompanyStats);
            }
        }
    }

    [HarmonyPatch(typeof(SGEngineeringScreen), "PopulateUpgradeDictionary")]
    public static class SGEngineeringScreen_PopulateUpgradeDictionary
    {
        public static void Prefix(SGEngineeringScreen __instance)
        {
            
                return;
        }
    }

    [HarmonyPatch(typeof(SGEngineeringScreen), "CleanUpAllUpgradePips")]
    public static class SGEngineeringScreen_CleanUpAllUpgradePips
    {
        public static void Postfix(SGEngineeringScreen __instance)
        {
            try
            {
                if (__instance.transform.FindRecursive("BDUpgradePanel") != null)
                {
                    GameObject primelayout = __instance.transform.FindRecursive("BDUpgradePanel").gameObject;
                    List<SGEngineeringShipUpgradePip> engineeringShipUpgradePipList = new List<SGEngineeringShipUpgradePip>();
                    GameObject driverPipSlots = primelayout.transform.FindRecursive("BDDropTonnage").gameObject;
                    GameObject structurePipSlots = primelayout.transform.FindRecursive("BDMechControl").gameObject;
                    GameObject powerPipSlots = primelayout.transform.FindRecursive("BDMechDrops").gameObject;
                    engineeringShipUpgradePipList.AddRange((IEnumerable<SGEngineeringShipUpgradePip>)driverPipSlots.GetComponentsInChildren<SGEngineeringShipUpgradePip>());
                    engineeringShipUpgradePipList.AddRange((IEnumerable<SGEngineeringShipUpgradePip>)structurePipSlots.GetComponentsInChildren<SGEngineeringShipUpgradePip>());
                    engineeringShipUpgradePipList.AddRange((IEnumerable<SGEngineeringShipUpgradePip>)powerPipSlots.GetComponentsInChildren<SGEngineeringShipUpgradePip>());
                    List<ShipModuleUpgrade> available = (List<ShipModuleUpgrade>)AccessTools.Field(typeof(SGEngineeringScreen), "AvailableUpgrades").GetValue(__instance);
                    List<ShipModuleUpgrade> purchased = (List<ShipModuleUpgrade>)AccessTools.Field(typeof(SGEngineeringScreen), "PurchasedUpgrades").GetValue(__instance);
                    UIManager uiManager = (UIManager)AccessTools.Field(typeof(SGEngineeringScreen), "uiManager").GetValue(__instance);
                    foreach (SGEngineeringShipUpgradePip engineeringShipUpgradePip in engineeringShipUpgradePipList)
                    {
                        string id = "uixPrfIndc_SIM_argoUpgradePipUnavailable-element";

                        if (available.Contains(engineeringShipUpgradePip.UpgradeModule))
                            id = "uixPrfIndc_SIM_argoUpgradePipAvailable-element";
                        else if (purchased.Contains(engineeringShipUpgradePip.UpgradeModule))
                            id = "uixPrfIndc_SIM_argoUpgradePip-element";
                        uiManager.dataManager.PoolGameObject(id, engineeringShipUpgradePip.gameObject);
                    }

                }
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }
    }

    [HarmonyPatch(typeof(SGEngineeringScreen), "ClearUpgradePips")]
    public static class SGEngineeringScreen_ClearUpgradePips
    {
        public static void Postfix(SGEngineeringScreen __instance)
        {
            try
            {
                if (__instance.transform.FindRecursive("BDUpgradePanel") != null)
                {
                    GameObject primelayout = __instance.transform.FindRecursive("BDUpgradePanel").gameObject;
                    List<GameObject> engineeringShipUpgradePipList = new List<GameObject>();
                    GameObject driverPipSlots = primelayout.transform.FindRecursive("BDDropTonnage").gameObject;
                    foreach (Transform transform in driverPipSlots.transform)
                    {
                        if ((UnityEngine.Object)transform.gameObject.GetComponent<SGEngineeringShipUpgradePip>() != (UnityEngine.Object)null)
                            engineeringShipUpgradePipList.Add(transform.gameObject);
                    }
                    GameObject structurePipSlots = primelayout.transform.FindRecursive("BDMechControl").gameObject;
                    foreach (Transform transform in structurePipSlots.transform)
                    {
                        if ((UnityEngine.Object)transform.gameObject.GetComponent<SGEngineeringShipUpgradePip>() != (UnityEngine.Object)null)
                            engineeringShipUpgradePipList.Add(transform.gameObject);
                    }
                    GameObject powerPipSlots = primelayout.transform.FindRecursive("BDMechDrops").gameObject;
                    foreach (Transform transform in powerPipSlots.transform)
                    {
                        if ((UnityEngine.Object)transform.gameObject.GetComponent<SGEngineeringShipUpgradePip>() != (UnityEngine.Object)null)
                            engineeringShipUpgradePipList.Add(transform.gameObject);
                    }
                    List<ShipModuleUpgrade> available = (List<ShipModuleUpgrade>)AccessTools.Field(typeof(SGEngineeringScreen), "AvailableUpgrades").GetValue(__instance);
                    List<ShipModuleUpgrade> purchased = (List<ShipModuleUpgrade>)AccessTools.Field(typeof(SGEngineeringScreen), "PurchasedUpgrades").GetValue(__instance);
                    SimGameState simGame = (SimGameState)AccessTools.Property(typeof(SGEngineeringScreen), "simState").GetValue(__instance);
                    engineeringShipUpgradePipList.ForEach((Action<GameObject>)(item =>
                    {
                        string id = "uixPrfIndc_SIM_argoUpgradePipUnavailable-element";
                        ShipModuleUpgrade upgradeModule = item.GetComponent<SGEngineeringShipUpgradePip>().UpgradeModule;
                        if (available.Contains(upgradeModule))
                            id = "uixPrfIndc_SIM_argoUpgradePipAvailable-element";
                        else if (purchased.Contains(upgradeModule))
                            id = "uixPrfIndc_SIM_argoUpgradePip-element";
                        simGame.DataManager.PoolGameObject(id, item);
                    }));
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }
    }

    [HarmonyPatch(typeof(SGEngineeringScreen), "AddUpgradePip")]
    public static class SGEngineeringScreen_AddUpgradePip
    {
        public static void UpgradeSelected(this SGEngineeringScreen screen, ShipModuleUpgrade upgrade)
        {
            AccessTools.Method(typeof(SGEngineeringScreen), "OnUpgradeSelected").Invoke(screen, new object[] { upgrade });
        }
        public static bool Prefix(SGEngineeringScreen __instance, ShipModuleUpgrade upgrade)
        {
                return true;
        }
    }

    /* [HarmonyPatch(typeof(PlayerLanceSpawnerGameLogic), "ContractInitialize")]
        public static class PlayerLanceSpawnerGameLogic_ContractInitialize {
            static void Prefix(PlayerLanceSpawnerGameLogic __instance) {
                try {
                    UnitSpawnPointGameLogic[] unitSpawnPointGameLogicList = __instance.unitSpawnPointGameLogicList;
                    SpawnableUnit[] lanceUnits = __instance.Combat.ActiveContract.Lances.GetLanceUnits("ecc8d4f2-74b4-465d-adf6-84445e5dfc230");
                    int num = 1;
                    while (num < lanceUnits.Length && num < unitSpawnPointGameLogicList.Length) {
                        unitSpawnPointGameLogicList[num].OverrideSpawn(lanceUnits[num]);
                        num++;
                    }
                }
                catch (Exception e) {
                    Logger.LogError(e);
                }
            }
        }*/
}