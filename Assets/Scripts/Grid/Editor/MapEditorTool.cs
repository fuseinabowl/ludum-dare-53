using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEditor.EditorTools;
using Unity.Profiling;

using CarbideFunction.Wildtile;

namespace CarbideFunction.Wildtile.Editor
{

/// <summary>
/// This class is a <see href="https://docs.unity3d.com/ScriptReference/EditorTools.EditorTool.html">Unity Tool</see> that allows the user to visually edit <seealso cref="GridPlacer"/> maps. It affects the data in <seealso cref="VoxelGrid"/>.
///
/// Opening this tool will replace the selected GridPlacer's models with a simple voxel grid that can be edited by the user. <see href="~/articles/editing_grids.md">See the manual pages for information on how to use the tool.</see>
/// </summary>
[EditorTool("Tile Map Editor", typeof(IrregularGrid))]
public class MapEditorTool : EditorTool
{
    private GUIContent iconContent;

    private static MapEditorTool toolInstance = null;

    private void OnEnable()
    {
        Assert.IsNull(toolInstance);
        toolInstance = this;

        iconContent = new GUIContent{
            text = "Map Editor",
            tooltip = String.Format("Grid editor, for use on {0} components", typeof(IrregularGrid).Name)
        };
    }

    public override GUIContent toolbarIcon => iconContent;

    /// <summary>
    /// Opens the tool if the editor is currently able to do so.
    ///
    /// Calls to this method will be silently ignored if the user hasn't selected a <seealso cref="GridPlacer"/>, or if they have selected more than one object.
    /// </summary>
    [Shortcut("Tools/Voxel Editor", context:null, KeyCode.U)]
    public static void OpenTool()
    {
        if (   (toolInstance?.IsAvailable() ?? false)
            && IsSelectingEditableObject()
        )
        {
            ToolManager.SetActiveTool(toolInstance);
        }
    }

    private static bool IsSelectingEditableObject()
    {
        // Even if selection.count == 1, activeTransform could be null if the selection contained something
        // else that doesn't have a transform e.g. an asset.
        if (Selection.count == 1 && Selection.activeTransform != null)
        {
            if (Selection.activeTransform.gameObject.GetComponent<IrregularGrid>() != null)
            {
                return true;
            }
        }
        return false;
    }

    public override void OnActivated()
    {
        tileMapEditorToolPerfMarker.Begin();
        // TODO activate tool
        tileMapEditorToolPerfMarker.End();
        Undo.undoRedoPerformed += OnUndoRedo;
    }

    private void OnUndoRedo()
    {
        tileMapEditorToolPerfMarker.Begin();
        // TODO restore tiles
        tileMapEditorToolPerfMarker.End();
    }

    public override void OnToolGUI(EditorWindow window)
    {
        tileMapEditorToolPerfMarker.Begin();
        var evt = Event.current;

        if (evt.type == EventType.MouseDown)
        {
            if (evt.button == 0)
            {
                Click(evt);
                evt.Use();
            }
        }

        // prevent the user from accidentally clicking off this tool
        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

        window.Repaint();
        tileMapEditorToolPerfMarker.End();
    }

    private GameObject GetObjectUnderCursor()
    {
        var mousePosition = Event.current.mousePosition;
        var clickedObject = HandleUtility.PickGameObject(mousePosition, selectPrefabRoot:false, ignore:null, filter:GetGridChildren());
        return clickedObject;
    }

    private GameObject[] GetGridChildren()
    {
        var children = new List<GameObject>(CastTarget.transform.childCount);

        for (var childIndex = 0; childIndex < CastTarget.transform.childCount; ++childIndex)
        {
            children.Add(CastTarget.transform.GetChild(childIndex).gameObject);
        }

        return children.ToArray();
    }

    private void Click(Event evt)
    {
        clickPerfMarker.Begin();
        var hit = GetObjectUnderCursor();
        if (hit != null)
        {
            var quadIndex = hit.GetComponent<GridQuadIndexRegister>().quadIndex;
            var toBePlaced = CastTarget.CurrentPaletteOption;

            SavePrefabToSlot(toBePlaced, quadIndex);
            GameObject.DestroyImmediate(hit);
            CastTarget.CreatePrefabInSlot(toBePlaced, quadIndex);
        }
        clickPerfMarker.End();
    }

    private void SavePrefabToSlot(GameObject toBePlaced, int slot)
    {
        var serializedObject = new SerializedObject(CastTarget.GridData);

        var arrayProp = serializedObject.FindProperty(nameof(GridData.matchingOrderPrefabOverrides));
        EnsurePrefabOverrideListSize(arrayProp, slot);
        var quadOverrideInstance = arrayProp.GetArrayElementAtIndex(slot);
        quadOverrideInstance.FindPropertyRelative(nameof(GridData.QuadOverride.prefab)).objectReferenceValue = toBePlaced;

        serializedObject.ApplyModifiedProperties();
    }

    private void EnsurePrefabOverrideListSize(SerializedProperty gridDataOverridesArray, int requiredIndex)
    {
        if (requiredIndex >= gridDataOverridesArray.arraySize)
        {
            gridDataOverridesArray.arraySize = requiredIndex + 1;
        }
    }

    public override void OnWillBeDeactivated()
    {
        tileMapEditorToolPerfMarker.Begin();
        Undo.undoRedoPerformed -= OnUndoRedo;
        tileMapEditorToolPerfMarker.End();
    }

    private IrregularGrid CastTarget => target as IrregularGrid;

    private static readonly ProfilerMarker tileMapEditorToolPerfMarker = new ProfilerMarker(ProfilerCategory.Scripts, "TileMapEditorTool.Update");
    private static readonly ProfilerMarker clickPerfMarker = new ProfilerMarker(ProfilerCategory.Scripts, "TileMapEditorTool.Click");
    private static readonly ProfilerMarker undoRedoPerfMarker = new ProfilerMarker(ProfilerCategory.Scripts, "TileMapEditorTool.UndoRedo");
}

}
