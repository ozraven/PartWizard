using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;

using UnityEngine;

using Localized = PartWizard.Resources.Strings;

namespace PartWizard
{
    // TODO: Move the MonoBehaviour elements to a dedicated PartWizardPlugin class.
    // TODO: Add support for a configurable hotkey to show/hide the PartWizard UI.
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

        private readonly Color TooltipLabelColor = Color.yellow;
        private readonly Color PartCountLabelColor = Color.white;

        private const string WindowPositionConfigurationName = "PART_WIZARD_WINDOW";

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

        private GUILayoutOption PartActionButtonWidth = GUILayout.Width(22);

        private volatile bool controllingPartHighlight = false;
        private uint highlightedPartId = 0;

        private Vector2 scrollPosition;

        private enum ViewType
        {
            All = 0,
            Hidden = 1
        }

        private ViewType viewType = ViewType.All;

        private GUIStyle selectedViewTypeStyle;
        private GUIStyle unselectedViewTypeStyle;

        private GUIContent[] viewTypeContents;

        private bool renderError = false;
        
        public void Awake()
        {
            if(HighLogic.LoadedSceneIsEditor)
            {
                Assembly executingAssembly = Assembly.GetExecutingAssembly();
                this.pluginName = System.Diagnostics.FileVersionInfo.GetVersionInfo(executingAssembly.Location).ProductName;
                string version = System.Diagnostics.FileVersionInfo.GetVersionInfo(executingAssembly.Location).ProductVersion;

                this.windowTitle = string.Format(CultureInfo.CurrentCulture, "{0} ({1})", this.pluginName, version);

                this.tooltipLabelStyle = new GUIStyle();
                this.tooltipLabelStyle.normal.textColor = TooltipLabelColor;

                this.partCountLabelStyle = new GUIStyle();
                this.partCountLabelStyle.normal.textColor = PartCountLabelColor;
                this.partCountLabelStyle.alignment = TextAnchor.LowerRight;

                this.selectedViewTypeStyle = new GUIStyle("button");
                this.selectedViewTypeStyle.onNormal.textColor = Color.green;
                this.selectedViewTypeStyle.onHover.textColor = Color.green;

                this.unselectedViewTypeStyle = new GUIStyle("button");

                this.viewTypeContents = new GUIContent[] { new GUIContent(Localized.ViewTypeAll), new GUIContent(Localized.ViewTypeHidden) };

                if(ToolbarManager.ToolbarAvailable)
                {
                    this.partWizardToolbarButton = ToolbarManager.Instance.add("PartWizardNS", "partWizardButton");
                    this.partWizardToolbarButton.ToolTip = this.pluginName;
                    this.partWizardToolbarButton.OnClick += partWizardButton_Click;
                    this.partWizardToolbarButton.Visibility = new GameScenesVisibility(GameScenes.EDITOR, GameScenes.SPH);
                }

                // Always start hidden to stay out of the way.
                this.Hide();
            }
        }

        private void Hide()
        {
            this.visible = false;

            this.UpdateToolbarIcon();
        }
        
        // TODO: Move this elsewhere. The button exists when this window does not and this window doesn't care how it comes in to being.
        private void partWizardButton_Click(ClickEvent e)
        {
            this.visible = !this.visible;

            if(this.visible)
            {
                this.partWizardWindowId = gameObject.GetInstanceID();

                this.window = Configuration.GetValue(PartWizardWindow.WindowPositionConfigurationName, new Rect(PartWizardWindow.DefaultX, PartWizardWindow.DefaultY, PartWizardWindow.DefaultWidth, PartWizardWindow.DefaultHeight));

                this.window.x = Mathf.Clamp(this.window.x, 0, Screen.width - MaximumWidth);
                this.window.y = Mathf.Clamp(this.window.y, 0, Screen.height - MaximumHeight);
            }
            else
            {
                if(!this.renderError)
                {
                    Configuration.SetValue(PartWizardWindow.WindowPositionConfigurationName, this.window);

                    Configuration.Save();
                }
            }

            this.UpdateToolbarIcon();
        }

        private void UpdateToolbarIcon()
        {
            // TODO: Magic strings.
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
                // TODO: Centralize logging.
                Debug.Log("PartWizard :: OnDestroy, this.window is null.");
            }
            else
            {
                if(!this.renderError)
                {
                    Configuration.SetValue(PartWizardWindow.WindowPositionConfigurationName, this.window);

                    Configuration.Save();
                }
            }

