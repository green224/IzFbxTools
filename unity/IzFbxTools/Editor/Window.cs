using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace IzFbxTools {

/**
 * トゥーン用の輪郭線が、ソリッド辺で途切れてしまう問題の対処を行うためのモジュール。
 * 途切れてしまう輪郭線部分に輪郭線用のポリゴンを仕込むことで、辺が途切れないようにする。
 */
sealed class Window : EditorWindow {

	/** GUIStyle定義 */
	static class Styles {
		public static GUIContent[] tabToggles { get {
			return _tabToggles ?? ( _tabToggles = new [] {"Mesh単体", "FBX全体"}.Select(x => new GUIContent(x)).ToArray() );
		} }
		public static readonly GUIStyle tabButtonStyle = "LargeButton";
		public static readonly GUI.ToolbarButtonSize tabButtonSize = GUI.ToolbarButtonSize.Fixed;

		static GUIContent[] _tabToggles = null;
	}

	enum TargetTypeTab {
		SingleMesh,
		FbxOverall,
	}

    TargetTypeTab _tgtTypeTab = TargetTypeTab.SingleMesh;

	Mesh _srcMesh = null;					//!< 変換対象のMesh
	GameObject _srcGObj = null;				//!< 変換対象のGameObject
	float _mergeLength = 0.00001f;			//!< マージ距離
	bool _isCombineMesh = true;				//!< メッシュを一つに結合する
	bool _isOnlyCombineMesh = false;		//!< メッシュ結合のみを行う

	/** 実行できるか否か */
	bool isValidParam {get{
		switch (_tgtTypeTab) {
			case TargetTypeTab.SingleMesh: return _srcMesh != null;
			case TargetTypeTab.FbxOverall: return _srcGObj != null;
			default:throw new SystemException();
		}
	}}

	/** メニューコマンド */
	[MenuItem("Tools/トゥーン輪郭線用 Solid辺融合")]
	static void create() {
		GetWindow<Window>("トゥーン輪郭線用 Solid辺融合");
	}

	/** GUI描画処理 */
	void OnGUI() {
        // タブを描画する
        using (new EditorGUILayout.HorizontalScope()) {
            GUILayout.FlexibleSpace();
            _tgtTypeTab = (TargetTypeTab)GUILayout.Toolbar((int)_tgtTypeTab, Styles.tabToggles, Styles.tabButtonStyle, Styles.tabButtonSize);
            GUILayout.FlexibleSpace();
        }

		switch (_tgtTypeTab) {
			case TargetTypeTab.SingleMesh:{
				_srcMesh = EditorGUILayout.ObjectField( "元Mesh", _srcMesh, typeof( Mesh ), false ) as Mesh;
			}break;
			case TargetTypeTab.FbxOverall:{
				_srcGObj = EditorGUILayout.ObjectField( "元FBX", _srcGObj, typeof( GameObject ), false ) as GameObject;
				_isCombineMesh = EditorGUILayout.Toggle( "メッシュを一つに結合", _isCombineMesh );
				if (_isCombineMesh)
					_isOnlyCombineMesh = EditorGUILayout.Toggle( "メッシュ結合のみを実行", _isOnlyCombineMesh );
			}break;
			default:throw new SystemException();
		}
		_mergeLength = EditorGUILayout.FloatField( "融合距離", _mergeLength );

		using (new EditorGUI.DisabledGroupScope( !isValidParam )) {
			if (GUILayout.Button("実行")) build();
		}

		// 結果ログ
		EditorGUILayout.Space();
		var log = Log.instance;
		if ( log.lineCnt != 0 ) {
			using (new GUILayout.VerticalScope("box")) {
				EditorGUILayout.SelectableLabel(
					log.getResultStr(),
					GUILayout.Height(EditorGUIUtility.singleLineHeight * log.lineCnt)
				);
			}
		}
	}

	/** 出力処理本体 */
	void build() {
		Log.instance.reset();

		switch (_tgtTypeTab) {
			case TargetTypeTab.SingleMesh:{

				// 1メッシュに対してエッジ融合処理を行う
				procOneMeshEdgeMerge(_srcMesh, _mergeLength);

			}break;
			case TargetTypeTab.FbxOverall:{

				// 出力先を読み込む
//				Debug.Log(PrefabUtility.GetPrefabType( _srcGObj ));throw new SystemException();
//				var srcPath = AssetDatabase.GetAssetPath( _srcGObj );
//				var dstObj = PrefabUtility.LoadPrefabContents( srcPath );
//				PrefabUtility.UnpackPrefabInstance(dstObj, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
				var dstObj = Instantiate( _srcGObj );
				var name = dstObj.name;
				dstObj.name = name.Substring(0, name.Length - 7);

				// プレファブ全体に対してメッシュ結合を行う
				if (_isCombineMesh) {
					var dstMesh = getDstMesh( getDstPath(_srcGObj, ".asset") );
					var dstMeshObj = new MeshCombiner.MeshObject() {mesh = dstMesh};
					dstMeshObj.reset();
					MeshCombiner.combine(dstObj, dstMeshObj);
				}

				// プレファブ全体に対してエッジ融合処理を行う
				if (!_isCombineMesh || !_isOnlyCombineMesh) {
					var dstMeshes = MeshComponentWrapper.getMeshComponentsInChildren(dstObj);
					foreach (var i in dstMeshes) {
						var dstMesh = procOneMeshEdgeMerge(i.mesh, _mergeLength);
						i.mesh = dstMesh;
					}
				}

				// 出力
				var dstPath = getDstPath(_srcGObj, ".prefab");
				PrefabUtility.SaveAsPrefabAsset(dstObj, dstPath, out var success);
				DestroyImmediate(dstObj);
				if (!success) throw new SystemException();

			}break;
			default:throw new SystemException();
		}

		AssetDatabase.SaveAssets();
	}

	/** 出力先パスを決定する */
	static string getDstPath(UnityEngine.Object srcObj, string ext) {
		var srcPath = AssetDatabase.GetAssetPath( srcObj );
		var srcName = srcObj.name;
		return
			srcPath.Substring(
				0,
				srcPath.Length - System.IO.Path.GetExtension(srcPath).Length
			) + " (EdgeMerged)" + srcName + ext;
	}

	// Meshの出力先を読み込む
	static Mesh getDstMesh( Mesh srcMesh ) => getDstMesh( getDstPath(srcMesh, ".asset") );
	static Mesh getDstMesh( string path ) {
		var ret = AssetDatabase.LoadAssetAtPath(path, typeof(Mesh)) as Mesh;
		if (ret == null) {
			ret = new Mesh();
			AssetDatabase.CreateAsset( ret, path );
		}

		return ret;
	}

	/** 1メッシュに対してエッジ融合処理を行う */
	static Mesh procOneMeshEdgeMerge(Mesh srcMesh, float mergeLength) {
		var dstMesh = getDstMesh(srcMesh);
		EdgeMerger.proc( srcMesh, dstMesh, mergeLength );
		return dstMesh;
	}

}

}