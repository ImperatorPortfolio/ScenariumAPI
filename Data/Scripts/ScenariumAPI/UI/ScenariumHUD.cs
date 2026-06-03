using System;
using System.Text;
using VRageMath;

using RichHudFramework.Client;
using RichHudFramework.UI;
using RichHudFramework.UI.Client;
using RichHudFramework.UI.Rendering;

namespace ScenariumAPI
{
    public class ScenariumHUD
    {
        readonly ScenariumSaveData _data;
        readonly Action<string> _addEvent;
        readonly Action _save;

        TexturedBox _root;
        TexturedBox _titleBar;
        TexturedBox _tabBar;
        TexturedBox _sidePanel;
        TexturedBox _mainPanel;
        TexturedBox _statusBar;

        Label _title;
        Label _sideTitle;
        Label _mainTitle;
        Label _mainBody;
        Label _status;

        LabelBoxButton _tabScenario;
        LabelBoxButton _tabQuests;
        LabelBoxButton _tabFactions;
        LabelBoxButton _tabIntel;

        LabelBoxButton _sideOverview;
        LabelBoxButton _sidePrimary;
        LabelBoxButton _sideSecondary;
        LabelBoxButton _sideTertiary;

        LabelBoxButton _closeButton;

        bool _created;
        int _lastHash;

        public ScenariumHUD(ScenariumSaveData data, Action<string> addEvent, Action save)
        {
            _data = data;
            _addEvent = addEvent;
            _save = save;
        }

        public void Create()
        {
            if (_created)
                return;

            RichHudClient.Init("ScenariumAPI", HudInit, ClientReset);
        }

        void HudInit()
        {
            _root = new TexturedBox(HudMain.HighDpiRoot);
            _root.Size = new Vector2(1180f, 760f);
            _root.Offset = new Vector2(0f, 0f);
            _root.Color = new Color(4, 9, 15, 238);
            _root.Visible = _data.PanelVisible;
            _root.ZOffset = 10;

            _titleBar = Box(_root, new Vector2(1180f, 64f), new Vector2(0f, 348f), new Color(0, 40, 58, 250), 11);
            _tabBar = Box(_root, new Vector2(1180f, 56f), new Vector2(0f, 288f), new Color(7, 18, 29, 246), 11);
            _sidePanel = Box(_root, new Vector2(310f, 585f), new Vector2(-415f, -28f), new Color(8, 14, 22, 245), 11);
            _mainPanel = Box(_root, new Vector2(810f, 585f), new Vector2(170f, -28f), new Color(10, 17, 27, 245), 11);
            _statusBar = Box(_root, new Vector2(1180f, 48f), new Vector2(0f, -356f), new Color(0, 35, 48, 240), 11);

            _title = Label(_titleBar, "SCENARIO", new Vector2(-515f, 0f), new Vector2(360f, 44f), new Color(0, 220, 255, 255), 1.25f, TextAlignment.Left, 12);
            _closeButton = Button(_titleBar, "X", new Vector2(548f, 0f), new Vector2(42f, 38f), new Color(75, 15, 20, 255), new Color(140, 35, 45, 255), 12);
            _closeButton.MouseInput.LeftClicked += delegate { Close(); _addEvent("Scenarium panel closed."); _save(); };

            _tabScenario = TabButton("Scenario", -420f, "SCENARIO");
            _tabQuests = TabButton("Quests", -145f, "QUESTS");
            _tabFactions = TabButton("Faction States", 145f, "FACTIONS");
            _tabIntel = TabButton("Intel Log", 420f, "INTEL");

            _sideTitle = Label(_sidePanel, "OVERVIEW", new Vector2(0f, 252f), new Vector2(270f, 34f), new Color(0, 220, 255, 255), 0.8f, TextAlignment.Center, 12);
            _sideOverview = SideButton("Overview", 190f, "OVERVIEW");
            _sidePrimary = SideButton("Active Objective", 130f, "PRIMARY");
            _sideSecondary = SideButton("Progression", 70f, "SECONDARY");
            _sideTertiary = SideButton("Details", 10f, "TERTIARY");

            _mainTitle = Label(_mainPanel, "DETAIL", new Vector2(0f, 252f), new Vector2(760f, 34f), new Color(0, 220, 255, 255), 0.85f, TextAlignment.Left, 12);
            _mainBody = Label(_mainPanel, "", new Vector2(0f, -28f), new Vector2(760f, 500f), new Color(235, 240, 245, 255), 0.72f, TextAlignment.Left, 12);
            _mainBody.AutoResize = false;
            _mainBody.VertCenterText = false;
            _mainBody.BuilderMode = TextBuilderModes.Wrapped;
            _mainBody.LineWrapWidth = 740f;

            _status = Label(_statusBar, "Shift+Q Open/Close | Click tabs and sidebar items", new Vector2(0f, 0f), new Vector2(1120f, 32f), new Color(185, 210, 225, 255), 0.62f, TextAlignment.Left, 12);

            _created = true;
            HudMain.EnableCursor = _data.PanelVisible;
            Refresh(true);
        }