            this.partWizardToolbarButton.Destroy(); this.partWizardToolbarButton = null;
        }

        private void OnRender(int windowId)
        {
            GUIControls.BeginLayout();

            try
            {
                if(GUIControls.TitleBarButton(this.window))
                {
                    this.Hide();
                }

                if(!renderError)
                {
                    List<Part> parts = EditorLogic.fetch.ship != null ? EditorLogic.fetch.ship.Parts : new List<Part>();

                    GUILayout.BeginVertical();

                    #region Display Mode Control

                    GUILayout.BeginHorizontal();

                    this.viewType = (ViewType)GUIControls.HorizontalToggleSet((int)this.viewType, this.viewTypeContents, this.selectedViewTypeStyle, this.unselectedViewTypeStyle);

                    GUILayout.EndHorizontal();

                    if(this.viewType == ViewType.Hidden)
                    {
                        parts = parts.FindAll((p) => { return p.partInfo.category == PartCategories.none; });
                    }

                    #endregion

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

                        string breakabilityReport = default(string);
                        GUI.enabled = !EditorLogic.SelectedPart && PartWizard.HasBreakableSymmetry(part, out breakabilityReport);

                        string breakSymmetryTooltip = GUI.enabled ? Localized.BreakSymmetryDescription : default(string);

                        if(GUIControls.MouseOverButton(new GUIContent(Localized.BreakSymmetryButtonText, breakSymmetryTooltip), out breakSymmetryMouseOver, PartActionButtonWidth))
                        {
                            PartWizard.BreakSymmetry(part);

                            Debug.Log(string.Format(CultureInfo.InvariantCulture, "[PartWizard] {0} symmetry broken", part.name));
                        }

                        mouseOver |= breakSymmetryMouseOver;

                        #endregion

                        #region Delete Button

                        bool deleteButtonMouseOver = false;     // Will be set to true if the mouse is over the part's delete button.

                        bool deleted = false;                   // Will be set to true if the delete button was pressed.

                        GUI.enabled = !EditorLogic.SelectedPart && PartWizard.IsDeleteable(part);

                        string deleteTooltip = GUI.enabled ? Localized.DeletePartDescription : default(string);

                        if(GUIControls.MouseOverButton(new GUIContent(Localized.DeletePartButtonText, deleteTooltip), out deleteButtonMouseOver, PartActionButtonWidth))
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

                    if(!string.IsNullOrEmpty(GUI.tooltip))
                    {
                        status = string.Format(CultureInfo.CurrentCulture, Localized.StatusLabelTooltipTextFormat, parts.Count, GUI.tooltip);
                    }
                    else
                    {
                        status = string.Format(CultureInfo.CurrentCulture, Localized.StatusLabelTextFormat, parts.Count);
                    }

                    GUILayout.Label(status, this.tooltipLabelStyle);

                    GUILayout.EndVertical();

                    #endregion
                }
                else
                {
                    #region Error Mode GUI

                    GUILayout.BeginVertical();

                    GUILayoutOption maxWidth = GUILayout.MaxWidth(this.window.width);
                    GUILayoutOption maxHeight = GUILayout.MaxHeight(this.window.height / 2);    // Magic number 2 because we're going to have only 2 GUI controls, below.
                    GUILayoutOption lockWidth = GUILayout.ExpandWidth(false);
                    GUILayoutOption lockHeight = GUILayout.ExpandHeight(false);

                    GUILayout.Label(string.Format(CultureInfo.CurrentCulture, Localized.GuiRenderErrorTextFormat, this.pluginName), maxWidth, maxHeight, lockWidth, lockHeight);

                    // Fix up the path for the current environment.
                    string platformCompatibleRootPath = KSPUtil.ApplicationRootPath.Replace('/', Path.DirectorySeparatorChar);
                    // Trim off the extra path components to get the actual KSP root path.
                    string actualRootPath = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(platformCompatibleRootPath)));
                    string kspDataPath = Path.Combine(actualRootPath, "KSP_Data");
                    string kspLogFile = Path.Combine(kspDataPath, "output_log.txt");

                    GUIStyle textFieldStyle = new GUIStyle();
                    textFieldStyle.wordWrap = true;
                    textFieldStyle.normal.textColor = Color.white;

                    GUILayout.TextField(kspLogFile, textFieldStyle, maxWidth, maxHeight, lockWidth, lockHeight);

                    GUILayout.EndVertical();

                    #endregion
                }
            }
            catch(Exception e)
            {
                this.renderError = true;

                Debug.LogError("PartWizard :: Window rendering error, details follow:");
                Debug.LogException(e);

                throw;
            }
            finally
            {
                GUI.DragWindow();

                GUIControls.EndLayout();
            }
        }
    }
}
