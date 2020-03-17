using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace IzFbxTools.Window {

/**
 * トゥーン用の輪郭線が、ソリッド辺で途切れてしまう問題の対処を行うためのモジュール。
 * 途切れてしまう輪郭線部分に輪郭線用のポリゴンを仕込むことで、辺が途切れないようにする。
 */
sealed class Root : EditorWindow {

    TargetTab _tgtTab = new TargetTab();	//!< 目標タイプのタブ
	float _mergeLength = 0.00001f;			//!< マージ距離
	bool _isCombineMesh = true;				//!< メッシュを一つに結合する
	bool _isOnlyCombineMesh = false;		//!< メッシュ結合のみを行う
	LogViewer _logViewer = new LogViewer();	//!< ログ表示モジュール

	/** 実行できるか否か */
	bool isValidParam {get{
		if (_tgtTab.target==null) return false;
		return true;
	}}

	/** メニューコマンド */
	[MenuItem("Tools/IzFbxTools")]
	static void create() {
		GetWindow<Root>("IzFbxTools");
	}

	/** GUI描画処理 */
	void OnGUI() {
		// タブを描画する
		_tgtTab.drawGUI();

		switch (_tgtTab.mode) {
			case TargetTab.Mode.Mesh:{
			}break;
			case TargetTab.Mode.Fbx:{
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
		_logViewer.drawGUI();
	}

	/** 出力処理本体 */
	void build() {
		Log.instance.reset();

		switch (_tgtTab.mode) {
			case TargetTab.Mode.Mesh:{

				// 1メッシュに対してエッジ融合処理を行う
				procOneMeshEdgeMerge(_tgtTab.tgtMesh, _mergeLength);

			}break;
			case TargetTab.Mode.Fbx:{

				// 出力先を読み込む
//				Debug.Log(PrefabUtility.GetPrefabType( _tgtTab.tgtGObj ));throw new SystemException();
//				var srcPath = AssetDatabase.GetAssetPath( _tgtTab.tgtGObj );
//				var dstObj = PrefabUtility.LoadPrefabContents( srcPath );
//				PrefabUtility.UnpackPrefabInstance(dstObj, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
				var dstObj = Instantiate( _tgtTab.tgtGObj );
				var name = dstObj.name;
				dstObj.name = name.Substring(0, name.Length - 7);

				// プレファブ全体に対してメッシュ結合を行う
				if (_isCombineMesh) {
					var dstMesh = getDstMesh( getDstPath(_tgtTab.tgtGObj, ".asset") );
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
				var dstPath = getDstPath(_tgtTab.tgtGObj, ".prefab");
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
			) + " (Optimized)" + srcName + ext;
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