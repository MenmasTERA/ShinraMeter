﻿using System;
using System.Collections.Generic;
using System.Linq;
using DamageMeter.Skills;
using DamageMeter.Skills.Skill;
using Tera.Game;
using Tera.Game.Messages;
using Skill = DamageMeter.Skills.Skill.Skill;

namespace DamageMeter
{
    public class DamageTracker
    {
        public delegate void CurrentBossChange(Entity entity);

        private static DamageTracker _instance;

        public Dictionary<Entity, EntityInfo> EntitiesStats = new Dictionary<Entity, EntityInfo>();
        public Dictionary<Player, PlayerInfo> UsersStats = new Dictionary<Player, PlayerInfo>();


        private DamageTracker()
        {
        }

        public long TotalDamage(Entity entity, bool timedEncounter) 
        {
            
                if (entity == null)
                {
                    return (from users in UsersStats from skills in users.Value.Dealt.AllSkills from skill in skills.Value select skill.Value.Damage).Sum();

                }
                if (!EntitiesStats.ContainsKey(entity))
                {
                    return 0;
                }

                if (!timedEncounter)
                {
                return (from users in UsersStats from skills in users.Value.Dealt.GetSkills(entity) from skill in skills.Value select skill.Value.Damage).Sum();

            }

            return (from users in UsersStats from skills in users.Value.Dealt.GetSkillsByTime(entity) from skill in skills.Value select skill.Value.Damage).Sum();
            
        }

        public void RegisterDead(SCreatureLife dead)
        {
            var user = (UserEntity)NetworkController.Instance.EntityTracker.GetOrNull(dead.User);
            if (user != null)
            {
                var player = NetworkController.Instance.PlayerTracker.GetOrUpdate(user);
                if (dead.Dead)
                {
                    UsersStats[player].DeathCounter++;
                    Console.WriteLine(player.Name + " is dead");
                }else
                {
                    Console.WriteLine(player.Name + " is alive");
                }
            }
        }

        public long PartyDps(Entity entity, bool timedEncounter)
        {
            var interval = Interval(entity);
            var damage = TotalDamage(entity, timedEncounter);
            if (interval == 0)
            {
                return damage;
            }
            return damage / interval;
        }


     

        public long FirstHit(Entity currentBoss)
        {
           
                long firsthit = 0;
                foreach (var userstat in UsersStats)
                {
                    if (((firsthit == 0) || (userstat.Value.Dealt.FirstHit(currentBoss) < firsthit)) &&
                        userstat.Value.Dealt.FirstHit(currentBoss) != 0)
                    {
                        firsthit = userstat.Value.Dealt.FirstHit(currentBoss);
                    }
                }
                return firsthit;
            
        }

        public long Interval(Entity currentboss) {
            return LastHit(currentboss) - FirstHit(currentboss);
        }

        public long LastHit(Entity currentBoss)
        {
           
                long lasthit = 0;
                foreach (var userstat in UsersStats)
                {
                    if (((lasthit == 0) || (userstat.Value.Dealt.LastHit(currentBoss) > lasthit)) &&
                        userstat.Value.Dealt.LastHit(currentBoss) != 0)
                    {
                        lasthit = userstat.Value.Dealt.LastHit(currentBoss);
                    }
                }
                return lasthit;
            
        }

        public static DamageTracker Instance => _instance ?? (_instance = new DamageTracker());

        public Dictionary<Entity, EntityInfo> GetEntityStats()
        {
            return EntitiesStats.ToDictionary(stats => stats.Key, stats => (EntityInfo) stats.Value.Clone());
        }

        public List<PlayerInfo> GetPlayerStats()
        {
            return UsersStats.Select(stat => (PlayerInfo) stat.Value.Clone()).ToList();
        }

        private List<Entity> _toDelete = new List<Entity>();

        public void UpdateEntities(NpcOccupierResult npcOccupierResult, long time)
        {
            foreach (var entityStats in EntitiesStats)
            {
                var entity = entityStats.Key;
                if (entity.Id != npcOccupierResult.Npc || !entity.IsBoss || !npcOccupierResult.HasReset) continue;

                /*
                * Instead of deleting the entity directly, we store what entity need to be deleted.  
                * With that, we are able to keep data on try run (without that, if you try the queen in DS2, when you wipe, all stats are deleted)
                * Now, with that, the data will be keep until the next try against the boss
                */
                _toDelete.Add(entity);
                return;
            }
        }


