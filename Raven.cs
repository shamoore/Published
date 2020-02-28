using Oxide.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Physics = UnityEngine.Physics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Ext.Discord.DiscordObjects;
using Oxide.Ext.Discord.Attributes;
using Oxide.Ext.Discord;
using Oxide.Ext.Discord.DiscordEvents;
using Rust;
using Component = UnityEngine.Component;

namespace Oxide.Plugins
{
    [Info("Raven", "Shawhiz", ".01")]
    [Description("Discord plugin that notifies players on events in game")]
    internal class Raven : CovalencePlugin
    {
        [PluginReference] private Plugin DiscordCore;
        private bool _init = false;

        private List<KeyValuePair<string, string>> messageQue = new List<KeyValuePair<string, string>>();
        
        private Dictionary<string, string> prettyNames = new Dictionary<string, string>()
        {
            {"ANDSwitch", "ANDSwitch"	},
            {"AudioAlarm", "Audio Alarm"},
            {"autoturret", "Auto Turret"},
            {"barricade.concrete", "Concrete Barricade"},
            {"barricade.metal", "Metal Barricade"},
            {"barricade.sandbags", "Sandbags"},
            {"barricade.stone", "Stone Barricade"},
            {"barricade.wood", "Wood Barricade"},
            {"barricade.woodwire", "Wire Barricade"},
            {"bbq", "BBQ"},
            {"beartrap", "Bear Trap"},
            {"bed", "Bed"},
            {"boomer.blue	", "Fireworks"},
            {"boomer.champagne", "Fireworks"},
            {"boomer.green", "Fireworks"},
            {"boomer.orange", "Fireworks"},
            {"boomer.red", "Fireworks"},
            {"boomer.violet", "Fireworks"},
            {"box_wooden", "Small Wooden Box"},
            {"box.wooden.large", "Large Wooden Box"},
            {"CableTunnel", "Cable Tunnel"},
            {"campfire", "Campfire"},
            {"cauldron", "Cauldron"},
            {"ceilinglight", "Ceiling Light"},
            {"chair", "Chair"},
            {"chineselantern", "Chinese Lantern"},
            {"chippyarcademachine", "Arcade Machine"},
            {"christmas_door_wreath", "Christmas Wreath"},
            {"coffinstorage", "Coffin"},
            {"corn_clone", "Corn"},
            {"corn_seed", "Corn"},
            {"counter", "Counter"},
            {"cupboard.tool", "Tool Cupboard"},
            {"door.double.hinged.metal", "Double Sheet Metal Door"},
            {"door.double.hinged.toptier", "Double Armored Door"},
            {"door.double.hinged.wood", "Double Wood Door"},
            {"door.hinged.metal", "Sheet Metal Door"},
            {"door.hinged.toptier", "Armored Door"},
            {"door.hinged.wood", "Wood Door"},
            {"doorcloser", "Door Closer"},
            {"doorcontroller", "Door Controller"},
            {"doorgarland", "Door Garland"},
            {"dragondoorknocker","Dragon Door Knocker"},
            {"dropbox", "Dropbox"},
            {"drumkit", "Drumkit"},
            {"easter_door_wreath", "Easter Wreath"},
            {"electric.flasherlight", "Flasher Light"},
            {"electric.sirenlight", "Siren LIght"},
            {"electrical.blocker", "Blocker"},
            {"electrical.branch", "Branch"},
            {"electrical.combiner", "Combiner"},
            {"electrical.memorycell", "Memory Cell"},
            {"electrical.random.switch", "Random Switch"},
            {"fireplace", "Fireplace"},
            {"flameturret", "Flame Turret"},
            {"floor.grill", "Floor Grill"},
            {"floor.ladder.hatch", "Ladder Hatch"},
            {"fogmachine", "Fog Machine"},
            {"fridge", "Fridge"},
            {"furnace", "Furnace"},
            {"furnace.large", "Large Furnace"},
            {"gates.external.high.stone", "High External Stone Gate"},
            {"gates.external.high.wood", "High External Wood Gate"},
            {"generator.small", "Small Generator"},
            {"generator.wind.scrap", "Windmill"},
            {"GiantCandyCane", "Giant Candy Cane"},
            {"Giantlollipops", "Giant Lollipop"},
            {"gravestone.stone", "Stone Gravestone"},
            {"gravestone.wood", "Wood Gravestone"},
            {"graveyardfence", "Graveyard Fence"},
            {"hbhfsensor", "HBHF Sensor"},
            {"hemp_clone", "Hemp "},
            {"hemp_seed", "Hemp Seed"},
            {"HitchTrough", "Hitch Trough"},
            {"igniter	", "Igniter"},
            {"jackolantern.angry", "Jackolantern Angry"},
            {"jackolantern.happy", "Jackolantern Happy"},
            {"ladder.wooden.wall", "Ladder"},
            {"landmine", "Landmine"},
            {"lantern", "Lantern"},
            {"large.rechargable.battery", "Large Recharchable Battery"},
            {"largecandles", "Candles"},
            {"LaserDetector", "Laser Detector"},
            {"lock.code", "Code Lock"},
            {"lock.key", "Lock"},
            {"locker", "Locker"},
            {"mailbox", "Mailbox"},
            {"medium.rechargable.battery", "Medium Rechargable Battery"},
            {"mining.pumpjack", "Pumpjack"},
            {"mining.quarry", "Quarry"},
            {"newyeargong", "New YearS Gong"},
            {"ORSwitch", "Or Switch"},
            {"piano", "Piano"},
            {"planter.large", "Planter (large)"},
            {"planter.small", "Planter (small)"},
            {"pookie", "Pookiebear"},
            {"pressurepad", "Pressure Pad"},
            {"pumpkin_clone", "Pumpkin"},
            {"	pumpkin_seed", "Pumpkin Seed"},
            {"reactivetarget", "Reactive Target"},
            {"recycler_static", "Recycler"},
            {"repairbench", "Repair Bench"},
            {"researchtable", "Research Table"},
            {"RFBroadcaster", "RFBroadcaster"},
            {"RFReceiver", "RFReceiver"},
            {"romancandle.blue", "Fireworks"},
            {"romancandle.green", "Fireworks"},
            {"romancandle.red", "Fireworks"},
            {"romancandle.violet", "Fireworks"},
            {"rug", "Rug"},
            {"rug.bear", "Bear Rug"},
            {"rustige_egg_a", "Egg"},
            {"rustige_egg_b", "Egg"},
            {"sam.site", "Sam Site"},
            {"scarecrow	", "Scarecrow"},
            {"searchlight", "Search Light"},
            {"shelves", "Shelves"},
            {"guntrap", "Shotgun Trap"},
            {"shutter.metal.embrasure.a", "Metal Embrasure"},
            {"shutter.metal.embrasure.b", "Metal Embrasure"},
            {"shutter.wood.a", "Wood Shutter"},
            {"sign.hanging", "Sign"},
            {"sign.hanging.banner.large", "Sign"},
            {"sign.hanging.ornate", "Sign"},
            {"sign.pictureframe.landscape", "Sign"},
            {"sign.pictureframe.portrait", "Sign"},
            {"sign.pictureframe.tall", "Sign"},
            {"sign.pictureframe.xl", "Sign"},
            {"sign.pictureframe.xxl", "Sign"},
            {"sign.pole.banner.large", "Sign"},
            {"sign.post.double", "Sign"},
            {"sign.post.single", "Sign"},
            {"sign.post.town", "Sign"},
            {"sign.post.town.roof", "Sign"},
            {"sign.wooden.huge", "Sign"},
            {"sign.wooden.large", "Sign"},
            {"sign.wooden.medium", "Sign"},
            {"sign.wooden.small", "Sign"},
            {"simplelight", "Simple Light"},
            {"skull_door_knocker", "Skull Door Knocker"},
            {"skull_fire_pit", "Skull Firepit"},
            {"sleepingbag", "Sleeping Bag"},
            {"small_fuel_generator", "Small Fuel Generator"},
            {"small_oil_refinery", "Small Oil Refinery"},
            {"small_stash", "Small Stash"},
            {"small.rechargable.battery", "Small Rechargable Battery"},
            {"smallcandles", "Candles"},
            {"snowmachine", "Snow Machine"},
            {"snowman", "Snowman"},
            {"solarpanel.large", "Solar Panel (Large)"},
            {"SpiderWeb", "SpiderWeb"},
            {"spikes.floor", "Floor Spikes"},
            {"spinner.wheel", "Spinner Wheel"},
            {"splitter", "Splitter"},
            {"spookyspeaker", "Spooky Speaker"},
            {"stocking.large", "Stocking (Large)"},
            {"stocking.small", "Stocking (Small)"},
            {"strobelight", "Strobe Light"},
            {"survivalfishtrap", "Survival Fish Trap"},
            {"switch", "Switch"},
            {"table", "Table"},
            {"teslacoil", "Tesla Coil"},
            {"timer", "Timer"},
            {"tunalight", "Tuna Can Light"},
            {"vendingmachine", "Vending Machine"},
            {"volcanofirework", "Fireworks"},
            {"volcanofirework.red", "Fireworks"},
            {"volcanofirework.violet", "Fireworks"},
            {"wall.external.high.stone", "High External Stone Wall"},
            {"wall.external.high.wood", "High External Wood Wall"},
            {"wall.frame.cell", "Cell Wall"},
            {"wall.frame.cell.gate", "Cell Gate"},
            {"wall.frame.fence", "Fence Wall"},
            {"wall.frame.fence.gate", "Fence Gate"},
            {"wall.frame.garagedoor", "Garage Door"},
            {"wall.frame.netting", "Wall Frame (Netting)"},
            {"wall.frame.shopfront", "Shopfront Wood"},
            {"wall.frame.shopfront.metal", "Shopfront Metal"},
            {"wall.window.bars.metal", "Window Bars Metal"},
            {"wall.window.bars.toptier", "Window Bars Armored"},
            {"wall.window.bars.wood", "Window Bars Wood"},
            {"wall.window.glass.reinforced", "Window Bars Glass Reinforced"},
            {"watchtower.wood", "Watchtower"},
            {"water_catcher_large", "Water Catcher (Large)"},
            {"water_catcher_small", "Water Catcher (Small)"},
            {"waterbarrel", "Water Barrel"},
            {"waterpurifier", "Water Purifier"},
            {"windowgarland", "Window Garland"},
            {"workbench1", "Workbench 1"},
            {"workbench2", "Workbench 2"},
            {"workbench3", "Workbench 3"},
            {"xmas_tree", "Christmas Tree"},
            {"xmas.lightstring", "Christmas Lights"},
            {"XORSwitch", "XORSwitch"},
            {"xylophone", "Xylophone"}
        };


