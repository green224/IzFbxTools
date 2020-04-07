using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace IzFbxTools.Window {

/**
 * 目標を設定するためのタブ
 */
[Serializable] sealed class TargetTab {

	// 目標タイプ
	public enum Mode { Fbx, Mesh, Anim }
    public Mode mode = Mode.Fbx;

	public Mesh tgtMesh = null;					//!< 変換対象のMesh
	public AnimationClip tgtAnim = null;		//!< 変換対象のAnimationClip
	public GameObject tgtGObj = null;			//!< 変換対象のGameObject
	public UnityEngine.Object target {get{		//!< 対象をObjectで受け取る
		switch (mode) {
			case Mode.Mesh: return tgtMesh;
			case Mode.Anim: return tgtAnim;
			case Mode.Fbx: return tgtGObj;
			default:throw new SystemException();
		}
	}}

	/** GUI描画処理 */
	public void drawGUI() {
        using (new EditorGUILayout.VerticalScope("box")) {
			using (new EditorGUILayout.HorizontalScope()) {
	//			EditorGUILayout.LabelField( "対象", GUILayout.Width(30) );
				GUILayout.FlexibleSpace();
				mode = (Mode)GUILayout.Toolbar((int)mode, Styles.tabToggles, Styles.tabButtonStyle, Styles.tabButtonSize);
				GUILayout.FlexibleSpace();
			}

			switch (mode) {
				case Mode.Mesh:
					tgtMesh = EditorGUILayout.ObjectField( tgtMesh, typeof( Mesh ), false ) as Mesh;
					break;
				case Mode.Anim:
					tgtAnim = EditorGUILayout.ObjectField( tgtAnim, typeof( AnimationClip ), false ) as AnimationClip;
					break;
				case Mode.Fbx:
					tgtGObj = EditorGUILayout.ObjectField( tgtGObj, typeof( GameObject ), false ) as GameObject;
					break;
				default:throw new SystemException();
			}
		}
	}

	/** GUIStyle定義 */
	static class Styles {
		public static GUIContent[] tabToggles => _tabToggles ?? ( _tabToggles = new [] {"FBX", "Mesh", "Anim"}.Select(x => new GUIContent(x)).ToArray() );
		public static readonly GUIStyle tabButtonStyle = "LargeButton";
		public static readonly GUI.ToolbarButtonSize tabButtonSize = GUI.ToolbarButtonSize.Fixed;

		static GUIContent[] _tabToggles = null;
	}

}

}