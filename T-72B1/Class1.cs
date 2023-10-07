using System;
using MelonLoader;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using GHPC.Weapons;
using UnityEngine;
using GHPC.Camera;
using GHPC.Player;
using GHPC.Vehicle;
using GHPC.Equipment;
using GHPC;

namespace T_72B1
{
    public class T72MOD : MelonMod
    {
        AmmoClipCodexScriptable clip_codex_3bm32;
        AmmoType.AmmoClip clip_3bm32;
        AmmoCodexScriptable ammo_codex_3bm32;
        AmmoType ammo_3bm32;
        AmmoType ammo_3bm15;

        ArmorType armor_textolite;

        ArmorCodexScriptable armor_codex_superTextolite;
        ArmorType armor_superTextolite;

        GameObject[] vic_gos;
        GameObject gameManager;
        CameraManager cameraManager;
        PlayerInput playerManager;

        // https://snipplr.com/view/75285/clone-from-one-object-to-another-using-reflection
        public static void ShallowCopy(System.Object dest, System.Object src)
        {
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            FieldInfo[] destFields = dest.GetType().GetFields(flags);
            FieldInfo[] srcFields = src.GetType().GetFields(flags);

            foreach (FieldInfo srcField in srcFields)
            {
                FieldInfo destField = destFields.FirstOrDefault(field => field.Name == srcField.Name);

                if (destField != null && !destField.IsLiteral)
                {
                    if (srcField.FieldType == destField.FieldType)
                        destField.SetValue(dest, srcField.GetValue(src));
                }
            }
        }
        public static void EmptyRack(GHPC.Weapons.AmmoRack rack)
        {
            MethodInfo removeVis = typeof(GHPC.Weapons.AmmoRack).GetMethod("RemoveAmmoVisualFromSlot", BindingFlags.Instance | BindingFlags.NonPublic);

            PropertyInfo stored_clips = typeof(GHPC.Weapons.AmmoRack).GetProperty("StoredClips");
            stored_clips.SetValue(rack, new List<AmmoType.AmmoClip>());

            rack.SlotIndicesByAmmoType = new Dictionary<AmmoType, List<byte>>();

            foreach (Transform transform in rack.VisualSlots)
            {
                AmmoStoredVisual vis = transform.GetComponentInChildren<AmmoStoredVisual>();

                if (vis != null && vis.AmmoType != null)
                {
                    removeVis.Invoke(rack, new object[] { transform });
                }
            }
        }
        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            if (sceneName == "LOADER_INITIAL" || sceneName == "MainMenu2_Scene") return;

            vic_gos = GameObject.FindGameObjectsWithTag("Vehicle");

            if (ammo_3bm32 == null)
            {

                foreach (AmmoCodexScriptable s in Resources.FindObjectsOfTypeAll(typeof(AmmoCodexScriptable)))
                {
                    if (s.AmmoType.Name == "3BM15 APFSDS-T")
                    {
                        ammo_3bm15 = s.AmmoType;
                    }
                }

                foreach (ArmorCodexScriptable s in Resources.FindObjectsOfTypeAll(typeof(ArmorCodexScriptable)))
                {
                    if (s.ArmorType.Name == "glass textolite")
                    {
                        armor_textolite = s.ArmorType;
                    }
                }

                // 3bm32 
                ammo_3bm32 = new AmmoType();
                ShallowCopy(ammo_3bm32, ammo_3bm15);
                ammo_3bm32.Name = "3BM32 APFSDS-T";
                ammo_3bm32.Caliber = 125;
                ammo_3bm32.RhaPenetration = 540;
                ammo_3bm32.MuzzleVelocity = 1700f;
                ammo_3bm32.Mass = 6.5f;

                ammo_codex_3bm32 = ScriptableObject.CreateInstance<AmmoCodexScriptable>();
                ammo_codex_3bm32.AmmoType = ammo_3bm32;
                ammo_codex_3bm32.name = "ammo_3bm32";

                clip_3bm32 = new AmmoType.AmmoClip();
                clip_3bm32.Capacity = 1;
                clip_3bm32.Name = "3BM32 APFSDS-T";
                clip_3bm32.MinimalPattern = new AmmoCodexScriptable[1];
                clip_3bm32.MinimalPattern[0] = ammo_codex_3bm32;

                clip_codex_3bm32 = ScriptableObject.CreateInstance<AmmoClipCodexScriptable>();
                clip_codex_3bm32.name = "clip_3bm32";
                clip_codex_3bm32.ClipType = clip_3bm32;

                armor_superTextolite = new ArmorType();
                ShallowCopy(armor_superTextolite, armor_textolite);
                armor_superTextolite.RhaeMultiplierCe = 1.2f;
                armor_superTextolite.RhaeMultiplierKe = 0.8f;
                armor_superTextolite.Name = "super textolite";

                armor_codex_superTextolite = ScriptableObject.CreateInstance<ArmorCodexScriptable>();
                armor_codex_superTextolite.hideFlags = HideFlags.DontUnloadUnusedAsset;
                armor_codex_superTextolite.name = "super textolite";
                armor_codex_superTextolite.ArmorType = armor_superTextolite;
            }