        private void OnServerInitialized()
        {
            if (DiscordCore == null)
            {
                PrintError("Missing plugin dependency DiscordCore: https://umod.org/plugins/discord-core");
                return;
            }

            OnDiscordCoreReady();
        }
        private void OnDiscordCoreReady()
        {
            if (!(DiscordCore?.Call<bool>("IsReady") ?? false))
            {
                return;
            }

            _init = true;
        }

        private string GetUsername(string playerID)
        {
            return covalence.Players.FindPlayerById(playerID)?.Name;

        }

        public static string FormatGridReference(Vector3 position) // Credit: Jake_Rich
        {
            Vector2 roundedPos = new Vector2(World.Size / 2 + position.x, World.Size / 2 - position.z);
            string grid = $"{NumberToLetter((int) (roundedPos.x / 150))}{(int) (roundedPos.y / 150)}";

            return grid;
        }

        public static string NumberToLetter(int num) // Credit: Jake_Rich
        {
            int num2 = Mathf.FloorToInt((float) (num / 26));
            int num3 = num % 26;
            string text = string.Empty;
            if (num2 > 0)
            {
                for (int i = 0; i < num2; i++)
                {
                    text += Convert.ToChar(65 + i);
                }
            }

            return text + Convert.ToChar(65 + num3).ToString();
        }