        TexturedBox Box(HudParentBase parent, Vector2 size, Vector2 offset, Color color, int z)
        {
            TexturedBox box = new TexturedBox(parent);
            box.Size = size;
            box.Offset = offset;
            box.Color = color;
            box.ZOffset = (sbyte)z;
            box.Visible = _data.PanelVisible;
            return box;
        }

        Label Label(HudParentBase parent, string text, Vector2 offset, Vector2 size, Color color, float scale, TextAlignment align, int z)
        {
            Label label = new Label(parent);
            label.Text = text;
            label.Offset = offset;
            label.Size = size;
            label.AutoResize = false;
            label.VertCenterText = true;
            label.BuilderMode = TextBuilderModes.Wrapped;
            label.LineWrapWidth = size.X - 20f;
            label.TextBoard.Scale = scale;
            label.TextBoard.SetFormatting(new GlyphFormat(color, align, scale));
            label.ZOffset = (sbyte)z;
            label.Visible = _data.PanelVisible;
            return label;
        }

        LabelBoxButton Button(HudParentBase parent, string text, Vector2 offset, Vector2 size, Color color, Color highlight, int z)
        {
            LabelBoxButton button = new LabelBoxButton(parent);
            button.Text = text;
            button.Offset = offset;
            button.Size = size;
            button.Color = color;
            button.HighlightColor = highlight;
            button.TextBoard.Scale = 0.68f;
            button.TextBoard.SetFormatting(new GlyphFormat(new Color(235, 245, 255, 255), TextAlignment.Center, 0.68f));
            button.AutoResize = false;
            button.VertCenterText = true;
            button.MouseInput.RequestCursor = true;
            button.ZOffset = (sbyte)z;
            button.Visible = _data.PanelVisible;
            return button;
        }

        LabelBoxButton TabButton(string text, float x, string tab)
        {
            LabelBoxButton button = Button(_tabBar, text, new Vector2(x, 0f), new Vector2(250f, 38f), TabColor(tab), new Color(0, 85, 115, 255), 12);
            button.MouseInput.LeftClicked += delegate
            {
                _data.PanelTab = tab;
                _data.SelectedItemId = "OVERVIEW";
                _addEvent("Opened " + text + " tab.");
                Refresh(true);
                _save();
            };
            return button;
        }

        LabelBoxButton SideButton(string text, float y, string id)
        {
            LabelBoxButton button = Button(_sidePanel, text, new Vector2(0f, y), new Vector2(260f, 42f), new Color(16, 35, 48, 255), new Color(0, 80, 110, 255), 12);
            button.MouseInput.LeftClicked += delegate
            {
                _data.SelectedItemId = id;
                Refresh(true);
                _save();
            };
            return button;
        }

        Color TabColor(string tab)
        {
            return _data.PanelTab == tab ? new Color(0, 80, 110, 255) : new Color(15, 30, 42, 255);
        }

        void ClientReset()
        {
            _created = false;
            _root = null;
            _titleBar = null;
            _tabBar = null;
            _sidePanel = null;
            _mainPanel = null;
            _statusBar = null;
            _title = null;
            _sideTitle = null;
            _mainTitle = null;
            _mainBody = null;
            _status = null;
        }

        public void Open()
        {
            _data.PanelVisible = true;
            SetVisible(true);
            HudMain.EnableCursor = true;
            Refresh(true);
        }

        public void Close()
        {
            _data.PanelVisible = false;
            HudMain.EnableCursor = false;
            SetVisible(false);
        }