        public void DeleteEntity(Entity entity)
        {
            if (NetworkController.Instance.Encounter == entity)
            {
                NetworkController.Instance.NewEncounter = null;
            }

            var add = false;
            var newEntityStat = new EntityInfo();
            if (EntitiesStats.ContainsKey(entity))
            {
                foreach (var abnormality in EntitiesStats[entity].AbnormalityTime)
                {
                   
                    if (!AbnormalityTracker.Instance.AbnormalityExist(entity.Id, abnormality.Key))
                    {
                        continue;
                    }
                    var duration = new AbnormalityDuration(abnormality.Value.InitialPlayerClass);
                    duration.ListDuration.Add(abnormality.Value.ListDuration[abnormality.Value.ListDuration.Count - 1]);
                    newEntityStat.AbnormalityTime.Add(abnormality.Key, duration);
                    add = true;
                }
            }

            EntitiesStats.Remove(entity);
            if (add)
            {
                EntitiesStats.Add(entity, newEntityStat);
            }


            foreach (var stats in UsersStats)
            {
                stats.Value.Dealt.RemoveEntity(entity);
                stats.Value.Received.RemoveEntity(entity);
            }
        }

        public void UpdateCurrentBoss(Entity entity)
        {
            if (entity.IsBoss)
            {
                NetworkController.Instance.NewEncounter = entity;
            }
        }

        public void SetFirstHit(Entity entity, long time)
        {
            if (EntitiesStats[entity].FirstHit == 0)
            {
                EntitiesStats[entity].FirstHit = time;
            }
        }

        public void SetLastHit(Entity entity, long time)
        {
            EntitiesStats[entity].LastHit = time;
        }

        public void Reset()
        {
            var newUserStats = new Dictionary<Player, PlayerInfo>();
            var newEntityStats = new Dictionary<Entity, EntityInfo>();
            bool add;
            foreach (var entity in EntitiesStats)
            {
                add = false;
                var newEntityStat = new EntityInfo();
                foreach (var abnormality in entity.Value.AbnormalityTime)
                {
                    if (abnormality.Value.Ended())
                    {
                        continue;
                    }
                    var duration = new AbnormalityDuration(abnormality.Value.InitialPlayerClass);
                    duration.ListDuration.Add(abnormality.Value.ListDuration[abnormality.Value.ListDuration.Count - 1]);
                    newEntityStat.AbnormalityTime.Add(abnormality.Key, duration);
                    add = true;
                }

                if (add)
                {
                    newEntityStats.Add(entity.Key, newEntityStat);
                }
            }

            foreach (var user in UsersStats)
            {
                add = false;
                var newUserStat = new PlayerInfo(user.Key);

                foreach (var abnormality in user.Value.AbnormalityTime)
                {
                    if (abnormality.Value.Ended())
                    {
                        continue;
                    }
                    var duration = new AbnormalityDuration(abnormality.Value.InitialPlayerClass);
                    duration.ListDuration.Add(abnormality.Value.ListDuration[abnormality.Value.ListDuration.Count - 1]);
                    newUserStat.AbnormalityTime.Add(abnormality.Key, duration);
                    add = true;
                }
                if (add)
                {
                    newUserStats.Add(user.Key, newUserStat);
                }
            }

            UsersStats = newUserStats;
            EntitiesStats = newEntityStats;
            NetworkController.Instance.NewEncounter = null;
           
        }


        private PlayerInfo GetOrCreate(Player player)
        {
            PlayerInfo playerStats;

            if (UsersStats.TryGetValue(player, out playerStats))
            {
                return playerStats;
            }

            playerStats = new PlayerInfo(player);
            UsersStats.Add(player, playerStats);


            return playerStats;
        }


        public Entity GetActorEntity(EntityId entityId)
        {
            var entity = NetworkController.Instance.EntityTracker.GetOrPlaceholder(entityId);
            if (entity is NpcEntity)
            {
                var target = (NpcEntity) entity;
                return new Entity(target);
            }
            if (entity is UserEntity)
            {
                var target = (UserEntity) entity;
                return new Entity(target.Name);
            }

            return null;
        }

        public Entity GetEntity(EntityId entityId)
        {
            var entity = NetworkController.Instance.EntityTracker.GetOrPlaceholder(entityId);
            var entity2 = GetActorEntity(entityId);
            if (entity2 != null)
            {
                return entity2;
            }
            if (entity is ProjectileEntity)
            {
                var source = (ProjectileEntity) entity;
                if (source.Owner is NpcEntity)
                {
                    var source2 = (NpcEntity) source.Owner;
                    return new Entity(source2);
                }
                if (source.Owner is UserEntity)
                {
                    var source2 = (UserEntity) source.Owner;
                    return new Entity(source2.Name);
                }
                return null;
            }
            return null;
        }

