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
using Reticle;

// todo 
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

        AmmoClipCodexScriptable clip_codex_svir;
        AmmoType.AmmoClip clip_svir;
        AmmoCodexScriptable ammo_codex_svir;
        AmmoType ammo_svir;

        AmmoType ammo_3bm15;
        AmmoType ammo_9m111;
        AmmoType ammo_3of26; 

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

        public override void OnUpdate()
        {
            if (!playerManager) return;
            if (playerManager.CurrentPlayerWeapon == null) return;
            if (playerManager.CurrentPlayerWeapon.Name != "125mm gun 2A46M") return;

            FireControlSystem FCS = playerManager.CurrentPlayerWeapon.FCS;
            ParticleSystem[] particleSystem = playerManager.CurrentPlayerWeapon.Weapon.MuzzleEffects;

            FieldInfo reticleCurrentRange = typeof(ReticleMesh).GetField("curReticleRange", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo reticleTargetRange = typeof(ReticleMesh).GetField("targetReticleRange", BindingFlags.NonPublic | BindingFlags.Instance);
            PropertyInfo deltad = typeof(FireControlSystem).GetProperty("ActiveDeltaD", BindingFlags.Public | BindingFlags.Instance);

            if (FCS.CurrentAmmoType.Name == "9M119 Svir")
            {
                deltad.SetValue(FCS, false);
                particleSystem[0].transform.GetChild(0).transform.gameObject.SetActive(false);
                particleSystem[0].transform.GetChild(1).transform.gameObject.SetActive(false);
                particleSystem[0].transform.GetChild(3).transform.gameObject.SetActive(false);

                // set range to 0 and lock it 
                reticleCurrentRange.SetValue(FCS.MainOptic.reticleMesh, 0);
                reticleTargetRange.SetValue(FCS.MainOptic.reticleMesh, 0);
            }
            else {
                deltad.SetValue(FCS, true);
                particleSystem[0].transform.GetChild(0).transform.gameObject.SetActive(true);
                particleSystem[0].transform.GetChild(1).transform.gameObject.SetActive(true);
                particleSystem[0].transform.GetChild(3).transform.gameObject.SetActive(true);
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

                    if (s.AmmoType.Name == "9M111 Fagot")
                    {
                        ammo_9m111 = s.AmmoType;
                    }

                    if (s.AmmoType.Name == "3OF26 HEF-FS-T")
                    {
                        ammo_3of26 = s.AmmoType;
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
                gun_2a46m.CaliberMm = 125;
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

                // svir
                ammo_svir = new AmmoType();
                ShallowCopy(ammo_svir, ammo_9m111);
                ammo_svir.Name = "9M120 SVIR";
                ammo_svir.Guidance = AmmoType.GuidanceType.Laser;
                ammo_svir.Caliber = 125;
                ammo_svir.RhaPenetration = 800;
                ammo_svir.MuzzleVelocity = 350;
                ammo_svir.Mass = 26f;
                ammo_svir.ArmingDistance = 70;
                ammo_svir.SpallMultiplier = 1.45f;
                ammo_svir.SpiralPower = 17.5f;
                ammo_svir.TntEquivalentKg = 4.6f; 
                ammo_svir.TurnSpeed = 0.33f;
                ammo_svir.SpiralAngularRate = 1200f;
                ammo_svir.RangedFuseTime = 12.5f;
                ammo_svir.MaximumRange = 4000;
                ammo_svir.MaxSpallRha = 12f;
                ammo_svir.MinSpallRha = 4f;
                ammo_svir.CertainRicochetAngle = 3f;
                ammo_svir.ShotVisual = ammo_9m111.ShotVisual; 

                ammo_codex_svir = ScriptableObject.CreateInstance<AmmoCodexScriptable>();
                ammo_codex_svir.AmmoType = ammo_svir;
                ammo_codex_svir.name = "ammo_svir";

                clip_svir = new AmmoType.AmmoClip();
                clip_svir.Capacity = 1;
                clip_svir.Name = "9M120 SVIR";
                clip_svir.MinimalPattern = new AmmoCodexScriptable[1];
                clip_svir.MinimalPattern[0] = ammo_codex_svir;

                clip_codex_svir = ScriptableObject.CreateInstance<AmmoClipCodexScriptable>();
                clip_codex_svir.name = "clip_svir";
                clip_codex_svir.ClipType = clip_svir;

                armor_superTextolite = new ArmorType();
                ShallowCopy(armor_superTextolite, armor_textolite);
                armor_superTextolite.RhaeMultiplierCe = 1.2f;
                armor_superTextolite.RhaeMultiplierKe = 0.89f;
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
                    GameObject ammo_svir_vis = null;

                    // generate visual models 
                    if (ammo_3bm32_vis == null)
                    {
                        ammo_3bm32_vis = GameObject.Instantiate(ammo_3bm15.VisualModel);
                        ammo_3bm32_vis.name = "3BM32 visual";
                        ammo_3bm32.VisualModel = ammo_3bm32_vis;
                        ammo_3bm32.VisualModel.GetComponent<AmmoStoredVisual>().AmmoType = ammo_3bm32;
                        ammo_3bm32.VisualModel.GetComponent<AmmoStoredVisual>().AmmoScriptable = ammo_codex_3bm32;
                    }

                    if (ammo_svir_vis == null)
                    {
                        ammo_svir_vis = GameObject.Instantiate(ammo_3of26.VisualModel);
                        ammo_svir_vis.name = "svir visual";
                        ammo_svir.VisualModel = ammo_svir_vis;
                        ammo_svir.VisualModel.GetComponent<AmmoStoredVisual>().AmmoType = ammo_svir;
                        ammo_svir.VisualModel.GetComponent<AmmoStoredVisual>().AmmoScriptable = ammo_codex_svir;
                    }

                    // rename to t72b
                    FieldInfo friendlyName = typeof(GHPC.Unit).GetField("_friendlyName", BindingFlags.NonPublic | BindingFlags.Instance);
                    friendlyName.SetValue(vic, "T-72B");

                    FieldInfo uniqueName = typeof(GHPC.Unit).GetField("_uniqueName", BindingFlags.NonPublic | BindingFlags.Instance);
                    uniqueName.SetValue(vic, "T-72B");

                    // convert weapon system and FCS
                    LoadoutManager loadoutManager = vic.GetComponent<LoadoutManager>();
                    WeaponsManager weaponsManager = vic.GetComponent<WeaponsManager>();
                    WeaponSystemInfo mainGunInfo = weaponsManager.Weapons[0];
                    WeaponSystem mainGun = mainGunInfo.Weapon;

                    mainGun.Feed.ReloadDuringMissileTracking = true;
                    mainGunInfo.FCS.MainOptic.slot.VibrationBlurScale = 0.0f;
                    mainGunInfo.FCS.MainOptic.slot.VibrationShakeMultiplier = 0.2f;
                    mainGunInfo.FCS.MainOptic.slot.OtherFovs = new float[] {3.5f}; 

                    mainGunInfo.Name = "125mm gun 2A46M";
                    FieldInfo codex = typeof(WeaponSystem).GetField("CodexEntry", BindingFlags.NonPublic | BindingFlags.Instance);
                    codex.SetValue(mainGun, gun_2a46m);

                    loadoutManager.LoadedAmmoTypes[0] = clip_codex_3bm32;
                    loadoutManager.LoadedAmmoTypes[2] = clip_codex_svir;

                    // [0] = ap [1] = heat [2] = he 
                    for (int i = 0; i <= 2; i++)
                    {
                        GHPC.Weapons.AmmoRack rack = loadoutManager.RackLoadouts[i].Rack;
                        rack.ClipTypes[0] = clip_codex_3bm32.ClipType;
                        rack.ClipTypes[2] = clip_codex_svir.ClipType;
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
                    Reparent reparent = guidance_computer_obj.GetComponent<Reparent>();
                    reparent.NewParent = vic_go.transform.Find("---MESH---/HULL/TURRET").gameObject.transform;
                    typeof(Reparent).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(reparent, new object[] {});

                    MissileGuidanceUnit computer = guidance_computer_obj.GetComponent<MissileGuidanceUnit>();
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