        public string GetWeaponInfo(HitInfo hitInfo)
        {
            string weaponInfo = "";
             if (hitInfo.damageTypes != null)
             {
                 var damageType = hitInfo.damageTypes.GetMajorityDamageType();
                 switch (damageType)
                 {
                     case DamageType.Explosion:
                     {
                         weaponInfo = " with Explosives ";
                         break;
                     }

                     case DamageType.Bullet:
                     {
                         weaponInfo = "with something that's not hurting much..yet";
                         break;
                     }

                     case DamageType.AntiVehicle:
                     {
                         if (hitInfo.InitiatorPlayer.GetHeldEntity() !=null && hitInfo.InitiatorPlayer.GetHeldEntity().ShortPrefabName.Equals("rocket_launcher.entity"))
                         {
                             weaponInfo = "with an incendiary rocket ";
                         }
                         else
                         {
                             weaponInfo = "with something that's not hurting much..yet ";
                         }

                         break;
                          
                     }
                     case DamageType.Blunt:
                     {
                         weaponInfo = "with a blunt object ";
                         break;
                          
                     }
                     case DamageType.Stab:
                     {
                         weaponInfo = "with a stabbing weapon ";
                         break;
                          
                     }
                     case DamageType.Slash:
                     {
                         weaponInfo = "with a slashing weapon ";
                         break;
                          
                     }

                     default:
                     {
                         weaponInfo = "";
                         break;
                     }
                 }
             }

             return weaponInfo;
        }
        public string GetItemName(BaseEntity entity)
        {
            
            var itemname = entity.ShortPrefabName;
            if (entity is BuildingBlock)
            {
                var buildingBlock = entity as BuildingBlock;
                var grade = buildingBlock.grade.ToString();
                if (grade.Equals("TopTier")) grade = "High Quality Armored";
                if (grade.Equals("Twigs")) grade = "Twig";
                var type = "";
                
                if (buildingBlock.name.Contains("foundation")) type = "Foundation";
                else if (buildingBlock.name.Contains("stairs")) type = "Stairs";
                else if (buildingBlock.name.Contains("doorway")) type = "Doorway";
                else if (buildingBlock.name.Contains("halfwall")) type = "Halfwall";
                else if (buildingBlock.name.Contains("roof")) type = "Roof";
                else if (buildingBlock.name.Contains("floor")) type = "Floor";
                else if (buildingBlock.name.Contains("wall")) type = "Wall";
                else type = buildingBlock.name;

                itemname = grade + " " + type;
            }
            else
            {
                foreach (var name in prettyNames)
                {
                    if (entity.ShortPrefabName.StartsWith(name.Key))
                    {
                        itemname = name.Value;
                    }
                }
            }

            return itemname;
        }

        
        
