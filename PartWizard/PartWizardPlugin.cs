// Copyright (c) 2014, Eric Harris (ozraven)
// All rights reserved.

// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
//     * Redistributions of source code must retain the above copyright
//       notice, this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above copyright
//       notice, this list of conditions and the following disclaimer in the
//       documentation and/or other materials provided with the distribution.
//     * Neither the name of the copyright holder nor the
//       names of its contributors may be used to endorse or promote products
//       derived from this software without specific prior written permission.

// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
// ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL ERIC HARRIS BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

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
        private IButton partWizardBlizzyButton;
        private ApplicationLauncherButton partWizardStockButton;

        private PartWizardWindow partWizardWindow;

        public static readonly string Name = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductName;
        public static readonly string Version = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion;

        internal static bool ToolbarIsStock;
        internal static bool ToolbarTypeToggleActive = false;
        private const string BlizzyToolbarIconActive = "PartWizard/Icons/partwizard_active_toolbar_24_icon";
        private const string BlizzyToolbarIconInactive = "PartWizard/Icons/partwizard_inactive_toolbar_24_icon";
        private const string StockToolbarIconActive = "PartWizard/Icons/partwizard_active_toolbar_38_icon";
        private const string StockToolbarIconInactive = "PartWizard/Icons/partwizard_inactive_toolbar_38_icon";
        private const string keyToolbarIsStock = "toolbarIsStock";
        private const string KeyToolbarIconActive = "toolbarActiveIcon";
        private const string KeyToolbarIconInactive = "toolbarInactiveIcon";

        private string toolbarIconActive;
        private string toolbarIconInactive;

        public void Awake()
        {
            if(HighLogic.LoadedSceneIsEditor)
            {
                this.partWizardWindow = new PartWizardWindow(PartWizardPlugin.Name, PartWizardPlugin.Version);
                this.partWizardWindow.OnVisibleChanged += partWizardWindow_OnVisibleChanged;

                // Are we using Blizzy's Toolbar?
                ToolbarIsStock = bool.Parse(Configuration.GetValue(PartWizardPlugin.keyToolbarIsStock, "False"));

                if(ToolbarManager.ToolbarAvailable && !ToolbarIsStock)
                {
                    this.toolbarIconActive = Configuration.GetValue(PartWizardPlugin.KeyToolbarIconActive, PartWizardPlugin.BlizzyToolbarIconActive);
                    this.toolbarIconInactive = Configuration.GetValue(PartWizardPlugin.KeyToolbarIconInactive, PartWizardPlugin.BlizzyToolbarIconInactive);

                    Configuration.SetValue(PartWizardPlugin.keyToolbarIsStock, ToolbarIsStock.ToString());
                    Configuration.SetValue(PartWizardPlugin.KeyToolbarIconActive, this.toolbarIconActive);
                    Configuration.SetValue(PartWizardPlugin.KeyToolbarIconInactive, this.toolbarIconInactive);
                    Configuration.Save();

                    this.partWizardBlizzyButton = ToolbarManager.Instance.add("PartWizardNS", "partWizardButton");
                    this.partWizardBlizzyButton.ToolTip = PartWizardPlugin.Name;
                    this.partWizardBlizzyButton.OnClick += this.partWizardButton_Click;
                    this.partWizardBlizzyButton.Visibility = new GameScenesVisibility(GameScenes.EDITOR);

                    this.UpdateToolbarIcon();
                }
                else
                {
                    // Blizzy toolbar not available, or Stock Toolbar selected Let's go stock :(
                    ToolbarIsStock = true;
                    GameEvents.onGUIApplicationLauncherReady.Add(OnGUIAppLauncherReady);
                    GameEvents.onGUIApplicationLauncherDestroyed.Add(OnGUIAppLauncherDestroyed);

                    this.toolbarIconActive = Configuration.GetValue(PartWizardPlugin.KeyToolbarIconActive, PartWizardPlugin.StockToolbarIconActive);
                    this.toolbarIconInactive = Configuration.GetValue(PartWizardPlugin.KeyToolbarIconInactive, PartWizardPlugin.StockToolbarIconInactive);

                    Configuration.SetValue(PartWizardPlugin.keyToolbarIsStock, ToolbarIsStock.ToString());
                    Configuration.SetValue(PartWizardPlugin.KeyToolbarIconActive, this.toolbarIconActive);
                    Configuration.SetValue(PartWizardPlugin.KeyToolbarIconInactive, this.toolbarIconInactive);
                    Configuration.Save();
                }
            }
        }

        public void Start()
        {
            // this is needed because of a bug in KSP with event onGUIAppLauncherReady.
            if(ToolbarIsStock && HighLogic.LoadedSceneIsEditor)
                OnGUIAppLauncherReady();
        }

        private void partWizardWindow_OnVisibleChanged(GUIWindow window, bool visible)
        {
            this.UpdateToolbarIcon();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "GUI")]
        public void OnGUI()
        {
            if(ToolbarTypeToggleActive)
            {
                ToolbarTypeToggle();
            }
            if(this.partWizardWindow != null)
            {
                this.partWizardWindow.Render();
            }
        }

        public void OnDestroy()
        {
            this.partWizardWindow.OnVisibleChanged -= partWizardWindow_OnVisibleChanged;

            this.partWizardWindow.Hide();
            this.partWizardWindow = null;

            if(ToolbarIsStock)
            {
                GameEvents.onGUIApplicationLauncherReady.Remove(OnGUIAppLauncherReady);
                GameEvents.onGUIApplicationLauncherDestroyed.Remove(OnGUIAppLauncherDestroyed);

                if(this.partWizardStockButton != null)
                {
                    ApplicationLauncher.Instance.RemoveModApplication(this.partWizardStockButton);
                    this.partWizardStockButton = null;
                }
            }
            else
            {
                this.partWizardBlizzyButton.Destroy();
                this.partWizardBlizzyButton = null;
            }
        }

        private void ToggleVisibility()
        {
            if(this.partWizardWindow.Visible)
            {
                this.partWizardWindow.Hide();
            }
            else
            {
                this.partWizardWindow.Show();
            }
        }

        private void partWizardButton_Click(ClickEvent e)
        {
            this.ToggleVisibility();
        }

        private void UpdateToolbarIcon()
        {
            if(!ToolbarIsStock)
            {
                this.partWizardBlizzyButton.TexturePath = this.partWizardWindow.Visible ? this.toolbarIconActive : this.toolbarIconInactive;
            }
            else
            {
                this.partWizardStockButton.SetTexture((Texture)GameDatabase.Instance.GetTexture(this.partWizardWindow.Visible ? this.toolbarIconActive : this.toolbarIconInactive, false));
            }
        }

        private void OnGUIAppLauncherReady()
        {
            // Setup PW Stock Toolbar button
            bool hidden = false;
            if(ApplicationLauncher.Ready && (partWizardStockButton == null || !ApplicationLauncher.Instance.Contains(partWizardStockButton, out hidden)))
            {
                partWizardStockButton = ApplicationLauncher.Instance.AddModApplication(
                    ToggleVisibility,
                    ToggleVisibility,
                    DummyHandler,
                    DummyHandler,
                    DummyHandler,
                    DummyHandler,
                    ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH,
                    (Texture)GameDatabase.Instance.GetTexture(StockToolbarIconActive, false));

                if(this.partWizardWindow.Visible)
                    partWizardStockButton.SetTexture((Texture)GameDatabase.Instance.GetTexture(this.partWizardWindow.Visible ? this.toolbarIconActive : this.toolbarIconInactive, false));
            }
        }

        private void OnGUIAppLauncherDestroyed()
        {
            if(partWizardStockButton != null)
            {
                ApplicationLauncher.Instance.RemoveModApplication(partWizardStockButton);
                partWizardStockButton = null;
            }
        }

        void DummyHandler()
        {
        }

        internal void ToolbarTypeToggle()
        {
            // ToolbarIsStock value has not yet changed, so we evaluate the value against the fact it will be chaning.
            if(ToolbarIsStock)
            {
                // Was Stock bar, so let't try to use Blizzy's toolbar
                if(ToolbarManager.ToolbarAvailable)
                {
                    // Okay, Blizzy toolbar is available, so lets switch.
                    OnGUIAppLauncherDestroyed();
                    GameEvents.onGUIApplicationLauncherReady.Remove(OnGUIAppLauncherReady);
                    GameEvents.onGUIApplicationLauncherDestroyed.Remove(OnGUIAppLauncherDestroyed);
                    ToolbarIsStock = false;

                    this.toolbarIconActive = Configuration.GetValue(PartWizardPlugin.KeyToolbarIconActive, PartWizardPlugin.BlizzyToolbarIconActive);
                    this.toolbarIconInactive = Configuration.GetValue(PartWizardPlugin.KeyToolbarIconInactive, PartWizardPlugin.BlizzyToolbarIconInactive);

                    this.partWizardBlizzyButton = ToolbarManager.Instance.add("PartWizardNS", "partWizardButton");
                    this.partWizardBlizzyButton.ToolTip = PartWizardPlugin.Name;
                    this.partWizardBlizzyButton.OnClick += this.partWizardButton_Click;
                    this.partWizardBlizzyButton.Visibility = new GameScenesVisibility(GameScenes.EDITOR);

                    // save the settings.
                    Configuration.SetValue(PartWizardPlugin.keyToolbarIsStock, ToolbarIsStock.ToString());
                    Configuration.SetValue(PartWizardPlugin.KeyToolbarIconActive, this.toolbarIconActive);
                    Configuration.SetValue(PartWizardPlugin.KeyToolbarIconInactive, this.toolbarIconInactive);
                    Configuration.Save();

                    this.UpdateToolbarIcon();
                }
                else
                {
                    // We failed to activate the toolbar, so revert to stock
                    ToolbarIsStock = true;
                    GameEvents.onGUIApplicationLauncherReady.Add(OnGUIAppLauncherReady);
                    GameEvents.onGUIApplicationLauncherDestroyed.Add(OnGUIAppLauncherDestroyed);
                    this.partWizardBlizzyButton.Visible = false;

                    this.toolbarIconActive = Configuration.GetValue(PartWizardPlugin.KeyToolbarIconActive, PartWizardPlugin.StockToolbarIconActive);
                    this.toolbarIconInactive = Configuration.GetValue(PartWizardPlugin.KeyToolbarIconInactive, PartWizardPlugin.StockToolbarIconInactive);

                    Configuration.SetValue(PartWizardPlugin.keyToolbarIsStock, ToolbarIsStock.ToString());
                    Configuration.SetValue(PartWizardPlugin.KeyToolbarIconActive, this.toolbarIconActive);
                    Configuration.SetValue(PartWizardPlugin.KeyToolbarIconInactive, this.toolbarIconInactive);
                    Configuration.Save();

                    OnGUIAppLauncherReady();
                }
            }
            else
            {
                // Use stock Toolbar
                ToolbarIsStock = true;
                this.partWizardBlizzyButton.Visible = false;
                GameEvents.onGUIApplicationLauncherReady.Add(OnGUIAppLauncherReady);
                GameEvents.onGUIApplicationLauncherDestroyed.Add(OnGUIAppLauncherDestroyed);
                this.toolbarIconActive = Configuration.GetValue(PartWizardPlugin.KeyToolbarIconActive, PartWizardPlugin.StockToolbarIconActive);
                this.toolbarIconInactive = Configuration.GetValue(PartWizardPlugin.KeyToolbarIconInactive, PartWizardPlugin.StockToolbarIconInactive);

                Configuration.SetValue(PartWizardPlugin.keyToolbarIsStock, ToolbarIsStock.ToString());
                Configuration.SetValue(PartWizardPlugin.KeyToolbarIconActive, this.toolbarIconActive);
                Configuration.SetValue(PartWizardPlugin.KeyToolbarIconInactive, this.toolbarIconInactive);
                Configuration.Save();

                OnGUIAppLauncherReady();
            }
            ToolbarTypeToggleActive = false;
        }
    }
}
