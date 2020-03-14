using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace ToonMeshEdgeMerger {

/**
 * トゥーン用の輪郭線が、ソリッド辺で途切れてしまう問題の対処を行うためのモジュール。
 * 途切れてしまう輪郭線部分に輪郭線用のポリゴンを仕込むことで、辺が途切れないようにする。
 */
sealed class Window : EditorWindow {

	Mesh _srcMesh = null;					//!< 変換対象のMesh
	float _mergeLength = 0.00001f;			//!< マージ距離
	LogBuilder _log = new LogBuilder();		//!< 結果ログ

	/** メニューコマンド */
	[MenuItem("Tools/トゥーン輪郭線用 Solid辺融合")]
	static void create() {
		GetWindow<Window>("トゥーン輪郭線用 Solid辺融合");
	}

	/** GUI描画処理 */
	void OnGUI() {
		_srcMesh = EditorGUILayout.ObjectField( "元Mesh", _srcMesh, typeof( Mesh ), false ) as Mesh;
		_mergeLength = EditorGUILayout.FloatField( "融合距離", _mergeLength );

		using (new EditorGUI.DisabledGroupScope(_srcMesh == null)) {
			if (GUILayout.Button("実行")) {
				build();
			}
		}

		// 結果ログ
		EditorGUILayout.Space();
		if ( _log.lineCnt != 0 ) {
			using (new GUILayout.VerticalScope("box")) {
				EditorGUILayout.SelectableLabel(
					_log.getResultStr(),
					GUILayout.Height(EditorGUIUtility.singleLineHeight * _log.lineCnt)
				);
			}
		}
	}

	/** 出力処理本体 */
	void build() {
		_log.reset();

		// 出力先パスを決定
		var dstMeshPath = AssetDatabase.GetAssetPath( _srcMesh );
		dstMeshPath =
			dstMeshPath.Substring(
				0,
				dstMeshPath.Length - System.IO.Path.GetExtension(dstMeshPath).Length
			) + " (EdgeMerged)" + _srcMesh.name + ".asset";

		// 出力先を読み込む
		var dstMesh = AssetDatabase.LoadAssetAtPath(dstMeshPath, typeof(Mesh)) as Mesh;
		if (dstMesh == null) {
			dstMesh = new Mesh();
			AssetDatabase.CreateAsset( dstMesh, dstMeshPath );
		}
		MeshCloner.clone(_srcMesh, dstMesh);
		_log.beginOneProc(_srcMesh, dstMesh);

		// 形状情報を生成
		var topology = new Topology(dstMesh, _mergeLength);

		// コーナー情報の収集用処理
		var cornerList = new Dictionary<List<Topology.Vertex>, List<Topology.HalfEdge>>();
		Action<List<Topology.Vertex>, Topology.HalfEdge> addCorner = (v,e) => {
			if ( cornerList.TryGetValue(v, out var eLst) )	eLst.Add( e );
			else											cornerList.Add( v, new List<Topology.HalfEdge>(){e} );
		};
		Action<Topology.HalfEdge, Topology.HalfEdge, Topology.HalfEdge> addCorner2 = (tgt,e0,e1) => {
			addCorner(tgt.vertex.samePos, e0); addCorner(tgt.vertex.samePos, e1);
			addCorner(tgt.next.vertex.samePos, e0); addCorner(tgt.next.vertex.samePos, e1);
		};

		// エッジ部分の輪郭線を補強する
		var newTris = dstMesh.GetTriangles(0).ToList();
		var tgtEdges = new LinkedList<Topology.HalfEdge>(topology.edges);
		for (var i=tgtEdges.First; i!=null; i=i.Next) {
			var e = i.Value;
			if (e.pair!=null) continue;		// 一番端のエッジのみ
			if (e.vertex.samePos==null || e.next.vertex.samePos==null) continue;		// 同じ位置に別の頂点がある必要がある

			// 同じ位置に別のエッジがある必要がある
			Topology.HalfEdge otherEdge = null;
			foreach (var k in e.next.vertex.samePos) {
				if (k==e.next.vertex) continue;

				for (var j=k.edge; true;) {
					if (j.next.vertex.samePos?.Contains(e.vertex) ?? false) { otherEdge=j; break; }
					j=j.left;
					if (j==k.edge) break;
				}
				if (otherEdge != null) break;
			}
			if (otherEdge == null) continue;
			
			// 頂点ごとの法線が、山折りであるか谷折りであるかをチェック
			var edgeN = (e.next.vertex.pos - e.vertex.pos).normalized;
			var t0 = M.cross(edgeN, M.cross(e.face.center-e.vertex.pos, edgeN)).normalized;
			var t1 = M.cross(edgeN, M.cross(otherEdge.face.center-e.vertex.pos, edgeN)).normalized;
			var s0 = M.cross(edgeN, t0);
			Func<Vector3,Vector3,bool> isCrossingCheck = (n0, n1) => {
				var a = new Vector2(M.dot(n0, t0), M.dot(n0, s0));
				var b = new Vector2(M.dot(n1, t0), M.dot(n1, s0));
				return M.isCrossing(
					new M.HalfLine( a, new Vector2(1, 0) ),
					new M.HalfLine( b, new Vector2(M.dot(t1, t0), M.dot(t1, s0)) )
				);
			};

			// 山折り部分のエッジである必要がある。谷折り部分のエッジは対象ではない
			var isCrossing0 = isCrossingCheck( e.vertex.n, otherEdge.next.vertex.n );
			var isCrossing1 = isCrossingCheck( e.next.vertex.n, otherEdge.vertex.n );
			if ( isCrossing0 && isCrossing1 ) { addCorner2(e, null,null); continue; }

			{// エッジ部分に、ポリゴンを追加
				var (i0,i1,i2,i3) = (e.vertex.index, e.next.vertex.index, otherEdge.vertex.index, otherEdge.next.vertex.index);
				if (!isCrossing0) { newTris.Add(i0); newTris.Add(i3); newTris.Add(i2); }
				if (!isCrossing1) { newTris.Add(i0); newTris.Add(i2); newTris.Add(i1); }
			}

			// 接続先を候補から除外
			tgtEdges.Remove(otherEdge);
			addCorner2( e, e, otherEdge );
			_log.countMergedEdge();
		}

		// コーナー部分の輪郭線を補強
		foreach (var i in cornerList) {

			// コーナーか否か
			if ( i.Value.Count <= 4 ) continue;

			// 山折り部分と谷折り部分が共存しているコーナーについては、
			// 簡単に溶接できるとは限らない。多くの場合、複雑な形状ゆえにポリゴンの追加のみではちゃんと溶接できない。
			// したがってそういう箇所については何もしない
			var isComplex = false;
			foreach (var j in i.Value) {
				if (j != null) continue;
				isComplex = true;
				break;
			}
			if ( isComplex ) continue;

			// コーナーの向きを算出
			var cornerDirZ = Vector3.zero;
			foreach (var j in i.Key) {
				cornerDirZ += j.n;
			}
			cornerDirZ.Normalize();

			// コーナーの向きを軸の一つとする座標系の基底を構築
			var cornerDirX = M.cross(cornerDirZ, new Vector3(1,0,0));
			if (cornerDirX.sqrMagnitude < 0.00001f) {
				cornerDirX = M.cross(cornerDirZ, new Vector3(0,1,0));
			}
			cornerDirX.Normalize();
			var cornerDirY = M.cross(cornerDirZ, cornerDirX);

			// コーナーを構築する頂点を、その方向順に並べる
			var vLst = new List<(float theta, Topology.Vertex v)>();
			foreach (var j in i.Key) {
				var x = M.dot(j.n, cornerDirX);
				var y = M.dot(j.n, cornerDirY);
				var theta = Mathf.Atan2( y, x );
				vLst.Add( (theta, j) );
			}
			vLst = vLst.OrderBy(a => a.theta).ToList();

			// コーナー部分にポリゴンを追加
			for (int j=2; j<vLst.Count; ++j) {
				newTris.Add( vLst[0].v.index );
				newTris.Add( vLst[j-1].v.index );
				newTris.Add( vLst[j].v.index );
			}
			_log.countMergedCorner();
		}

		// 加工して保存
		dstMesh.SetTriangles( newTris, 0 );		// どこでもいいと思うので適当に0番目のサブメッシュに追加
		AssetDatabase.SaveAssets();

		// ログの生成
		_log.endOneProc( true );
	}

}

}