        public void Refresh(bool force = false)
        {
            if (!_created || _mainBody == null)
                return;

            UpdateTabVisuals();
            UpdateSidebarLabels();

            string body = BuildMainBody();
            int hash = (_data.PanelTab + "|" + _data.SelectedItemId + "|" + body).GetHashCode();

            if (force || hash != _lastHash)
            {
                _lastHash = hash;
                _mainTitle.Text = BuildMainTitle();
                _mainBody.Text = body;
                _status.Text = "Shift+Q Open/Close | Current Tab: " + _data.PanelTab + " | Selected: " + _data.SelectedItemId;
            }

            SetVisible(_data.PanelVisible);
            HudMain.EnableCursor = _data.PanelVisible;
        }

        void UpdateTabVisuals()
        {
            if (_tabScenario != null) _tabScenario.Color = TabColor("SCENARIO");
            if (_tabQuests != null) _tabQuests.Color = TabColor("QUESTS");
            if (_tabFactions != null) _tabFactions.Color = TabColor("FACTIONS");
            if (_tabIntel != null) _tabIntel.Color = TabColor("INTEL");
        }

        void UpdateSidebarLabels()
        {
            if (_sideTitle == null) return;

            if (_data.PanelTab == "SCENARIO")
            {
                _sideTitle.Text = "SCENARIO";
                _sideOverview.Text = "Campaign Overview";
                _sidePrimary.Text = "Current Objective";
                _sideSecondary.Text = "Progression Chain";
                _sideTertiary.Text = "Sector Status";
            }
            else if (_data.PanelTab == "QUESTS")
            {
                _sideTitle.Text = "QUESTS";
                _sideOverview.Text = "Quest Overview";
                _sidePrimary.Text = "Active Objectives";
                _sideSecondary.Text = "Completed";
                _sideTertiary.Text = "Locked";
            }
            else if (_data.PanelTab == "FACTIONS")
            {
                _sideTitle.Text = "FACTIONS";
                _sideOverview.Text = "Faction Overview";
                _sidePrimary.Text = "UTD Status";
                _sideSecondary.Text = "Conquest Chain";
                _sideTertiary.Text = "Doctrine";
            }
            else
            {
                _sideTitle.Text = "INTEL LOG";
                _sideOverview.Text = "Recent Events";
                _sidePrimary.Text = "Operational Intel";
                _sideSecondary.Text = "System Notes";
                _sideTertiary.Text = "Debug Commands";
            }

            HighlightSideButton(_sideOverview, "OVERVIEW");
            HighlightSideButton(_sidePrimary, "PRIMARY");
            HighlightSideButton(_sideSecondary, "SECONDARY");
            HighlightSideButton(_sideTertiary, "TERTIARY");
        }

        void HighlightSideButton(LabelBoxButton button, string id)
        {
            if (button == null) return;
            button.Color = _data.SelectedItemId == id ? new Color(0, 75, 100, 255) : new Color(16, 35, 48, 255);
        }

        string BuildMainTitle()
        {
            if (_data.PanelTab == "SCENARIO") return "SCENARIO / " + _data.SelectedItemId;
            if (_data.PanelTab == "QUESTS") return "QUESTS / " + _data.SelectedItemId;
            if (_data.PanelTab == "FACTIONS") return "FACTION STATES / " + _data.SelectedItemId;
            return "INTEL LOG / " + _data.SelectedItemId;
        }

        string BuildMainBody()
        {
            StringBuilder sb = new StringBuilder();

            if (_data.PanelTab == "SCENARIO") BuildScenario(sb);
            else if (_data.PanelTab == "QUESTS") BuildQuests(sb);
            else if (_data.PanelTab == "FACTIONS") BuildFactions(sb);
            else BuildIntel(sb);

            return sb.ToString();
        }

