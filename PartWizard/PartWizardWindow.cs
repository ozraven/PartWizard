using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

using UnityEngine;

using Localized = PartWizard.Resources.Strings;

namespace PartWizard
{
    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    [CLSCompliant(false)]
    public class PartWizardWindow : MonoBehaviour
    {
        private const float DefaultX = 300;
        private const float DefaultY = 200;
        private const float DefaultWidth = 250;
        private const float DefaultHeight = 400;
        private const float MaximumWidth = 600;    // TODO: Review what this does.
        private const float MaximumHeight = 500;   // TODO: Review what this does.
        
        private string pluginName;
        private string windowTitle;

        private bool visible;
        
        private IButton partWizardToolbarButton;

        private int partWizardWindowId;

        // Early instantiation of this so that when we save the configuration we don't end up with all zeros. (Rect is a struct, so the default constructor is used as structs
        // are non-nullable entities.)
        private Rect window = new Rect(DefaultX, DefaultY, DefaultWidth, DefaultHeight);

        private GUIStyle tooltipLabelStyle;
        private GUIStyle partCountLabelStyle;

        private volatile bool controllingPartHighlight = false;
        private uint highlightedPartId = 0;

        private Vector2 scrollPosition;
        
        public void Awake()
        {
            if(HighLogic.LoadedSceneIsEditor)
            {
                Assembly executingAssembly = Assembly.GetExecutingAssembly();
                this.pluginName = System.Diagnostics.FileVersionInfo.GetVersionInfo(executingAssembly.Location).ProductName;
                string version = System.Diagnostics.FileVersionInfo.GetVersionInfo(executingAssembly.Location).ProductVersion;

                this.windowTitle = string.Format(CultureInfo.CurrentCulture, "{0} ({1})", this.pluginName, version);

                this.tooltipLabelStyle = new GUIStyle();
                // TODO: Magic numbers.
                this.tooltipLabelStyle.normal.textColor = Color.yellow;

                this.partCountLabelStyle = new GUIStyle();
                this.partCountLabelStyle.normal.textColor = Color.white;
                this.partCountLabelStyle.alignment = TextAnchor.LowerRight;

                // Always start hidden to stay out of the way.
                this.visible = false;
                
                if(ToolbarManager.ToolbarAvailable)
                {
                    this.partWizardToolbarButton = ToolbarManager.Instance.add("PartWizardNS", "partWizardButton");
                    this.partWizardToolbarButton.ToolTip = this.pluginName;
                    this.partWizardToolbarButton.OnClick += partWizardButton_Click;
                    this.partWizardToolbarButton.Visibility = new GameScenesVisibility(GameScenes.EDITOR, GameScenes.SPH);

                    this.UpdateToolbarIcon();
                }
            }
        }
        
        // TODO: Move this elsewhere. The button exists when this window does not and this window doesn't care how it comes in to being.
        private void partWizardButton_Click(ClickEvent e)
        {
            this.visible = !this.visible;

            if(this.visible)
            {
                this.partWizardWindowId = gameObject.GetInstanceID();

                float x = Configuration.GetValue("x", DefaultX);
                float y = Configuration.GetValue("y", DefaultY);
                float width = Configuration.GetValue("width", DefaultWidth);
                float height = Configuration.GetValue("height", DefaultHeight);

                // TODO: Do I need to clean this up?
                this.window = new Rect(x, y, width, height);

                this.window.x = Mathf.Clamp(this.window.x, 0, Screen.width - MaximumWidth);
                this.window.y = Mathf.Clamp(this.window.y, 0, Screen.height - MaximumHeight);
            }

            this.UpdateToolbarIcon();
        }

        private void UpdateToolbarIcon()
        {
            if(this.visible && ToolbarManager.ToolbarAvailable)
            {
                this.partWizardToolbarButton.TexturePath = "PartWizard/Icons/partwizard_active_toolbar_24_icon";
            }
            else
            {
                this.partWizardToolbarButton.TexturePath = "PartWizard/Icons/partwizard_inactive_toolbar_24_icon";
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "GUI")]
        public void OnGUI()
        {
            if(this.visible && (HighLogic.LoadedScene == GameScenes.EDITOR || HighLogic.LoadedScene == GameScenes.SPH))
            {
                GUI.skin.window.clipping = TextClipping.Clip;
                if(Event.current.type == EventType.Layout)
                {
                    this.window = GUILayout.Window(this.partWizardWindowId, this.window, OnRender, this.windowTitle);    
                }
            }
        }

        public void OnDestroy()
        {
            if(this.window == null)
            {
                Debug.Log("PartWizard :: OnDestroy, this.window is null.");
            }
            else
            {
                Configuration.SetValue("x", (int)this.window.x);
                Configuration.SetValue("y", (int)this.window.y);
                Configuration.SetValue("width", (int)this.window.width);
                Configuration.SetValue("height", (int)this.window.height);

                Configuration.Save();
            }

            this.partWizardToolbarButton.Destroy(); this.partWizardToolbarButton = null;
        }

