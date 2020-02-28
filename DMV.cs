using System;
using System.Collections.Generic;
using Oxide.Core;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

namespace Oxide.Plugins
{
    [Info("DMV", "Shawhiz", "0.1")]
    [Description("Gives the ability to spawn vehicles on a configurable cooldown/cost")]
    public class DMV : RustPlugin
    {
        #region Data setup

        private List<SpawnRecord> SpawnRecords = new List<SpawnRecord>();


        private String GetPrefab(Type type)
        {
            switch (type)
            {
                case Type.Mini: return "assets/content/vehicles/minicopter/minicopter.entity.prefab";
                case Type.Tranny: return "assets/content/vehicles/scrap heli carrier/scraptransporthelicopter.prefab";
                case Type.Rowboat: return "assets/content/vehicles/boats/rowboat/rowboat.prefab";
                case Type.Speedboat: return "assets/content/vehicles/boats/rhib/rhib.prefab";
                default: return "";
            }
        }

        private Type? GetType(string type)
        {
            switch (type)
            {
                case "mini" : return Type.Mini;
                case "tranny": return Type.Tranny;
                case "boat": return Type.Rowboat;
                case "rowboat": return Type.Rowboat;
                case "rhib": return Type.Speedboat;
                case "speedboat": return Type.Speedboat;
                default: return null;
            }
        }

        private enum PermissionLevel {
            
            licensed = 0,
            vip  = 1,
            admin =2,
            none = 3
            
        }
        private enum Type
        {
            Mini = 0,
            Tranny = 1,
            Rowboat = 2,
            Speedboat = 3
        }

        private class Spawn
        {
            public DateTime time;
            public Vector3 location;
            public Type type;

            public Spawn(DateTime time, Vector3 location, Type type)
            {
                this.time = time;
                this.location = location;
                this.type = type;
            }
        }

        private class SpawnRecord
        {
            public SpawnRecord(ulong? userId, List<Spawn> spawns)
            {
                this.userId = userId;
                this.spawns = spawns;
            }

            public ulong? userId;
            public List<Spawn> spawns;
        }
        #endregion
        #region Config

        private static ConfigData configData;
        private class  ConfigData
        {
            public Dictionary<PermissionLevel, List<TypeConfigData>> configData = new Dictionary<PermissionLevel, List<TypeConfigData>>();
        }

        private class TypeConfigData
        {
            public Type type;
            public int cooldown_seconds;
            public bool cost;
            
            public Dictionary<String, int> materials;

            public TypeConfigData(Type type, int cooldown_seconds, bool cost, Dictionary<string, int> materials)
            {
                this.type = type;
                this.cooldown_seconds = cooldown_seconds;
                this.cost = cost;
                this.materials = materials;
            }
        }


        private void LoadVariables()
        {
            LoadConfigVariables();
            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            var demoMaterials = new Dictionary<String, int>
            {
                {"metal.refined", 15}
            };
            var demoLicensedConfigDataList = new List<TypeConfigData>();
            
            var demoVipConfigDataList = new List<TypeConfigData>();
            
            var demoAdminConfigDataList = new List<TypeConfigData>();

            
            foreach (Type type in Enum.GetValues(typeof(Type)))
            {
                demoVipConfigDataList.Add(new TypeConfigData(type, 600, true, demoMaterials));
                demoLicensedConfigDataList.Add(new TypeConfigData(type, 3600, true, demoMaterials));
                demoAdminConfigDataList.Add(new TypeConfigData(type, 0, false, demoMaterials));
            }

            var config = new ConfigData
            {
                configData = new Dictionary<PermissionLevel, List<TypeConfigData>>
                {
                    {PermissionLevel.licensed, demoLicensedConfigDataList},
                    {PermissionLevel.vip, demoVipConfigDataList},
                    {PermissionLevel.admin, demoAdminConfigDataList}
                }
            };
          

                
                
                
                
                
            SaveConfig(config);
        }

        void SaveConfig(ConfigData config) => Config.WriteObject(config, true);


        private void LoadConfigVariables() => configData = Config.ReadObject<ConfigData>();

        #endregion
        
        #region Oxide Hooks

        void Init()
        {
            LoadVariables();
            LoadConfig();
            permission.RegisterPermission("dmv.licensed", this);
            permission.RegisterPermission("dmv.vip", this);
            permission.RegisterPermission("dmv.admin", this);

            RegisterMessages();
        }

        void Loaded()
        {
            try
            {
                SpawnRecords =
                    Interface.Oxide.DataFileSystem.ReadObject<List<SpawnRecord>>(
                        "DMVSpawnRecords");
                ;
            }
            catch
            {
                SpawnRecords = new List<SpawnRecord>();
            }
        }

        void OnServerSave()
        {
            SaveData();
        }

        #endregion

        #region Chat Commands

        [ChatCommand("mini")]
        void CmdMini(BasePlayer player, string command, string[] args)
        {
            SpawnVehicle(player, Type.Mini);
        }
        
    [ChatCommand("tranny")]
        void CmdTranny(BasePlayer player, string command, string[] args)
        {
            SpawnVehicle(player, Type.Tranny);
        }  
        