            foreach (GameObject armour in GameObject.FindGameObjectsWithTag("Penetrable"))
            {
                VariableArmor texolitePlate = armour.GetComponent<VariableArmor>();

                if (texolitePlate == null) continue;
                if (texolitePlate.Unit.FriendlyName != "T-72M1") continue;
                if (texolitePlate.Name != "glass textolite layers") continue;

                MelonLogger.Msg("ya");

                MelonLogger.Msg(armor_codex_superTextolite);
                FieldInfo armorPlate = typeof(VariableArmor).GetField("_armorType", BindingFlags.NonPublic | BindingFlags.Instance);
                armorPlate.SetValue(texolitePlate, armor_codex_superTextolite);
            }

            foreach (GameObject vic_go in vic_gos)
            {
                Vehicle vic = vic_go.GetComponent<Vehicle>();

                if (vic == null) continue;

                if (vic.FriendlyName == "T-72M1")
                {

                    gameManager = GameObject.Find("_APP_GHPC_");
                    cameraManager = gameManager.GetComponent<CameraManager>();
                    playerManager = gameManager.GetComponent<PlayerInput>();

                    GameObject ammo_3bm32_vis = null;

                    // generate visual models 
                    if (ammo_3bm32_vis == null)
                    {
                        ammo_3bm32_vis = GameObject.Instantiate(ammo_3bm15.VisualModel);
                        ammo_3bm32_vis.name = "3BM32 visual";
                        ammo_3bm32.VisualModel = ammo_3bm32_vis;
                        ammo_3bm32.VisualModel.GetComponent<AmmoStoredVisual>().AmmoType = ammo_3bm32;
                        ammo_3bm32.VisualModel.GetComponent<AmmoStoredVisual>().AmmoScriptable = ammo_codex_3bm32;
                    }

                    // rename to t72b
                    FieldInfo friendlyName = typeof(GHPC.Unit).GetField("_friendlyName", BindingFlags.NonPublic | BindingFlags.Instance);
                    friendlyName.SetValue(vic, "T-72B");

                    FieldInfo uniqueName = typeof(GHPC.Unit).GetField("_uniqueName", BindingFlags.NonPublic | BindingFlags.Instance);
                    uniqueName.SetValue(vic, "T-72B");

                    // convert ammo
                    LoadoutManager loadoutManager = vic.GetComponent<LoadoutManager>();
                    WeaponsManager weaponsManager = vic.GetComponent<WeaponsManager>();
                    WeaponSystemInfo mainGunInfo = weaponsManager.Weapons[0];
                    WeaponSystem mainGun = mainGunInfo.Weapon;

                    loadoutManager.LoadedAmmoTypes[0] = clip_codex_3bm32;

                    for (int i = 0; i <= 2; i++)
                    {
                        GHPC.Weapons.AmmoRack rack = loadoutManager.RackLoadouts[i].Rack;
                        rack.ClipTypes[0] = clip_codex_3bm32.ClipType;
                        EmptyRack(rack);
                    }

                    loadoutManager.SpawnCurrentLoadout();

                    PropertyInfo roundInBreech = typeof(AmmoFeed).GetProperty("AmmoTypeInBreech");
                    roundInBreech.SetValue(mainGun.Feed, null);

                    MethodInfo refreshBreech = typeof(AmmoFeed).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic);
                    refreshBreech.Invoke(mainGun.Feed, new object[] { });

                    // update ballistics computer
                    MethodInfo registerAllBallistics = typeof(LoadoutManager).GetMethod("RegisterAllBallistics", BindingFlags.Instance | BindingFlags.NonPublic);
                    registerAllBallistics.Invoke(loadoutManager, new object[] { });
                }
            }
        }
    }
}