        void BuildScenario(StringBuilder sb)
        {
            if (_data.SelectedItemId == "PRIMARY")
            {
                sb.AppendLine("CURRENT OBJECTIVE");
                sb.AppendLine(GetActiveObjectiveTitle());
                sb.AppendLine("");
                sb.AppendLine(GetActiveObjectiveDescription());
                return;
            }

            if (_data.SelectedItemId == "SECONDARY")
            {
                sb.AppendLine("SOLARFRONTIER PROGRESSION CHAIN");
                sb.AppendLine("1. Conquer regional factions on Earth.");
                sb.AppendLine("2. Recover jump-gate components.");
                sb.AppendLine("3. Build and activate Jump Gate Alpha.");
                sb.AppendLine("4. Travel to the next sector.");
                sb.AppendLine("5. Repeat escalation until final sector completion.");
                return;
            }

            if (_data.SelectedItemId == "TERTIARY")
            {
                sb.AppendLine("SECTOR STATUS");
                sb.AppendLine("Current Sector: " + _data.CurrentSector);
                sb.AppendLine("Campaign Stage: " + _data.CampaignStage);
                sb.AppendLine("Tracked Factions: " + _data.Factions.Count);
                sb.AppendLine("Tracked Quests: " + _data.Quests.Count);
                return;
            }

            sb.AppendLine("CAMPAIGN OVERVIEW");
            sb.AppendLine("Campaign: " + _data.CampaignId);
            sb.AppendLine("Sector: " + _data.CurrentSector);
            sb.AppendLine("Stage: " + _data.CampaignStage);
            sb.AppendLine("");
            sb.AppendLine("Scenarium tracks campaign state, conquest chains, faction defeat,");
            sb.AppendLine("quest unlocks, rewards, and future sector progression.");
        }

        void BuildQuests(StringBuilder sb)
        {
            if (_data.SelectedItemId == "PRIMARY")
            {
                sb.AppendLine("ACTIVE OBJECTIVES");
                foreach (ScenariumQuestState q in _data.Quests)
                    if (q.Revealed && !q.Completed)
                        sb.AppendLine("[ ] " + q.Title + "\n    " + q.Description + "\n");
                return;
            }

            if (_data.SelectedItemId == "SECONDARY")
            {
                sb.AppendLine("COMPLETED OBJECTIVES");
                foreach (ScenariumQuestState q in _data.Quests)
                    if (q.Completed)
                        sb.AppendLine("[X] " + q.Title + " (" + q.Id + ")");
                return;
            }

            if (_data.SelectedItemId == "TERTIARY")
            {
                sb.AppendLine("LOCKED OBJECTIVES");
                foreach (ScenariumQuestState q in _data.Quests)
                    if (!q.Revealed && !q.Completed)
                        sb.AppendLine("[?] " + q.Id);
                return;
            }

            sb.AppendLine("QUEST OVERVIEW");
            sb.AppendLine("Active: " + CountActiveQuests());
            sb.AppendLine("Completed: " + CountCompletedQuests());
            sb.AppendLine("Locked: " + CountLockedQuests());
            sb.AppendLine("");
            sb.AppendLine("Use the sidebar to inspect active, completed, or locked objectives.");
        }

        void BuildFactions(StringBuilder sb)
        {
            if (_data.SelectedItemId == "PRIMARY")
            {
                sb.AppendLine("UTD STATUS");
                sb.AppendLine("State: " + GetFactionState("UTD"));
                sb.AppendLine("");
                sb.AppendLine("The United Terran Directorate is the current proof-of-concept");
                sb.AppendLine("faction for Scenarium conquest state.");
                return;
            }

            if (_data.SelectedItemId == "SECONDARY")
            {
                sb.AppendLine("UTD CONQUEST CHAIN");
                sb.AppendLine(GetQuestMark("UTD_OUTPOST") + " Military Outpost");
                sb.AppendLine(GetQuestMark("UTD_REGIONAL_BASE") + " Regional Military Base");
                sb.AppendLine(GetQuestMark("UTD_HQ") + " Clan HQ");
                sb.AppendLine(GetQuestMark("GATE_ALPHA_COMPONENT") + " Jump Gate Component Reward");
                return;
            }

            if (_data.SelectedItemId == "TERTIARY")
            {
                sb.AppendLine("DOCTRINE STATES");
                sb.AppendLine("Peacetime: mining, transport, economy, patrols.");
                sb.AppendLine("Alert: QRF, interceptors, guarded convoys.");
                sb.AppendLine("War: military outposts, defended sites, strike packages.");
                sb.AppendLine("Defeated: faction spawns disabled by campaign logic.");
                return;
            }

            sb.AppendLine("FACTION OVERVIEW");
            foreach (ScenariumFactionState f in _data.Factions)
                sb.AppendLine(f.Tag + " | " + f.State + (f.Defeated ? " | DEFEATED" : ""));
        }

