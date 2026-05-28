using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MainMenuManager))]
public class MainMenuManagerEditor : Editor
{
    private static bool bottomBarOpen = true;
    private static bool panelsOpen = true;
    private static bool playFlowOpen = true;
    private static bool animationOpen;
    private static bool navbarSpritesOpen = true;
    private static bool navbarLabelsOpen;
    private static bool navbarIconsOpen = true;
    private static bool navbarSelectedOpen = true;
    private static bool navbarLayoutOpen = true;
    private static bool navbarSeparatorsOpen = true;
    private static bool navbarShadowOpen;
    private static bool topBarOpen;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawSection(ref bottomBarOpen, "Bottom Bar Buttons", "buttons");
        DrawSection(ref panelsOpen, "Panels", "panels", "screenSlideDuration");
        DrawSection(ref playFlowOpen, "Play Flow", "playButton", "loadingSceneName");
        DrawSection(ref animationOpen, "Animation", "animationDuration");
        DrawSection(ref navbarSpritesOpen, "Navbar Sprites", "navbarContainerSprite", "navbarSelectedSprite", "navbarIconSprites", "navbarSeparatorSprite");
        DrawSection(ref navbarLabelsOpen, "Navbar Labels", "tabLabels", "navbarLabelFont", "navbarLabelFontSize", "navbarLabelMinFontSize", "navbarLabelAutoSize", "navbarLabelHeight");
        DrawSection(ref navbarIconsOpen, "Navbar Icon Sizes", "navIconDeselectedSize", "navIconSelectedSize", "navIconDeselectedSizeOverrides", "navIconSelectedSizeOverrides");
        DrawSection(ref navbarSelectedOpen, "Navbar Selected Item", "navSelectedBackgroundWidth", "navSelectedBackgroundHeight", "navSelectedBottomBleed", "navSelectedHorizontalInset", "navSelectedBackgroundXOffset", "navSelectedBackgroundTopCropPixels", "navSelectedIconYOffset", "navDeselectedIconYOffset", "navLabelYOffset");
        DrawSection(ref navbarLayoutOpen, "Navbar Layout", "navBarHeight", "navNormalSlotWeight", "navSelectedSlotWeight", "navSideBleed", "navBackgroundSideBleed");
        DrawSection(ref navbarSeparatorsOpen, "Navbar Separators", "navSeparatorWidth", "navSeparatorHeightRatio", "navSeparatorYOffset", "navSeparatorOpacity");
        DrawSection(ref navbarShadowOpen, "Navbar Main Container Shadow", "navbarMainContainerShadowEnabled", "navbarMainContainerShadowColor", "navbarMainContainerShadowSize", "navbarMainContainerShadowSpread", "navbarMainContainerShadowOffsetAngle", "navbarMainContainerShadowOffsetDistance");
        DrawSection(ref topBarOpen, "Top Bar", "topBarRoot", "topBarContainerSprite", "topBarProfileSprite", "topBarSettingsSprite", "topBarPlusSprite", "topBarHeartSprite", "topBarGoldSprite", "topBarButtonSprite", "topBarFont", "autoBuildTopBar", "topBarHeight", "topBarTopOffset");

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawSection(ref bool open, string title, params string[] propertyNames)
    {
        EditorGUILayout.Space(4f);
        open = EditorGUILayout.Foldout(open, title, true, EditorStyles.foldoutHeader);
        if (!open)
            return;

        EditorGUI.indentLevel++;
        foreach (string propertyName in propertyNames)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                EditorGUILayout.PropertyField(property, true);
            }
        }
        EditorGUI.indentLevel--;
    }
}
