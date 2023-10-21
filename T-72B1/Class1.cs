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
using GHPC.Utility;
using GHPC;
using NWH.VehiclePhysics;

// todo 
// improve stab
// prevent changing zero w/ atgm loaded 
// reticle for atgm 
// e 

namespace T_72B1
{
    public class T72MOD : MelonMod
    {
        WeaponSystemCodexScriptable gun_2a46m;

        AmmoClipCodexScriptable clip_codex_3bm32;
        AmmoType.AmmoClip clip_3bm32;
        AmmoCodexScriptable ammo_codex_3bm32;
        AmmoType ammo_3bm32;

        AmmoClipCodexScriptable clip_codex_svit;
        AmmoType.AmmoClip clip_svit;
        AmmoCodexScriptable ammo_codex_svit;
        AmmoType ammo_svit;

        AmmoType ammo_3bm15;
        AmmoType ammo_9m111;


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

                foreach (AmmoCodexScriptable s in Resources.FindObjectsOfTypeAll(typeof(AmmoCodexScriptable)))
                {
                    if (s.AmmoType.Name == "9M111 Fagot")
                    {
                        ammo_9m111 = s.AmmoType;
                    }
                }

                foreach (ArmorCodexScriptable s in Resources.FindObjectsOfTypeAll(typeof(ArmorCodexScriptable)))
                {
                    if (s.ArmorType.Name == "glass textolite")
                    {
                        armor_textolite = s.ArmorType;
                    }
                }

                // 2a46m
                gun_2a46m = ScriptableObject.CreateInstance<WeaponSystemCodexScriptable>();
                gun_2a46m.name = "gun_2a46m";
                gun_2a46m.CaliberMm = 120;
                gun_2a46m.FriendlyName = "125mm Gun 2A46M";
                gun_2a46m.Type = WeaponSystemCodexScriptable.WeaponType.LargeCannon;

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

                // svit
                ammo_svit = new AmmoType();
                ShallowCopy(ammo_svit, ammo_9m111);
                ammo_svit.Name = "9M119 Svit ATGM";
                ammo_svit.Caliber = 125;
                ammo_svit.RhaPenetration = 750;
                ammo_svit.MuzzleVelocity = 350;
                ammo_svit.Mass = 26f;
                ammo_svit.ArmingDistance = 70;
                ammo_svit.SpallMultiplier = 1.45f;
                ammo_svit.TurnSpeed = 0.24f; 
                ammo_svit.VisualModel = ammo_9m111.VisualModel; 

                ammo_codex_svit = ScriptableObject.CreateInstance<AmmoCodexScriptable>();
                ammo_codex_svit.AmmoType = ammo_svit;
                ammo_codex_svit.name = "ammo_svit";

                clip_svit = new AmmoType.AmmoClip();
                clip_svit.Capacity = 1;
                clip_svit.Name = "9M119 Svit ATGM";
                clip_svit.MinimalPattern = new AmmoCodexScriptable[1];
                clip_svit.MinimalPattern[0] = ammo_codex_svit;

                clip_codex_svit = ScriptableObject.CreateInstance<AmmoClipCodexScriptable>();
                clip_codex_svit.name = "clip_svit";
                clip_codex_svit.ClipType = clip_svit;

                armor_superTextolite = new ArmorType();
                ShallowCopy(armor_superTextolite, armor_textolite);
                armor_superTextolite.RhaeMultiplierCe = 1.2f;
                armor_superTextolite.RhaeMultiplierKe = 0.885f;
                armor_superTextolite.Name = "super textolite";

                armor_codex_superTextolite = ScriptableObject.CreateInstance<ArmorCodexScriptable>();
                armor_codex_superTextolite.name = "super textolite";
                armor_codex_superTextolite.ArmorType = armor_superTextolite;
            }


