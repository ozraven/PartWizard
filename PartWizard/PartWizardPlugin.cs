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
                    this.partWizardToolbarButton.Visibility = new GameScenesVisibility(GameScenes.EDITOR);

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
                this.partWizardWindow.Show();
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
