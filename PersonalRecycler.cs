﻿using Oxide.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Physics = UnityEngine.Physics;
namespace Oxide.Plugins

{
    [Info("PersonalRecycler", "Shawhiz", "0.3")]
    [Description("Gives the ability to spawn a personal recycler bin")]
    public class PersonalRecycler : RustPlugin
    {
        #region Strings and Variables

        private const string recylerPrefab = "assets/bundled/prefabs/static/recycler_static.prefab";
        private const ulong recyclerSkinId = 1594245394;
        private List<SpawnRecord> SpawnRecords = new List<SpawnRecord>();

        #endregion

        #region Config

        private static ConfigData configData;

        private class ConfigData
        {
            public int max_recyclers;
            public bool recycler_cost;

            public Dictionary<string, int> recycler_materials = new Dictionary<string, int> { };
        }

        private class SpawnRecord
        {
            public SpawnRecord(ulong? userId, List<KeyValuePair<DateTime, Vector3>> spawns)
            {
                this.userId = userId;
                this.spawns = spawns;
            }

            public ulong? userId;
            public List<KeyValuePair<DateTime, Vector3>> spawns;
        }

        private void LoadVariables()
        {
            LoadConfigVariables();
            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            var config = new ConfigData
            {
                max_recyclers = 1,
                recycler_cost = false,
                recycler_materials = new Dictionary<string, int>()
            };

            SaveConfig(config);
        }

        private void LoadConfigVariables() => configData = Config.ReadObject<ConfigData>();

        void SaveConfig(ConfigData config) => Config.WriteObject(config, true);

        #endregion

        #region oxide hooks

        void Init()
        {
            LoadVariables();
            LoadConfig();
            permission.RegisterPermission("personalRecycler.allow", this);
            permission.RegisterPermission("personalRecycler.vip", this);
            permission.RegisterPermission("personalRecycler.admin", this);

            RegisterMessages();
        }


        void OnEntityKill(BaseNetworkable entity){
        
            if (!(entity is BuildingBlock && entity.PrefabName.Contains("foundation") || entity.PrefabName.Contains("floor"))) return;
            var items = new List<BaseEntity>();
            var position = entity.transform.position;
            var hits = FindEntities<BaseEntity>(position, 1f);

            foreach (var entityComponent in hits.ToList())
            {
               
                if (entityComponent.PrefabName.Equals("assets/bundled/prefabs/static/recycler_static.prefab"))
                {
                    Puts("found a recycler nearby");
                    
                    var foundRecords = SpawnRecords.Find(s => s.userId == entityComponent.OwnerID);
                    
                    Puts("recycler belongs to " + entityComponent.OwnerID);
                    foreach (var foundRecord in foundRecords.spawns)
                    {
                        var position1 = entityComponent.transform.position;
                        Puts($"foundPosition =  {foundRecord.Value.x}, {foundRecord.Value.y}, {foundRecord.Value.z}" );
                        Puts($"entityPosition =  {position1.x}, {position1.y}, {position1.z}" );
                        if (Math.Abs(Math.Abs(foundRecord.Value.x) - Math.Abs(position1.x )) < 2.5 &&
                            Math.Abs(Math.Abs(foundRecord.Value.y) - Math.Abs(position1.y )) < 2.5 && 
                            Math.Abs(Math.Abs(foundRecord.Value.z) - Math.Abs(position1.z )) < 2.5)
                        {
                            RemoveSpawnRecord(foundRecords);
                        }
                    }
                    var message = $"Recycler De-spawned for {entityComponent.OwnerID} at {entityComponent.transform.position.x}, {entityComponent.transform.position.y}, {entityComponent.transform.position.z} with the destruction of a {entity.PrefabName} ";
                    LogToFile($"Recyclers Destroyed", $"[{DateTime.Now.ToString("hh:mm:ss")}] {message}", this);

                    entityComponent.Kill();
                }
            }
        }


        List<BaseEntity> FindEntities<T>(Vector3 position, float distance = 3f) where T : BaseEntity
        {
            var list = new List<BaseEntity>();
            Vis.Entities(position, distance, list);
            return list;
        }

        void OnItemDropped(Item item, BaseEntity entity)
        {
            if (item.skin != recyclerSkinId) return;
            var players = new List<BasePlayer>();
            Vis.Entities<BasePlayer>(entity.transform.position, 1, players);

            item.Remove();

            foreach (var player in players)
            {
                RemoveSpawnRecord(player);
            }
        }