        public void Update(SkillResult skillResult, long time)
        {
            PlayerInfo playerStats;
            if (skillResult.SourcePlayer != null)
            {
                playerStats = GetOrCreate(skillResult.SourcePlayer);
                var entityTarget = GetActorEntity(skillResult.Target.Id);
                if (entityTarget == null)
                {
                    throw new Exception("Unknow target" + skillResult.Target.GetType());
                }


                UpdateStatsDealt(playerStats, skillResult, entityTarget, time);
            }

            if (skillResult.TargetPlayer == null) return;
            playerStats = GetOrCreate(skillResult.TargetPlayer);
            var entitySource = new Entity("UNKNOW");
            if (skillResult.Source != null)
            {
                entitySource = GetEntity(skillResult.Source.Id) ?? new Entity("UNKNOW");
            }
            UpdateStatsReceived(playerStats, skillResult, entitySource, time);
        }

        private static bool IsValidAttack(SkillResult message)
        {
            if (message.Amount == 0)
            {
                return false;
            }

            if ((UserEntity.ForEntity(message.Source)["user"] == UserEntity.ForEntity(message.Target)["user"]) &&
                (message.Damage != 0))
            {
                return false;
            }

            return true;
        }

        private void UpdateEncounter(Entity entity, SkillResult msg)
        {
            if (!EntitiesStats.ContainsKey(entity) && msg.Damage != 0)
            {
                EntitiesStats.Add(entity, new EntityInfo());
            }
        }

        private void UpdateStatsDealt(PlayerInfo playerInfo, SkillResult message, Entity entityTarget, long time)
        {
          
            if (!IsValidAttack(message))
            {
                return;
            }

          
            if (_toDelete.Contains(entityTarget))
            {
                DeleteEntity(entityTarget);
                _toDelete.Remove(entityTarget);
            }


            UpdateEncounter(entityTarget, message);
            UpdateSkillStats(message, entityTarget, playerInfo, time);
        }


        private static void UpdateSkillStats(SkillResult message, Entity entityTarget, PlayerInfo playerInfo, long time)
        {
            var entities = playerInfo.Dealt;

            entities.AddStats(time, entityTarget, new SkillsStats(playerInfo));

            var skillName = message.SkillId.ToString();
            if (message.Skill != null)
            {
                skillName = message.Skill.Name;
            }
            var skillKey = new Skill(skillName, new List<int> {message.SkillId});


            SkillStats skillStats;
            var dictionarySkillStats = entities.GetSkills(time, entityTarget);
            dictionarySkillStats.TryGetValue(skillKey, out skillStats);
            if (skillStats == null)
            {
                skillStats = new SkillStats(playerInfo, entityTarget);
            }
            if (message.IsHp)
            {
                if (message.Amount > 0)
                {
                    skillStats.AddData(message.SkillId, message.Heal, message.IsCritical, SkillStats.Type.Heal, time);
                }
                else
                {
                    skillStats.AddData(message.SkillId, message.Damage, message.IsCritical, SkillStats.Type.Damage, time);
                }
            }
            else
            {
                skillStats.AddData(message.SkillId, message.Mana, message.IsCritical, SkillStats.Type.Mana, time);
            }

            var skill = dictionarySkillStats.Keys.ToList();
            var indexSkill = skill.IndexOf(skillKey);
            if (indexSkill != -1)
            {
                foreach (
                    var skillid in skill[indexSkill].SkillId.Where(skillid => !skillKey.SkillId.Contains(skillid)))
                {
                    skillKey.SkillId.Add(skillid);
                }
            }
            dictionarySkillStats.Remove(skillKey);
            dictionarySkillStats.Add(skillKey, skillStats);
        }

        private void UpdateStatsReceived(PlayerInfo playerInfo, SkillResult message, Entity entitySource, long time)
        {
          
            if (!IsValidAttack(message))
            {
                return;
            }

            //Not damage & if you are a healer, don't show heal / mana regen affecting you, as that will modify your crit rate and other stats. 
            if ((message.IsHp && message.Amount > 0 || !message.IsHp) && !playerInfo.IsHealer &&
                (UserEntity.ForEntity(message.Source)["user"] != UserEntity.ForEntity(message.Target)["user"]))
            {
                UpdateSkillStats(message, new Entity(playerInfo.Player.User.Name), playerInfo, time);
                return;
            }

            if (message.Damage <= 0)
            {
                return;
            }

            var entity = entitySource;

            if (entitySource.IsBoss)
            {
                foreach (
                    var t in EntitiesStats.Where(t => t.Key.Name == entitySource.Name && t.Key.Id == entitySource.Id))
                {
                    entity = t.Key;
                    break;
                }

                //Don't count damage taken if boss haven't taken any damage
                if (entity == null)
                {
                    return;
                }
            }

            playerInfo.Received.AddStats(time, entity, message.Damage);
            if (EntitiesStats.ContainsKey(entity)) return;
            EntitiesStats.Add(entity, new EntityInfo());
        }
    }
}