﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.Mvc;

using BossInformation = VF_RaidDamageDatabase.BossInformation;
using FightCacheData = VF_RaidDamageDatabase.FightDataCollection.FightCacheData;
using RaidCollection = VF_RaidDamageDatabase.RaidCollection;
using RaidBossFight = VF_RaidDamageDatabase.RaidBossFight;

namespace VF_RaidDamageWebsite
{
    public partial class BossList : System.Web.UI.Page
    {
        public MvcHtmlString m_BreadCrumbHTML = null;
        public MvcHtmlString m_BossListInfoHTML = null;

        public MvcHtmlString m_TableHeadHTML = null;
        public MvcHtmlString m_TableBodyHTML = null;

        protected void Page_Load(object sender, EventArgs e)
        {
            string guildLimit = PageUtility.GetQueryString(Request, "Guild", "null");

            var realm = RealmControl.Realm;

            string breadCrumbCommon = "";
            if (guildLimit != "null")
            {
                this.Title = "Boss records for " + guildLimit + " | RaidStats";

                breadCrumbCommon = PageUtility.BreadCrumb_AddHome()
                    + PageUtility.BreadCrumb_AddRealm(realm)
                    + PageUtility.BreadCrumb_AddLink("RaidList.aspx?Guild=" + guildLimit, guildLimit);

                m_BossListInfoHTML = new MvcHtmlString("<h1>Raid Bosses for " + guildLimit + "</h1>"
                    + "<p>Top DPS, HPS and fastest kill times are listed below for the different bosses</p>"
                    + "<p>Note that this does only show data from the guild " + guildLimit + ".<br />If you want to see for all guilds click " + PageUtility.CreateLink("BossList.aspx?Realm=" + RealmPlayersServer.StaticValues.ConvertRealmParam(realm), "here") + "</p>");
            }
            else
            {
                this.Title = "Boss records | RaidStats";

                breadCrumbCommon = PageUtility.BreadCrumb_AddHome()
                    + PageUtility.BreadCrumb_AddRealm(realm);

                m_BossListInfoHTML = new MvcHtmlString("<h1>Raid Bosses</h1>"
                    + "<p>Top DPS, HPS and fastest kill times are listed below for the different bosses</p>");
            }

            if (ClassControl.HasClassLimits() == true)
            {
                string colorClasses = ClassControl.GetColorClassesStr();
                string breadCrumb = breadCrumbCommon + PageUtility.BreadCrumb_AddThisPageWithout("Bosses", Request, "ClassLimit");

                if(ClassControl.HasFactionLimits() == true)
                {
                    breadCrumb += PageUtility.BreadCrumb_AddFinish("vs " + colorClasses);
                    breadCrumb += PageUtility.BreadCrumb_AddFinish("Only " + ClassControl.GetColorFactionStr());
                }
                else
                {
                    breadCrumb += PageUtility.BreadCrumb_AddFinish("vs " + colorClasses);
                }
                m_BreadCrumbHTML = new MvcHtmlString(breadCrumb);
            }
            else
            {
                if (ClassControl.HasFactionLimits() == true)
                {
                    m_BreadCrumbHTML = new MvcHtmlString(breadCrumbCommon
                        + PageUtility.BreadCrumb_AddThisPageWithout("Bosses", Request, "FactionLimit")
                        + PageUtility.BreadCrumb_AddFinish("Only " + ClassControl.GetColorFactionStr()));
                }
                else
                { 
                    m_BreadCrumbHTML = new MvcHtmlString(breadCrumbCommon
                        + PageUtility.BreadCrumb_AddFinish("Bosses"));
                }
            }

            m_TableHeadHTML = new MvcHtmlString(
                PageUtility.CreateTableRow("",
                PageUtility.CreateTableColumnHead("Instance") +
                PageUtility.CreateTableColumnHead("Boss") +
                PageUtility.CreateTableColumnHead("Top DPS") +
                PageUtility.CreateTableColumnHead("Top HPS") +
                PageUtility.CreateTableColumnHead("Fastest Kill") +
                PageUtility.CreateTableColumnHead("Kill Count")));

            DateTime earliestCompatibleDate = new DateTime(2013, 10, 23, 0, 0, 0);
            var realmDB = ApplicationInstance.Instance.GetRealmDB(VF_RealmPlayersDatabase.WowRealm.Emerald_Dream);
            //var raidCollection = ApplicationInstance.Instance.GetRaidCollection();

            var summaryDatabase = ApplicationInstance.Instance.GetSummaryDatabase();
            if (summaryDatabase == null)
                return;

            var classLimits = ClassControl.GetClassLimits();
            var factionLimits = ClassControl.GetFactionLimits();

            string tableBody = "";
            foreach (var boss in BossInformation.BossFights)
            {
                List<VF_RDDatabase.BossFight> bossFights = new List<VF_RDDatabase.BossFight>();
                double averagePrecision = 0.0;
                foreach (var groupRC in summaryDatabase.GroupRCs)
                {
                    if (realm != VF_RealmPlayersDatabase.WowRealm.All && realm != groupRC.Value.Realm)
                        continue;

                    if (guildLimit != "null" && guildLimit != groupRC.Value.GroupName)
                        continue;
                    foreach (var raid in groupRC.Value.Raids)
                    {
                        if (raid.Value.RaidInstance == boss.Value)
                        {
                            //if (realmDB.Realm != raid.Value.CacheGroup.Realm)
                            //    realmDB = ApplicationInstance.Instance.GetRealmDB(raid.Value.CacheGroup.Realm);
                            //var fights = raid.Value.BossFightsGetBossFights(ApplicationInstance.Instance.GetRaidFightCollection);
                            foreach (var bossFight in raid.Value.BossFights)
                            {
                                if (bossFight.BossName == boss.Key
                                && bossFight.StartDateTime > earliestCompatibleDate
                                && bossFight.IsQualityHigh())
                                {
                                    averagePrecision += bossFight.DataDetails.FightPrecision;// fight.CalculatePrecision(realmDB.RD_IsPlayer);
                                    bossFights.Add(bossFight);
                                }
                            }
                        }
                    }
                }
                if (bossFights.Count > 0)
                {
                    averagePrecision /= bossFights.Count;
                    Tuple<string, int> fastestKill = new Tuple<string, int>("", int.MaxValue);
                    Tuple<string, double> topDPS = new Tuple<string, double>("", 0.0);
                    Tuple<string, double> topHPS = new Tuple<string, double>("", 0.0);
                    foreach (var fight in bossFights)
                    {
                        if (realmDB.Realm != fight.CacheRaid.CacheGroup.Realm)
                            realmDB = ApplicationInstance.Instance.GetRealmDB(fight.CacheRaid.CacheGroup.Realm);
                        if (fight.DataDetails.FightPrecision < averagePrecision - 0.05)
                            continue;
                        if (fight.FightDuration < fastestKill.Item2)
                        {
                            fastestKill = new Tuple<string,int>(
                                PageUtility.CreateLink("FightOverview.aspx?Raid=" + fight.CacheRaid.UniqueRaidID
                                    + "&Fight=" + fight.StartDateTime.ToString("ddHHmmss"), fight.FightDuration.ToString() + " seconds")
                                , fight.FightDuration);
                        }

                        var unitsData = fight.PlayerFightData;//.GetFilteredPlayerUnitsData(true, realmDB.RD_GetPlayerIdentifier);
                        Tuple<string, int> topDmg = new Tuple<string,int>("", 0);
                        Tuple<string, int> topHeal = new Tuple<string, int>("", 0);
                        foreach (var unit in unitsData)
                        {
                            if (unit.Item2.Damage > topDmg.Item2)
                            {
                                var playerName = unit.Item1;
                                if (playerName == "Unknown")
                                    continue;
                                if (BossInformation.BossFights.ContainsKey(playerName) == true)
                                    continue;

                                var playerData = realmDB.RD_FindPlayer(playerName, fight);
                                if (playerData != null && (classLimits == null || classLimits.Contains(playerData.Character.Class))
                                    && (factionLimits == null || factionLimits.Contains(StaticValues.GetFaction(playerData.Character.Race))))
                                    topDmg = new Tuple<string, int>(unit.Item1, unit.Item2.Damage);
                            }
                            if (unit.Item2.EffectiveHeal > topHeal.Item2)
                            {
                                var playerName = unit.Item1;
                                if (playerName == "Unknown")
                                    continue;
                                if (BossInformation.BossFights.ContainsKey(playerName) == true)
                                    continue;

                                var playerData = realmDB.RD_FindPlayer(playerName, fight);
                                if (playerData != null && (classLimits == null || classLimits.Contains(playerData.Character.Class))
                                    && (factionLimits == null || factionLimits.Contains(StaticValues.GetFaction(playerData.Character.Race))))
                                    topHeal = new Tuple<string, int>(unit.Item1, unit.Item2.EffectiveHeal);
                            }
                        }
                        double topThisDPS = (double)topDmg.Item2 / (double)fight.FightDuration;
                        double topThisHPS = (double)topHeal.Item2 / (double)fight.FightDuration;
                        if (topThisDPS > topDPS.Item2)
                            topDPS = new Tuple<string, double>(PageUtility.CreateLink_RaidStats_Player(topDmg.Item1, realmDB.Realm
                                , PageUtility.CreateColorCodedName(topDmg.Item1, realmDB.GetPlayer(topDmg.Item1).Character.Class))
                                + "(" + PageUtility.CreateLink("FightOverview.aspx?Raid=" + fight.CacheRaid.UniqueRaidID
                                    + "&Fight=" + fight.StartDateTime.ToString("ddHHmmss")
                                , topThisDPS.ToStringDot("0.0")) + ")", topThisDPS);
                        if (topThisHPS > topHPS.Item2)
                            topHPS = new Tuple<string, double>(PageUtility.CreateLink_RaidStats_Player(topHeal.Item1, realmDB.Realm
                                , PageUtility.CreateColorCodedName(topHeal.Item1, realmDB.GetPlayer(topHeal.Item1).Character.Class))
                                + "(" + PageUtility.CreateLink("FightOverview.aspx?Raid=" + fight.CacheRaid.UniqueRaidID
                                    + "&Fight=" + fight.StartDateTime.ToString("ddHHmmss")
                                , topThisHPS.ToStringDot("0.0")) + ")", topThisHPS);

                    }
                    int killCount = bossFights.Count((_Value) => { return _Value.AttemptType == VF_RDDatabase.AttemptType.KillAttempt; });

                    tableBody += PageUtility.CreateTableRow("", PageUtility.CreateTableColumn(PageUtility.CreateImage(StaticValues._RaidInstanceImages[boss.Value]) + boss.Value) +
                        PageUtility.CreateTableColumn(PageUtility.CreateLink("FightOverallOverview.aspx?FightName=" + boss.Key + (guildLimit != "null" ? "&Guild=" + guildLimit : ""), boss.Key)) +
                        PageUtility.CreateTableColumn(topDPS.Item1) +
                        PageUtility.CreateTableColumn(topHPS.Item1) +
                        PageUtility.CreateTableColumn(fastestKill.Item1) +
                        PageUtility.CreateTableColumn(killCount.ToString()));
                }
            }
            m_TableBodyHTML = new MvcHtmlString(tableBody);
        }
    }
}