        void Loaded()
        {
            try
            {
                SpawnRecords =
                    Interface.Oxide.DataFileSystem.ReadObject<List<SpawnRecord>>(
                        "PersonalRecyclerSpawnRecords");
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


        void OnEntitySpawned(BaseNetworkable entity)
        {
            var baseEntity = entity?.GetComponent<BaseEntity>() ?? null;
            if (baseEntity == null) return;
            if (baseEntity.skinID != recyclerSkinId) return;
            var transform = entity.transform;
            var recycler = GameManager.server.CreateEntity(recylerPrefab, transform.position, transform.rotation);
            var player = BasePlayer.FindByID(baseEntity.OwnerID);
            recycler.OwnerID = player.userID;

            RaycastHit rhit;
            var cast = Physics.Raycast(entity.transform.position + new Vector3(0, 0.1f, 0), Vector3.down, out rhit, 4f,
                LayerMask.GetMask("Construction"));
            var distance = cast ? rhit.distance : 3f;

            if (distance > 0.2f)
            {
                RefundRecyclerItem(player);
                entity.Kill();
                SendReply(player, covalence.FormatText((lang.GetMessage("invalid_placement", this))));
            }

            else
            {
                var spawnRecord = GetSpawnsFor(player);
                if (spawnRecord == null || spawnRecord.spawns.Count < configData.max_recyclers)
                {
                    AddSpawnRecord(player);
                    recycler.Spawn();
                    entity.Kill();
                }
                
                else SendReply(player, covalence.FormatText(lang.GetMessage("cannot_loot_this", this)));
            }
        }

        #endregion

        #region Commands

        [ChatCommand("rc")]
        void CommandChatRecycler(BasePlayer player, string command, string[] args)
        {
            if (permission.UserHasPermission(player.UserIDString, "personalRecycler.allow"))
            {
                CreateRecyclerItem(player);
            }
            else
            {
                PermissionFailed(player);
            }
        }

        [ChatCommand("rs")]
        void CommandChatRecycler2(BasePlayer player, string command, string[] args)
        {
            if (permission.UserHasPermission(player.UserIDString, "personalRecycler.admin"))
            {
             
                    var item = ItemManager.CreateByName("workbench1");
                    item.skin = recyclerSkinId;
                    item.name = "Recycler";
                    SendReply(player, "Creating a recycler for you.");
                     player.GiveItem(item);
                    
                
            }
            else
            {
                PermissionFailed(player);
            }
        }

        [
            ChatCommand("rr")]
        void CommandChatRecyclerRemove(BasePlayer player, string command, string[] args)
        {
            if (permission.UserHasPermission(player.UserIDString, "personalRecycler.allow"))
            {
                RemoveRecycler(player);
            }
            else
            {
                PermissionFailed(player);
            }
        }

        [ChatCommand("rrall")]
        void CommandChatRecyclerRemoveAll(BasePlayer player, string command, string[] args)
        {
            if (permission.UserHasPermission(player.UserIDString, "personalRecycler.allow"))
            {
                RemoveRecyclers(player);
            }
            else
            {
                PermissionFailed(player);
            }
        }

        [ChatCommand("recyclerhelp")]
        void CommandChathelp(BasePlayer player, string command, string[] args)
        {
            SendReply(player, covalence.FormatText(lang.GetMessage("help_message", this)));
            RecyclerCostBreakdown(player);
        }

        [ChatCommand("rcost")]
        void CommandChatcost(BasePlayer player, string command, string[] args)
        {
            RecyclerCostBreakdown(player);
        }

        [ChatCommand("rclear")]
        void CommandChatClear(BasePlayer player, string command, string[] args)
        {
            if (permission.UserHasPermission(player.UserIDString, "personalRecycler.allow"))
            {
                RemoveSpawnRecord(player);
                SendReply(player, covalence.FormatText(lang.GetMessage("cleared_record", this)));
            }
            else PermissionFailed(player);
        }

        #endregion


        #region helper functions

        private void RecyclerCostBreakdown(BasePlayer player)
        {
            if (!configData.recycler_cost)
            {
                SendReply(player, covalence.FormatText(lang.GetMessage("free_recyclers", this)));
            }
            else
            {
                var itemDefinitions = ItemManager.GetItemDefinitions();


                string costBreakdown = lang.GetMessage("recycler_cost_breakdown", this) + "\n";
                foreach (var cost in configData.recycler_materials)
                {
                    var materialName = cost.Key.ToLower();
                    var materialQty = cost.Value;
                    if (permission.UserHasPermission(player.UserIDString, "personalRecycler.vip"))
                    {
                        materialQty /= 2;
                    }
                    var displayName = itemDefinitions.Find(s => s.shortname.ToLower().Equals(materialName)).displayName
                        .english;

                    costBreakdown += materialQty + " " + displayName + "\n";
                }

                SendReply(player, costBreakdown, this);
            }
        }

        private bool CanPay(BasePlayer player)
        {
            if (!configData.recycler_cost)
            {
                return true;
            }

            var itemDefinitions = ItemManager.GetItemDefinitions();
            var costs = configData.recycler_materials;
            var canPay = true;
            foreach (var cost in costs)
            {
                var materialName = cost.Key.ToLower();
                var materialQty = cost.Value;

                if (permission.UserHasPermission(player.UserIDString, "personalRecycler.vip"))
                {
                    materialQty /= 2;
                }
                
                var materialId = 0;

                var itemid = itemDefinitions.Find(s => s.shortname.ToLower().Equals(materialName))
                    ?.itemid;
                if (itemid != null) materialId = (int) itemid;

                if (player.inventory.GetAmount(materialId) < materialQty) canPay = false;
                if (!canPay)
                {
                    SendReply(player, covalence.FormatText(lang.GetMessage("cant_afford", this)));
                    break;
                }
            }

            return canPay;
        }

        private void PayForRecycler(BasePlayer player)
        {
            var itemDefinitions = ItemManager.GetItemDefinitions();
            var costs = configData.recycler_materials;
            List<Item> collect = new List<Item>();

            foreach (var cost in costs)
            {
                var materialName = cost.Key.ToLower();
                var materialQty = cost.Value;
                if (permission.UserHasPermission(player.UserIDString, "personalRecycler.vip"))
                {
                    materialQty /= 2;
                }
                
                var materialId = 0;

                var itemid = itemDefinitions.Find(s => s.shortname.ToLower().Equals(materialName))
                    ?.itemid;
                if (itemid != null) materialId = (int) itemid;

                player.inventory.Take(collect, materialId, materialQty);
            }

            foreach (Item item in collect)
            {
                item.Remove(0f);
            }
        }


        private SpawnRecord GetSpawnsFor(BasePlayer player)
        {
            return SpawnRecords.Find(s => s.userId == player.userID);
        }

        private void RegisterMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["permission_failed"] = "You do  [#red] not [/#] have permission to use this command!",
                ["cleared_record"] = "Your recycler spawn record has been reset.",
                ["maximum_recyclers"] = "You have already reached the maximum recycler count permitted.",
                ["invalid_placement"] = "You must place this on a foundation or floor you have built.",
                ["help_message"] =
                    "[#yellow]Personal Recycler Help[/#] \n To create a recycler type [#cyan]/rc [/#]\n To remove, look at the recycler and   type [#red]/rr[/#] \n To remove all recyclers type [#red]/rrall[/#]. \n To see the costs before creating one, type [#green]/rcost[/#]\n The max recyclers is " +
                    configData.max_recyclers + ". Attempting to place more will result in loss of the item.",
                ["cannot_loot_this"] = "You already have the maximum recyclers. Dont be greedy. ",
                ["free_recyclers"] = "This server has made recyclers free of charge.",
                ["recycler_cost_breakdown"] = "This server has set the charges for recyclers as follows:",
                ["cant_afford"] = "You do not have enough resources.",
            }, this, "en");
        }