        private void OnRender(int windowId)
        {
            GUIControls.BeginLayout();

            // TODO: Wrap all of this in a try...finally, and in the finally call EndLayout, so we don't start spewing GUI exceptions to the log.

            // TODO: When something does go wrong, set a flag so we render a simple message asking the user to check the forums and/or post their log files.

            if(GUIControls.TitleBarButton(this.window))
            {
                // TODO: Duplicate code. Move to a method and call it.
                this.visible = false;

                this.UpdateToolbarIcon();
            }

            List<Part> parts = EditorLogic.fetch.ship != null ? EditorLogic.fetch.ship.Parts : new List<Part>();

            GUILayout.BeginVertical();

            this.scrollPosition = GUILayout.BeginScrollView(this.scrollPosition, false, false);

            foreach(Part part in parts)
            {
                bool mouseOver = false;         // Must be updated (|=) by each control that can trigger part highlighting when the mouse is over the part in the list.

                GUILayout.BeginHorizontal();

                #region Part Label

                bool labelMouseOver = false;    // Will be set to true if the mouse is over the part's label.

                // We want to draw the part's name first, but it won't exist when the time comes to render it if the user chooses to delete it. DisplayName() must be
                // careful to make sure the part still exists.
                GUIControls.MouseOverLabel(new GUIContent(part.DisplayName(), part.partInfo.name), out labelMouseOver);

                mouseOver |= labelMouseOver;

                #endregion

                // Only enable the following buttons if there is no actively selected part, but we want to have them drawn.
                GUI.enabled = !EditorLogic.SelectedPart;

                #region Break Symmetry Button

                bool breakSymmetryMouseOver = false;

                GUI.enabled = !EditorLogic.SelectedPart && PartWizard.IsSymmetricalRoot(part);

                string breakSymmetryTooltip = GUI.enabled ? Localized.BreakSymmetryDescription : default(string);

                // TODO: Magic numbers.
                if(GUIControls.MouseOverButton(new GUIContent(Localized.BreakSymmetryButtonText, breakSymmetryTooltip), out breakSymmetryMouseOver, GUILayout.Width(22)))
                {
                    PartWizard.BreakSymmetry(part);

                    Debug.Log(string.Format(CultureInfo.InvariantCulture, "[PartWizard] {0} symmetry broken", part.name));
                }

                mouseOver |= breakSymmetryMouseOver;

                #endregion

                #region Delete Button

                bool deleteButtonMouseOver = false;     // Will be set to true if the mouse is over the part's delete button.

                bool deleted = false;                   // Will be set to true if the delete button was pressed.

                // Only enable the delete button if there are no parts selected, there is more than just the root part, and the part has no children because we can't pluck
                // parts from between other parts.
                GUI.enabled = !EditorLogic.SelectedPart && parts.Count > 1 && (part.children.Count == 0);

                string deleteTooltip = GUI.enabled ? Localized.DeletePartDescription : default(string);

                // TODO: Magic numbers.
                if(GUIControls.MouseOverButton(new GUIContent(Localized.DeletePartButtonText, deleteTooltip), out deleteButtonMouseOver, GUILayout.Width(22)))
                {
                    Debug.Log(string.Format(CultureInfo.InvariantCulture, "[PartWizard] deleting part {0}", part.name));

                    PartWizard.Delete(part);

                    // Set a flag so additional GUI logic can decide what to do in the case where a part is deleted.
                    deleted = true;
                }

                mouseOver |= deleteButtonMouseOver;

                #endregion

                GUI.enabled = true;

                GUILayout.EndHorizontal();      // End of row for this part.

                if(!deleted)
                {
                    if(mouseOver)
                    {
                        part.SetHighlight(true);

                        this.controllingPartHighlight = true;
                        this.highlightedPartId = part.uid;
                    }
                    else if(controllingPartHighlight && part.uid == this.highlightedPartId)
                    {
                        part.SetHighlight(false);

                        this.controllingPartHighlight = false;
                        this.highlightedPartId = 0;
                    }
                }

                // If we deleted a part, then just jump out of the loop since the parts list has been modified.
                if(deleted)
                {
                    break;
                }
            }

            GUILayout.EndScrollView();

            #region Status Area

            GUILayout.Space(3);

            string status = default(string);

            int partCount = EditorLogic.fetch.ship != null ? EditorLogic.fetch.ship.Parts.Count : 0;

            if(!string.IsNullOrEmpty(GUI.tooltip))
            {
                status = string.Format(CultureInfo.CurrentCulture, Localized.StatusLabelTooltipTextFormat, partCount, GUI.tooltip);
            }
            else
            {
                status = string.Format(CultureInfo.CurrentCulture, Localized.StatusLabelTextFormat, partCount);
            }

            GUILayout.Label(status, this.tooltipLabelStyle);
            
            GUILayout.EndVertical();

            #endregion

            // Make the window draggable.
            GUI.DragWindow();

            GUIControls.EndLayout();
        }
    }
}