        void BuildIntel(StringBuilder sb)
        {
            if (_data.SelectedItemId == "PRIMARY")
            {
                sb.AppendLine("OPERATIONAL INTEL");
                sb.AppendLine("Intel entries will later include coordinates, discovered bases,");
                sb.AppendLine("faction alerts, and unlocked objective chains.");
                return;
            }

            if (_data.SelectedItemId == "SECONDARY")
            {
                sb.AppendLine("SYSTEM NOTES");
                sb.AppendLine("RichHud interface active.");
                sb.AppendLine("Mouse cursor should be enabled while the Scenarium panel is open.");
                sb.AppendLine("Tab buttons and sidebar buttons are clickable.");
                return;
            }

            if (_data.SelectedItemId == "TERTIARY")
            {
                sb.AppendLine("DEBUG COMMANDS");
                sb.AppendLine("/scen complete UTD_OUTPOST");
                sb.AppendLine("/scen complete UTD_REGIONAL_BASE");
                sb.AppendLine("/scen complete UTD_HQ");
                sb.AppendLine("/scen war UTD");
                sb.AppendLine("/scen reset");
                return;
            }

            sb.AppendLine("RECENT EVENTS");
            int start = Math.Max(0, _data.Events.Count - 12);
            for (int i = start; i < _data.Events.Count; i++)
                sb.AppendLine("> " + _data.Events[i].Message);
        }

        string GetActiveObjectiveTitle()
        {
            foreach (ScenariumQuestState q in _data.Quests)
                if (q.Revealed && !q.Completed && q.Active)
                    return q.Title;

            foreach (ScenariumQuestState q in _data.Quests)
                if (q.Revealed && !q.Completed)
                    return q.Title;

            return "None";
        }

        string GetActiveObjectiveDescription()
        {
            foreach (ScenariumQuestState q in _data.Quests)
                if (q.Revealed && !q.Completed && q.Active)
                    return q.Description;

            foreach (ScenariumQuestState q in _data.Quests)
                if (q.Revealed && !q.Completed)
                    return q.Description;

            return "No active objective.";
        }

        string GetFactionState(string tag)
        {
            foreach (ScenariumFactionState f in _data.Factions)
            {
                if (string.Equals(f.Tag, tag, StringComparison.OrdinalIgnoreCase))
                    return f.State + (f.Defeated ? " / DEFEATED" : "");
            }

            return "Unknown";
        }

        string GetQuestMark(string id)
        {
            foreach (ScenariumQuestState q in _data.Quests)
            {
                if (string.Equals(q.Id, id, StringComparison.OrdinalIgnoreCase))
                {
                    if (q.Completed) return "[X]";
                    if (q.Revealed) return "[ ]";
                    return "[?]";
                }
            }

            return "[?]";
        }

        int CountActiveQuests()
        {
            int count = 0;
            foreach (ScenariumQuestState q in _data.Quests)
                if (q.Revealed && !q.Completed)
                    count++;
            return count;
        }

        int CountCompletedQuests()
        {
            int count = 0;
            foreach (ScenariumQuestState q in _data.Quests)
                if (q.Completed)
                    count++;
            return count;
        }

        int CountLockedQuests()
        {
            int count = 0;
            foreach (ScenariumQuestState q in _data.Quests)
                if (!q.Revealed && !q.Completed)
                    count++;
            return count;
        }

        void SetVisible(bool visible)
        {
            if (_root != null) _root.Visible = visible;
            if (_titleBar != null) _titleBar.Visible = visible;
            if (_tabBar != null) _tabBar.Visible = visible;
            if (_sidePanel != null) _sidePanel.Visible = visible;
            if (_mainPanel != null) _mainPanel.Visible = visible;
            if (_statusBar != null) _statusBar.Visible = visible;
            if (_title != null) _title.Visible = visible;
            if (_sideTitle != null) _sideTitle.Visible = visible;
            if (_mainTitle != null) _mainTitle.Visible = visible;
            if (_mainBody != null) _mainBody.Visible = visible;
            if (_status != null) _status.Visible = visible;

            SetButtonVisible(_tabScenario, visible);
            SetButtonVisible(_tabQuests, visible);
            SetButtonVisible(_tabFactions, visible);
            SetButtonVisible(_tabIntel, visible);
            SetButtonVisible(_sideOverview, visible);
            SetButtonVisible(_sidePrimary, visible);
            SetButtonVisible(_sideSecondary, visible);
            SetButtonVisible(_sideTertiary, visible);
            SetButtonVisible(_closeButton, visible);
        }

        void SetButtonVisible(LabelBoxButton button, bool visible)
        {
            if (button != null)
                button.Visible = visible;
        }

        public void CloseAndDispose()
        {
            Close();
            _created = false;
        }
    }
}