        void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitInfo)
        {
          
            if (entity.OwnerID.IsSteamId() && entity.SecondsSinceAttacked > 5 && hitInfo.InitiatorPlayer != null && hitInfo.InitiatorPlayer.userID != entity.OwnerID  && !hitInfo.InitiatorPlayer.IsBuildingAuthed()
)
            {
                var message = (GetUsername(hitInfo.InitiatorPlayer.UserIDString) + " is attacking your " +
                               GetItemName(entity) + " " +GetWeaponInfo(hitInfo)+ " at " + FormatGridReference(entity.ServerPosition) );

                messageQue.Add(new KeyValuePair<string, string>(entity.OwnerID.ToString(), message));
                
            }
        }
        void OnTick()
        {
            //to Prevent spam messages -- only send 1 per tick.
            if (messageQue.Count > 0)
            {
                var tempQue = new List<KeyValuePair<string, string>>();
                tempQue.AddRange(messageQue);
                var sentQue = new List<string>();
                foreach (var message in tempQue)
                {
                    if (!sentQue.Contains(message.Key))
                    {
                        Puts( GetUsername(message.Key) + ": " + message.Value);
                      DiscordCore.Call("SendMessageToUser", message.Key, message.Value );
                        sentQue.Add(message.Key);
                        messageQue.RemoveAll(m => m.Key == message.Key);
                    }
                }



                ;
                


            }
        }

        
   
    }
}