        private void SaveData()
        {
            if (SpawnRecords == null) return;
            Interface.Oxide.DataFileSystem.WriteObject("PersonalRecyclerSpawnRecords", SpawnRecords);
        }

        private static BaseEntity FindEntity(BasePlayer player)
        {
            RaycastHit hit;
            if (!Physics.Raycast(player.eyes.HeadRay(), out hit)) return null;
            if (hit.GetEntity() == null)
                return null;
            var entity = hit.GetEntity();
            return entity;
        }
        private void SpawnRecycler_2(BasePlayer player)
        {
            if (GetSpawnsFor(player).spawns.Count >= configData.max_recyclers) return;
            var entity = FindEntity(player);
            if (entity == null || !entity._name.Contains("workbench1") || (entity.OwnerID != player.userID)) return;
            var recycler = GameManager.server.CreateEntity(recylerPrefab, entity.transform.position,
                entity.transform.rotation);
            recycler.OwnerID = player.userID;
            recycler.Spawn();
        }


        private void RemoveRecycler(BasePlayer player)
        {
            var entity = FindEntity(player);
            if (entity != null && entity.name.Contains("recycler_static") && entity.OwnerID == player.userID)
            {
                entity.Kill();
                RemoveSpawnRecord(player, GetSpawnsFor(player).spawns.Find(s => s.Value == entity.transform.position));
                RefundRecyclerItem(player);
            }
        }

