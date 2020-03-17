using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace IzFbxTools {

/**
 * 形状情報
 */
sealed class Topology {

	/** 頂点情報 */
	public sealed class Vertex {
		public int index;
		public Vector3 pos, n;
		public HalfEdge edge;					//!< この頂点から始まるHalfEdgeの1つ
		public List<Vertex> samePos = null;		//!< 同一位置にある頂点リスト
	}

	/** 辺情報 */
	public sealed class HalfEdge {
		public Vertex vertex;				//!< 始点
		public HalfEdge pair;				//!< 逆向きのHalfEdge
		public HalfEdge next, prev;			//!< 同じ面を構築する次・前のHalfEdge
		public HalfEdge left;				//!< 同一頂点から伸びる別のHalfEdge。これをたどって自分自身に戻ったとき、同一頂点から伸びるすべてのEdgeをたどったことになる
		public Face face;					//!< このHalfEdgeが囲む面。nullにはならない
	}

	/** 面情報 */
	public sealed class Face {
		public HalfEdge edge;		//!< この面を時計回りに囲むhalfEdgeの1つ
		public Vector3 n;			//!< 法線
		public Vector3 center;		//!< 中央位置
	}

	public List<Vertex> verts;		//!< 頂点リスト
	public List<HalfEdge> edges;	//!< 辺リスト
	public List<Face> faces;		//!< 面リスト

	public Topology(
		Mesh srcMesh,
		float mergeLength		//!< 頂点の結合距離
	) {

		var srcN = srcMesh.normals;
		verts = srcMesh.vertices.Select((a,idx)=>new Vertex(){index=idx,pos=a,n=srcN[idx]}).ToList();
		edges = new List<HalfEdge>();
		faces = new List<Face>();

		var srcTri = new List<int>();
		for (int i=0; i<srcMesh.subMeshCount; ++i)
			srcTri.AddRange( srcMesh.GetTriangles(i) );
		var id2edge = new Dictionary<ulong,HalfEdge>();
		Func<int,int,HalfEdge> getEdge = (i0,i1)=>{
			var idx01 = (ulong)(uint)i0 | ((ulong)(uint)i1<<32);
			var idx10 = (ulong)(uint)i1 | ((ulong)(uint)i0<<32);
			if ( id2edge.TryGetValue(idx01, out var e) ) return e;
			var ret = new HalfEdge();
			id2edge.Add(idx01, ret);
			edges.Add(ret);
			if (verts[i0].edge == null) {
				verts[i0].edge = ret;
				ret.left = ret;
			} else {
				ret.left = verts[i0].edge.left;
				verts[i0].edge.left = ret;
			}
			if ( id2edge.TryGetValue(idx10, out var pair) ) {ret.pair=pair; pair.pair=ret;}
			return ret;
		};
		for (int i=0; i<srcTri.Count; i+=3) {
			var (i0,i1,i2) = (srcTri[i],srcTri[i+1],srcTri[i+2]);
			var (v0,v1,v2) = (verts[i0],verts[i1],verts[i2]);
			var f = new Face();
			faces.Add(f);
			var (e01,e12,e20) = (getEdge(i0,i1),getEdge(i1,i2),getEdge(i2,i0));
			e01.next = e20.prev = e12;
			e12.next = e01.prev = e20;
			e20.next = e12.prev = e01;
			e01.face = e12.face = e20.face = f;
			e01.vertex = v0;
			e12.vertex = v1;
			e20.vertex = v2;
			f.edge = e01;
			f.n = M.cross(v2.pos-v0.pos, v1.pos-v0.pos).normalized;
			f.center = (v0.pos+v1.pos+v2.pos) / 3;
		}

		// 一応バリデートをかけておく
		foreach (var i in verts) if (i.edge.vertex!=i) Debug.LogError("error:" + i.index);
		foreach (var i in edges) {
			if (i.pair!=null && i.pair.pair!=i) Debug.LogError("error");
			if (i.next.prev!=i) Debug.LogError("error");
			if (i.prev.next!=i) Debug.LogError("error");
		}
		foreach (var i in faces) if (i.edge.face!=i) Debug.LogError("error");

		// 同一位置の頂点リスト作成
		var sqrMergeLength = mergeLength * mergeLength;
		for (int i=0; i<verts.Count; ++i) {
			if (verts[i].samePos!=null) continue;
			var p = verts[i].pos;
			List<Vertex> spv = null;
			for (int j=i+1; j<verts.Count; ++j) {
				if (verts[j].samePos!=null) continue;
				if (sqrMergeLength < (verts[j].pos-p).sqrMagnitude) continue;
				if (spv == null) verts[i].samePos = spv = new List<Vertex>(){verts[i]};
				verts[j].samePos = spv;
				spv.Add(verts[j]);
			}
		}
	}

}

}