        [ChatCommand("rowboat")]
        void CmdRowboat(BasePlayer player, string command, string[] args)
        {
            SpawnVehicle(player, Type.Rowboat);
        }  
        
        [ChatCommand("rhib")]
        void CmdRhib(BasePlayer player, string command, string[] args)
        {
            SpawnVehicle(player, Type.Speedboat);
        }
        [ChatCommand("speedboat")]
        void CmdSpeedboat(BasePlayer player, string command, string[] args)
        {
            SpawnVehicle(player, Type.Speedboat);
        }

        [ChatCommand("dmvcost")]
        void CmdCost(BasePlayer player, string command, string[] args)
        {
            if (args.Length > 0)
            {
                var type = GetType(args[0]);
                if (type != null)
                {
                    SendCostBreakdown(player,(Type) type);
                }
                else
                {
                    SendReply(player, args[0] + " is not a valid type of vehicle");
                }
            }
        } 
        [ChatCommand("dmv")]
        void CmdSpawn(BasePlayer player, string command, string[] args)
        {
            if (args.Length > 0)
            {
                var type = GetType(args[0]);
                if (type != null)
                {
                    SpawnVehicle(player, (Type) type);
                }
                else
                {
                    SendReply(player, args[0] + " is not a valid type of vehicle at the Rust Dept. of Motor Vehicles.");
                }
            }
        }

        #endregion

        #region  Helpers

        private void SendCostBreakdown(BasePlayer player, Type type)
        {
            if (GetPermissionLevel(player) != PermissionLevel.none)
            {
                var costs = GetConfigCosts(GetPermissionLevel(player), type);
                if (costs.Count == 0)
                {
                    SendReply(player, covalence.FormatText(lang.GetMessage("no_costs", this)));
                }
                else
                {
                    var itemDefinitions = ItemManager.GetItemDefinitions();


                    string costBreakdown = lang.GetMessage("cost_breakdown", this) + "\n";
                    foreach (var cost in costs)
                    {
                        var materialName = cost.Key.ToLower();
                        var materialQty = cost.Value;

                        var displayName = itemDefinitions.Find(s => s.shortname.ToLower().Equals(materialName))
                            .displayName
                            .english;

                        costBreakdown += materialQty + " " + displayName + "\n";
                    }

                    SendReply(player, costBreakdown, this);
                }
            }

            else
            {
                PermissionFailed(player);
            }
        }
        private bool CheckInventoryToPay(BasePlayer player, Type type)
        {
            var canPay = true;

            if (GetPermissionLevel(player) != PermissionLevel.none)
            {
                var costs = (GetConfigCosts(GetPermissionLevel(player), type));
                if (costs == null)
                {
                    return true;
                }

                var itemDefinitions = ItemManager.GetItemDefinitions();
                foreach (var cost in costs)
                {
                    var materialName = cost.Key.ToLower();
                    var materialQty = cost.Value;

                    var materialId = 0;

                    var itemid = itemDefinitions.Find(s => s.shortname.ToLower().Equals(materialName))
                        ?.itemid;
                    if (itemid != null) materialId = (int) itemid;

                    if (player.inventory.GetAmount(materialId) < materialQty) canPay = false;
                    Puts(materialName + materialQty + " in inventory" + player.inventory.GetAmount(materialId));
                    if (!canPay)
                    {
                        SendReply(player, covalence.FormatText(lang.GetMessage("cant_afford", this)));
                        break;
                    }
                }

            }
            else
            {
                canPay = false;
                PermissionFailed(player);
            }
            
            return canPay;

        }

        private void PayForVehicle(BasePlayer player, Type type)
        {
            var costs =GetConfigCosts(GetPermissionLevel(player), type);
            var collect = new List<Item>();
            var itemDefinitions = ItemManager.GetItemDefinitions();
                foreach (var cost in costs)
                {
                    var materialName = cost.Key.ToLower();
                    var materialQty = cost.Value;
                    var materialId = 0;

                    var itemid = itemDefinitions.Find(s => s.shortname.ToLower().Equals(materialName))
                        ?.itemid;
                    if (itemid != null) materialId = (int) itemid;

                    player.inventory.Take(collect, materialId, materialQty);
                }

                foreach (var item in collect)
                {
                    item.Remove(0f);
                }
        }
            
        private PermissionLevel GetPermissionLevel(BasePlayer player)
        {

            var grantedPermission = PermissionLevel.none;
            
            if (permission.UserHasPermission(player.UserIDString, "dmv."+PermissionLevel.admin.ToString()))
            {
                grantedPermission = PermissionLevel.admin;
            } else if (permission.UserHasPermission(player.UserIDString, "dmv."+PermissionLevel.vip.ToString()))
            {
                grantedPermission = PermissionLevel.vip;
            }else if (permission.UserHasPermission(player.UserIDString, "dmv."+PermissionLevel.licensed.ToString()))
            {
                grantedPermission = PermissionLevel.licensed;
            }

            return grantedPermission;
        }