        private void RemoveRecyclers(BasePlayer player)
        {
            if (GetSpawnsFor(player) == null) return;
            foreach (var spawn in GetSpawnsFor(player).spawns)
            {
                var list = new List<BaseEntity>();
                Vis.Entities<BaseEntity>(spawn.Value, 5f, list);
                foreach (var entity in list)
                {
                    if (entity.name.Contains("recycler_static") && entity.OwnerID == player.userID)
                    {
                        entity.Kill();
                        RemoveAllSpawnRecord(player);
                        RefundRecyclerItem(player);
                    }
                }
            }
        }

        private void CreateRecyclerItem(BasePlayer player)
        {
            var item = ItemManager.CreateByName("workbench1");
            item.skin = recyclerSkinId;
            item.name = "Recycler";

            if (configData.recycler_cost && CanPay(player) || !configData.recycler_cost)
            {
                PayForRecycler(player);
                SendReply(player, "Creating a recycler for you.");
                player.GiveItem(item);
            }
        }

        private void RefundRecyclerItem(BasePlayer player)
        {
            var item = ItemManager.CreateByName("workbench1");
            item.skin = recyclerSkinId;
            item.name = "Recycler";
            SendReply(player, "Refunding a recycler for you.");
            player.GiveItem(item);
        }

        private void RemoveSpawnRecord(BasePlayer player)
        {
            if (GetSpawnsFor(player) != null)
            {
                Puts("removing Recycler for " + player.userID);
                var spawns = SpawnRecords.Find(s => s.userId == player.userID);
                if (spawns.spawns.Count == 1)
                {
                    SpawnRecords.Remove(GetSpawnsFor(player));
                }
                else
                {
                    SpawnRecords.Find(s => s.userId == player.userID).spawns.RemoveAt(0);
                }
            }

            SaveData();
        }

        private void RemoveSpawnRecord(SpawnRecord spawnRecord)
        {
            SpawnRecords.Remove(spawnRecord);
            SaveData();
        }


        private void RemoveAllSpawnRecord(BasePlayer player)
        {
            if (GetSpawnsFor(player) != null)
            {
                SpawnRecords.Remove(GetSpawnsFor(player));
            }
            else
            {
                SendReply(player, "you have no spawn records");
            }
        }

        private void RemoveSpawnRecord(BasePlayer player, KeyValuePair<DateTime, Vector3> spawn)
        {
            if (GetSpawnsFor(player) != null)
            {
                Puts("removing Recycler for " + player.userID);
                var spawns = SpawnRecords.Find(s => s.userId == player.userID);
                if (spawns.spawns.Count == 1)
                {
                    SpawnRecords.Remove(GetSpawnsFor(player));
                }
                else
                {
                    SpawnRecords.Find(s => s.userId == player.userID).spawns.Remove(spawn);
                }
            }

            SaveData();
        }

        private void AddSpawnRecord(BasePlayer player)
        {
            Puts("adding Recycler for " + player.userID);

            var spawnRecord = GetSpawnsFor(player);
            if (spawnRecord != null)
            {
                SpawnRecords.Find(s => s.userId == player.userID).spawns
                    .Add(new KeyValuePair<DateTime, Vector3>(DateTime.Now, player.transform.position));
            }
            else
            {
                var spawnList = new List<KeyValuePair<DateTime, Vector3>>
                {
                    new KeyValuePair<DateTime, Vector3>(DateTime.Now, player.transform.position)
                };

                spawnRecord = new SpawnRecord(player.userID, spawnList);
                SpawnRecords.Add(spawnRecord);
            }


            SaveData();
        }

        private void PermissionFailed(BasePlayer player)
        {
            SendReply(player, covalence.FormatText(lang.GetMessage("permission_failed", this)));
        }

        #endregion
    }
}