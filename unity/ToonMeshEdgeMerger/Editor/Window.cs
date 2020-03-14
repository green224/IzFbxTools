using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

/**
 * トゥーン用の輪郭線が、ソリッド辺で途切れてしまう問題の対処を行うためのモジュール。
 * 途切れてしまう輪郭線部分に輪郭線用のポリゴンを仕込むことで、辺が途切れないようにする。
 */
sealed class Window : EditorWindow {

	Mesh _srcMesh = null;			//!< 変換対象のMesh
	float _mergeLength = 0.00001f;	//!< マージ距離
	string _log = null;				//!< 結果ログ

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
		if ( _log != null ) EditorGUILayout.TextArea(_log);
	}

	/** 出力処理本体 */
	void build() {

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
		MeshCloner.clone( _srcMesh, dstMesh );

		// 形状情報を生成
		var topology = new Topology(dstMesh, _mergeLength);

		// 輪郭線を補強する必要のあるエッジ一覧を構築
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
//			var t = cross(edgeN, cross(e.face.center-e.vertex.pos, edgeN)).normalized;
//			var s = cross(edgeN, t);
//			Func<Vector3,float> getAgl = dir => ( Mathf.Atan2(dot(dir, s), dot(dir, t)) + Mathf.PI*2 ) % (Mathf.PI*2);
//			var agl00 = getAgl(e.vertex.n);
//			var agl01 = getAgl(e.next.vertex.n);
//			var agl10 = getAgl(otherEdge.vertex.n);
//			var agl11 = getAgl(otherEdge.next.vertex.n);
//			var isYamaori0 = 0.01f < agl11 - agl00;
//			var isYamaori1 = 0.01f < agl10 - agl01;
//
//			// 山折り部分のエッジである必要がある。谷折り部分のエッジは対象ではない
//			if ( !isYamaori0 && !isYamaori1 ) continue;
//
//			{// エッジ部分に、ポリゴンを追加
//				var (i0,i1,i2,i3) = (e.vertex.index, e.next.vertex.index, otherEdge.vertex.index, otherEdge.next.vertex.index);
//				if (isYamaori0) { newTris.Add(i0); newTris.Add(i3); newTris.Add(i2); }
//				if (isYamaori1) { newTris.Add(i0); newTris.Add(i2); newTris.Add(i1); }
//			}

			var t0 = cross(edgeN, cross(e.face.center-e.vertex.pos, edgeN)).normalized;
			var t1 = cross(edgeN, cross(otherEdge.face.center-e.vertex.pos, edgeN)).normalized;
			var s0 = cross(edgeN, t0);
			Func<Vector3,Vector3,bool> isCrossingCheck = (n0, n1) => {
				var a = new Vector2(dot(n0, t0), dot(n0, s0));
				var b = new Vector2(dot(n1, t0), dot(n1, s0));
				return M.isCrossing(
					new M.HalfLine( a, new Vector2(1, 0) ),
					new M.HalfLine( b, new Vector2(dot(t1, t0), dot(t1, s0)) )
				);
			};

			var isCrossing0 = isCrossingCheck( e.vertex.n, otherEdge.next.vertex.n );
			var isCrossing1 = isCrossingCheck( e.next.vertex.n, otherEdge.vertex.n );

			// 山折り部分のエッジである必要がある。谷折り部分のエッジは対象ではない
			if ( isCrossing0 && isCrossing1 ) continue;

			{// エッジ部分に、ポリゴンを追加
				var (i0,i1,i2,i3) = (e.vertex.index, e.next.vertex.index, otherEdge.vertex.index, otherEdge.next.vertex.index);
				if (!isCrossing0) { newTris.Add(i0); newTris.Add(i3); newTris.Add(i2); }
				if (!isCrossing1) { newTris.Add(i0); newTris.Add(i2); newTris.Add(i1); }
			}

			// 接続先を候補から除外
			tgtEdges.Remove(otherEdge);
		}

		// 加工して保存
		dstMesh.SetTriangles( newTris, 0 );		// どこでもいいと思うので適当に0番目のサブメッシュに追加
		AssetDatabase.SaveAssets();
	}

	static float dot(Vector3 a,Vector3 b) => a.x*b.x + a.y*b.y + a.z*b.z;
	static Vector3 cross(Vector3 a,Vector3 b) => new Vector3(
		a.y*b.z - a.z*b.y,
		a.z*b.x - a.x*b.z,
		a.x*b.y - a.y*b.x
	);

}