        private int GetConfigCooldownSeconds(PermissionLevel permissionLevel, Type type)
        {
            var playersLevel = configData.configData[permissionLevel];
            foreach (var cd in playersLevel)
            {
                if (cd.type == type)
                {
                    return cd.cooldown_seconds;
                }
            }
            return 0;
        }    
        
        private Dictionary<String, int> GetConfigCosts(PermissionLevel permissionLevel, Type type)
        {
            if (configData.configData[permissionLevel].Find(perm => perm.type == type).cost)
            {
                return configData.configData[permissionLevel].Find(cd => cd.type == type).materials;
            } 
            return new Dictionary<string, int>();
        }

        private void SpawnVehicle(BasePlayer player, Type type)
        {
            var permissionLevel = GetPermissionLevel(player);
            if (permissionLevel == PermissionLevel.none) {
                Puts( GetUsername(player) + "requested to spawn a " + type + ", but has no permissions");
            }
            else
            {
                var position = player.eyes.position + (player.eyes.MovementForward() * 5f);

                if (!CheckSpawnCooldown(player, type) || !CheckInventoryToPay(player, type)) return;
                try
                {
                    PayForVehicle(player, type);
                    ClearSpawn(player, type);
                    AddNewSpawn(player, type, position);
                    CreateItem(player, type, position);
                    Puts(GetUsername(player) + " ("+ player.userID+ ")  spawned a " + type);
                }
                catch
                {
                    ProblemWithSpawn(player, type);
                }
                finally
                {
                    SaveData();
                }
            }
           
        }

        private void ProblemWithSpawn(BasePlayer player, Type type)
        {
            Puts("Problem Spawning "+ type + " for " +GetUsername(player) );
            SendReply(player, "There was a problem creating your " + type + ". Please try again or contact an admin if the problem persists.");

        }

        private string GetUsername(BasePlayer player)
        {
            return covalence.Players.FindPlayerById(player.UserIDString)?.Name;
        }
        private void CreateItem(BasePlayer player, Type type, Vector3 position)
        {
            var prefab = GetPrefab(type);
            var vehicle = GameManager.server.CreateEntity(prefab, position,Quaternion.Euler(0, player.eyes.rotation.eulerAngles.y - 90f, 0)); 
            vehicle.Spawn();
        }
        private void AddNewSpawn(BasePlayer player, Type type, Vector3 position)
        {

            var playersFullRecord = SpawnRecords.Find(s => s.userId == player.userID);
            if(playersFullRecord == null){ SpawnRecords.Add(new SpawnRecord(player.userID, new List<Spawn>()));}
            SpawnRecords.Find(s => s.userId == player.userID).spawns.Add( new Spawn(DateTime.Now, position, type));
        }
        private void ClearSpawn(BasePlayer player, Type type)
        {
            if (SpawnRecords.Find(s => s.userId == player.userID) != null)
            {
                var results = SpawnRecords.Find(s => s.userId == player.userID).spawns;
                var typeResults = CheckForExistingSpawns(player, type);
                if (typeResults != null)
                {
                    SpawnRecords.Find(s => s.userId ==  player.userID).spawns.Remove(typeResults);
                }
            }
        }
        
        
        private Spawn CheckForExistingSpawns(BasePlayer player, Type type)
        { 
          var results = SpawnRecords.Find(s => s.userId == player.userID);
          if (results != null)
          {
              var typeResults = results.spawns.Find(s => s.type == type);
              return typeResults;
          }
          return null;
        }
        
        private bool CheckSpawnCooldown(BasePlayer player, Type type)
        //returns true unless cooldown period isn't up yet.
        {
            var canSpawn = true;
            var cooldown = GetConfigCooldownSeconds(GetPermissionLevel(player), type);
            var spawnRecord = CheckForExistingSpawns(player, type);
            if (spawnRecord != null)
            {
                canSpawn = (DateTime.Now - spawnRecord.time).TotalSeconds >= cooldown;
            }

            if (!canSpawn)
            {
                SendReply(player, "Your cooldown is not up yet. The cooldown is " + FormattedCooldownTime(cooldown));
            }

            return canSpawn;
        }

        
        private void SaveData()
        {
            if (SpawnRecords == null) return;
            Interface.Oxide.DataFileSystem.WriteObject("DMVSpawnRecords", SpawnRecords);
        }
        private void RegisterMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["permission_failed"] = "You do  [#red] not [/#] have permission to use this command!",
                ["no_costs"] ="This server has made that item free of charge",
                ["cost_breakdown"] = "The charge for that item is as follows:",
            }, this, "en");
        }

        private string FormattedCooldownTime(int time)
        {
            string cooldownTime = "";
            if (time < 60) //less than 1 minute
            {
                cooldownTime = time + " seconds ";
            }

            else if (time < 60 * 60) //less than an hour
            {
                cooldownTime = time / 60 + " minutes ";
            }
            else //more than an hour
            {
                cooldownTime = time / (60 * 60) + " hours and " +
                               time % (60 / 60) + " minutes";
            }

            return cooldownTime;
        }

        private void PermissionFailed(BasePlayer player)
        {
            SendReply(player, covalence.FormatText(lang.GetMessage("permission_failed", this)));
        }
        
        #endregion
        
    }
}