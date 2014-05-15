using System;
using System.Diagnostics;
using System.Reflection;

using UnityEngine;

namespace PartWizard
{
    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    [CLSCompliant(false)]
    public sealed class PartWizardPlugin : MonoBehaviour
    {
        private IButton partWizardToolbarButton;

        private PartWizardWindow partWizardWindow;

        public static readonly string Name = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductName;
        public static readonly string Version = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion;

        private const string DefaultToolbarIconActive = "PartWizard/Icons/partwizard_active_toolbar_24_icon";
        private const string DefaultToolbarIconInactive = "PartWizard/Icons/partwizard_inactive_toolbar_24_icon";
        private const string KeyToolbarIconActive = "toolbarActiveIcon";
        private const string KeyToolbarIconInactive = "toolbarInactiveIcon";

        private string toolbarIconActive;
        private string toolbarIconInactive;

        public void Awake()
        {
            if(HighLogic.LoadedSceneIsEditor)
            {
                this.partWizardWindow = new PartWizardWindow(PartWizardPlugin.Name, PartWizardPlugin.Version);

                if(ToolbarManager.ToolbarAvailable)
                {
                    this.toolbarIconActive = Configuration.GetValue(PartWizardPlugin.KeyToolbarIconActive, PartWizardPlugin.DefaultToolbarIconActive);
                    this.toolbarIconInactive = Configuration.GetValue(PartWizardPlugin.KeyToolbarIconInactive, PartWizardPlugin.DefaultToolbarIconInactive);

                    Configuration.SetValue(PartWizardPlugin.KeyToolbarIconActive, this.toolbarIconActive);
                    Configuration.SetValue(PartWizardPlugin.KeyToolbarIconInactive, this.toolbarIconInactive);
                    Configuration.Save();

                    this.partWizardToolbarButton = ToolbarManager.Instance.add("PartWizardNS", "partWizardButton");
                    this.partWizardToolbarButton.ToolTip = PartWizardPlugin.Name;
                    this.partWizardToolbarButton.OnClick += this.partWizardButton_Click;
                    this.partWizardToolbarButton.Visibility = new GameScenesVisibility(GameScenes.EDITOR, GameScenes.SPH);

                    this.UpdateToolbarIcon();
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "GUI")]
        public void OnGUI()
        {
            if(this.partWizardWindow != null)
            {
                this.partWizardWindow.Render();
            }
        }

        public void OnDestroy()
        {
            this.partWizardWindow.Hide();
            this.partWizardWindow = null;

            this.partWizardToolbarButton.Destroy();
            this.partWizardToolbarButton = null;
        }

        private void ToggleVisibility()
        {
            if(this.partWizardWindow.Visible)
            {
                this.partWizardWindow.Hide();
            }
            else
            {
                this.partWizardWindow.Show(gameObject.GetInstanceID());
            }

            this.UpdateToolbarIcon();
        }

        private void partWizardButton_Click(ClickEvent e)
        {
            this.ToggleVisibility();
        }

        private void UpdateToolbarIcon()
        {
            if(ToolbarManager.ToolbarAvailable && this.partWizardWindow.Visible)
            {
                this.partWizardToolbarButton.TexturePath = this.toolbarIconActive;
            }
            else
            {
                this.partWizardToolbarButton.TexturePath = this.toolbarIconInactive;
            }
        }
    }
}