            foreach (GameObject armour in GameObject.FindGameObjectsWithTag("Penetrable"))
            {
                if (armour == null) continue;

                VariableArmor texolitePlate = armour.GetComponent<VariableArmor>();

                if (texolitePlate == null) continue;
                if (texolitePlate.Unit == null) continue;
                if (texolitePlate.Unit.FriendlyName != "T-72M1") continue;
                if (texolitePlate.Name != "glass textolite layers") continue;

                FieldInfo armorPlate = typeof(VariableArmor).GetField("_armorType", BindingFlags.NonPublic | BindingFlags.Instance);
                armorPlate.SetValue(texolitePlate, armor_codex_superTextolite);

                MelonLogger.Msg(texolitePlate.ArmorType);
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
                    GameObject ammo_svit_vis = null;


                    // generate visual models 
                    if (ammo_3bm32_vis == null)
                    {
                        ammo_3bm32_vis = GameObject.Instantiate(ammo_3bm15.VisualModel);
                        ammo_3bm32_vis.name = "3BM32 visual";
                        ammo_3bm32.VisualModel = ammo_3bm32_vis;
                        ammo_3bm32.VisualModel.GetComponent<AmmoStoredVisual>().AmmoType = ammo_3bm32;
                        ammo_3bm32.VisualModel.GetComponent<AmmoStoredVisual>().AmmoScriptable = ammo_codex_3bm32;
                    }

                    if (ammo_svit_vis == null)
                    {
                        ammo_svit_vis = GameObject.Instantiate(ammo_3bm15.VisualModel);
                        ammo_svit_vis.name = "Svit visual";
                        ammo_svit.VisualModel = ammo_svit_vis;
                        ammo_svit.VisualModel.GetComponent<AmmoStoredVisual>().AmmoType = ammo_svit;
                        ammo_svit.VisualModel.GetComponent<AmmoStoredVisual>().AmmoScriptable = ammo_codex_svit;
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

                    mainGunInfo.Name = "125mm gun 2A46M";
                    FieldInfo codex = typeof(WeaponSystem).GetField("CodexEntry", BindingFlags.NonPublic | BindingFlags.Instance);
                    codex.SetValue(mainGun, gun_2a46m);

                    loadoutManager.LoadedAmmoTypes[0] = clip_codex_3bm32;
                    loadoutManager.LoadedAmmoTypes[2] = clip_codex_svit;

                    // [0] = ap [1] = heat [2] = he 
                    for (int i = 0; i <= 2; i++)
                    {
                        GHPC.Weapons.AmmoRack rack = loadoutManager.RackLoadouts[i].Rack;
                        rack.ClipTypes[0] = clip_codex_3bm32.ClipType;
                        rack.ClipTypes[2] = clip_codex_svit.ClipType;
                        EmptyRack(rack);
                    }

                    loadoutManager.SpawnCurrentLoadout();

                    PropertyInfo roundInBreech = typeof(AmmoFeed).GetProperty("AmmoTypeInBreech");
                    roundInBreech.SetValue(mainGun.Feed, null);

                    MethodInfo refreshBreech = typeof(AmmoFeed).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic);
                    refreshBreech.Invoke(mainGun.Feed, new object[] { });

                    // attach guidance computer
                    GameObject guidance_computer_obj = new GameObject("guidance computer");
                    guidance_computer_obj.transform.parent = vic.transform;
                    guidance_computer_obj.AddComponent<MissileGuidanceUnit>();
                    guidance_computer_obj.AddComponent<Reparent>(); 

                    MissileGuidanceUnit computer = guidance_computer_obj.GetComponent<MissileGuidanceUnit>();
                    Reparent reparent = guidance_computer_obj.GetComponent<Reparent>();
                    reparent.NewParent = vic_go.transform.Find("---MESH---/HULL/TURRET").gameObject.transform;
                    typeof(Reparent).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(reparent, new object[] {});

                    computer.AimElement = mainGunInfo.FCS.AimTransform;
                    mainGun.GuidanceUnit = computer; 

                    //engine upgrade 
                    VehicleController vehicleController = vic_go.GetComponent<VehicleController>();
                    vehicleController.engine.maxPower = 920; 

                    // update ballistics computer
                    MethodInfo registerAllBallistics = typeof(LoadoutManager).GetMethod("RegisterAllBallistics", BindingFlags.Instance | BindingFlags.NonPublic);
                    registerAllBallistics.Invoke(loadoutManager, new object[] { });
                }
            }
        }
    }
}