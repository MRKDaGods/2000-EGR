//#define MRK_RENDER_TILE_BOUNDS

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MRK {
	public class MRKMap : EGRBehaviour {
		const int EX_N = 2, EX_S = 2, EX_W = 2, EX_E = 2;

		[SerializeField]
		float m_Zoom;
		[SerializeField]
		Vector2d m_CenterLatLng;
		float m_WorldRelativeScale;
		Vector2d m_CenterMercator;
		readonly List<MRKTileID> m_ActiveTileIDs;
		readonly List<MRKTileID> m_SortedTileIDs;
		readonly Dictionary<MRKTileID, MRKTileID> m_SortedToActiveIDs;
		readonly List<MRKTile> m_Tiles;
		int m_InitialZoom;
		int m_AbsoluteZoom;
		[SerializeField]
		Material m_TileMaterial;
		[SerializeField]
		Texture2D m_LoadingTexture;
		[SerializeField]
		Texture2D m_StationaryTexture;
		[SerializeField]
		bool m_AutoInit;
		[SerializeField]
		string m_Tileset;
		[SerializeField]
		float m_TileSize = 100f;
		[SerializeField]
		float[] m_DesiredTilesetEmission;
		IMRKMapController m_MapController;
		bool m_AwaitingMapFullUpdateEvent;

		public event Action OnMapUpdated;
		public event Action<int, int> OnMapZoomUpdated;
		public event Action OnMapFullyUpdated;

		public Vector2d CenterLatLng => m_CenterLatLng;
		public float WorldRelativeScale => m_WorldRelativeScale;
		public int InitialZoom => m_InitialZoom;
		public int AbsoluteZoom => m_AbsoluteZoom;
		public float Zoom => m_Zoom;
		public Material TileMaterial => m_TileMaterial;
		public Texture2D LoadingTexture => m_LoadingTexture;
		public Texture2D StationaryTexture => m_StationaryTexture;
		public string Tileset => m_Tileset;
		public float TileSize => m_TileSize;
		public Vector2d CenterMercator => m_CenterMercator;
		public List<MRKTile> Tiles => m_Tiles;

		public MRKMap() {
			m_ActiveTileIDs = new List<MRKTileID>();
			m_SortedTileIDs = new List<MRKTileID>();
			m_SortedToActiveIDs = new Dictionary<MRKTileID, MRKTileID>();
			m_Tiles = new List<MRKTile>();
		}

		void Start() {
			//m_TileMaterial.SetTexture("_SecTex", m_LoadingTexture);

			if (m_AutoInit) {
				Initialize(m_CenterLatLng, Mathf.FloorToInt(m_Zoom));
			}
		}

		void Update() {
			//wait for velocity to go down
			if (m_AwaitingMapFullUpdateEvent) {
				float sqrMagnitude = m_MapController.GetMapVelocity().sqrMagnitude;
				if (sqrMagnitude <= 0.1f) {
					m_AwaitingMapFullUpdateEvent = false;
					m_AbsoluteZoom = Mathf.Clamp(Mathf.FloorToInt(m_Zoom), 0, 21);

					//raise map full update
					if (OnMapFullyUpdated != null) {
						OnMapFullyUpdated();
                    }

					UpdateScale();
					UpdatePosition();
				}
            }

#if MRK_DEBUG_FETCHER_LOCK
			Debug.Log(MRKTile.FetcherLock.Recursion);
#endif
		}

		public void Initialize(Vector2d center, int zoom) {
			m_AbsoluteZoom = zoom;
			m_Zoom = m_AbsoluteZoom;
			m_InitialZoom = m_AbsoluteZoom;
			m_CenterLatLng = center;

			UpdateTileset();
			UpdateMap(m_CenterLatLng, m_Zoom, true);
		}

		public void UpdateTileset() {
			m_Tileset = EGRSettings.GetCurrentTileset();
		}

		void UpdateScale() {
			RectD referenceTileRect = MRKMapUtils.TileBounds(MRKMapUtils.CoordinateToTileId(m_CenterLatLng, m_AbsoluteZoom));
			m_WorldRelativeScale = m_TileSize / (float)referenceTileRect.Size.x;
		}

		void UpdatePosition() {
			m_CenterMercator = MRKMapUtils.LatLonToMeters(m_CenterLatLng);

			m_ActiveTileIDs.Clear();
			m_SortedTileIDs.Clear();
			m_SortedToActiveIDs.Clear();
			MRKTileID centerTile = MRKMapUtils.CoordinateToTileId(m_CenterLatLng, m_AbsoluteZoom);

			int maxValidTile = (1 << m_AbsoluteZoom) - 1;
			for (int x = (centerTile.X - EX_W); x <= (centerTile.X + EX_E); x++) {
				for (int y = (centerTile.Y - EX_N); y <= (centerTile.Y + EX_S); y++) {
					m_ActiveTileIDs.Add(new MRKTileID(m_AbsoluteZoom, x, y, x < 0 || x > maxValidTile || y < 0 || y > maxValidTile));
				}
			}

			List<MRKTile> buf = new List<MRKTile>();
			foreach (MRKTile tile in m_Tiles) {
				if (!m_ActiveTileIDs.Contains(tile.ID)) {
					//m_TilePool.Free(tile);
					buf.Add(tile);
				}
			}

			foreach (MRKTile tile in buf) {
				tile.OnDestroy();

				//tell the map interface to dispose markers owned by such tiles
				EventManager.BroadcastEvent<EGREventTileDestroyed>(new EGREventTileDestroyed(tile));

				m_Tiles.Remove(tile);
			}

			foreach (MRKTileID id in m_ActiveTileIDs) {
				MRKTileID sortedID = new MRKTileID(0, id.X - centerTile.X, id.Y - centerTile.Y);
				m_SortedTileIDs.Add(sortedID);
				m_SortedToActiveIDs[sortedID] = id;
			}

			m_SortedTileIDs.Sort((x, y) => {
				int sqrMagX = x.Magnitude;
				int sqrMagY = y.Magnitude;

				return sqrMagX.CompareTo(sqrMagY);
			});

			float scaleFactor = Mathf.Pow(2, m_InitialZoom - m_AbsoluteZoom);

			int siblingIdx = 0;
			foreach (MRKTileID tileID in m_SortedTileIDs) {
				MRKTileID realID = m_SortedToActiveIDs[tileID];
				MRKTile tile = m_Tiles.Find(x => x.ID == realID);
				if (tile == null)
					tile = new MRKTile();

				tile.InitTile(this, realID, siblingIdx);

				RectD rect = tile.Rect;
				Vector3 position = new Vector3((float)(rect.Center.x - m_CenterMercator.x) * m_WorldRelativeScale * scaleFactor, 0f,
					(float)(rect.Center.y - m_CenterMercator.y) * m_WorldRelativeScale * scaleFactor);
				tile.Obj.transform.localScale = new Vector3(scaleFactor, scaleFactor, scaleFactor);
				tile.Obj.transform.localPosition = position;
				tile.Obj.transform.SetSiblingIndex(siblingIdx++);

				if (!m_Tiles.Contains(tile))
					m_Tiles.Add(tile);
			}

			transform.position = Vector3.zero;
		}

#if MRK_RENDER_TILE_BOUNDS
		void OnGUI() {
			foreach (MRKTile tile in m_Tiles) {
				RectD r = tile.Rect;

				Vector3 pos = GeoToWorldPosition(MRKMapUtils.MetersToLatLon(r.Min));

				Vector3 spos = Client.ActiveCamera.WorldToScreenPoint(pos); spos.y = Screen.height - spos.y;

				Vector3 pos2 = GeoToWorldPosition(MRKMapUtils.MetersToLatLon(r.Max));

				Vector3 spos2 = Client.ActiveCamera.WorldToScreenPoint(pos2); spos2.y = Screen.height - spos2.y;

				EGRGL.DrawCircle(spos, 10f, Color.blue);
				EGRGL.DrawCircle(spos2, 20f, Color.white);
				EGRGL.DrawLine(spos, spos2, Color.red, 1.5f);

				EGRGL.DrawBox(spos, spos2, Color.magenta, 1.5f);
			}
		}
#endif

		public void UpdateMap(Vector2d latLon, float zoom, bool force = false) {
			if (zoom > 21 || zoom < 0)
				return;

			//zoom didnt change
			if (!force && Math.Abs(m_Zoom - zoom) <= Mathf.Epsilon && !latLon.IsNotEqual(m_CenterLatLng)) return;

			//we are raising the full update event sometime soon
			m_AwaitingMapFullUpdateEvent = true;

			int newZoom = Mathf.FloorToInt(zoom);
			int oldZoom = AbsoluteZoom;
			bool egrMapZoomUpdated = newZoom != oldZoom;
			if (egrMapZoomUpdated) {
				if (OnMapZoomUpdated != null)
					OnMapZoomUpdated(oldZoom, newZoom);
			}

			bool egrMapUpdated = latLon.IsNotEqual(CenterLatLng) || egrMapZoomUpdated || Mathf.Abs(zoom - m_Zoom) > Mathf.Epsilon;

			// Update map zoom, if it has changed.
			if (Math.Abs(m_Zoom - zoom) > Mathf.Epsilon) {
				m_Zoom = zoom;
				//m_AbsoluteZoom = Mathf.FloorToInt(m_Zoom);
			}

			// Compute difference in zoom. Will be used to calculate correct scale of the map.
			float differenceInZoom = m_Zoom - m_InitialZoom;
			bool isAtInitialZoom = (differenceInZoom - 0.0 < Mathf.Epsilon);

			//Update center latitude longitude
			var centerLatitudeLongitude = latLon;
			double xDelta = centerLatitudeLongitude.x;
			double zDelta = centerLatitudeLongitude.y;

			xDelta = xDelta > 0 ? Mathd.Min(xDelta, MRKMapUtils.LATITUDE_MAX) : Mathd.Max(xDelta, -MRKMapUtils.LATITUDE_MAX);
			zDelta = zDelta > 0 ? Mathd.Min(zDelta, MRKMapUtils.LONGITUDE_MAX) : Mathd.Max(zDelta, -MRKMapUtils.LONGITUDE_MAX);

			//Set Center in Latitude Longitude and Mercator.
			m_CenterLatLng = new Vector2d(xDelta, zDelta);
			UpdateScale();
			UpdatePosition();

			//Scale the map accordingly.
			if (Math.Abs(differenceInZoom) > Mathf.Epsilon || isAtInitialZoom) {
				Vector3 sc = Vector3.one * Mathf.Pow(2, differenceInZoom);
				transform.localScale = sc;
			}

			//EGR
			if (egrMapUpdated) {
				if (OnMapUpdated != null)
					OnMapUpdated();
			}
		}

		public Vector2d WorldToGeoPosition(Vector3 realworldPoint) {
			float scaleFactor = Mathf.Pow(2, (InitialZoom - AbsoluteZoom));
			return (transform.InverseTransformPoint(realworldPoint)).GetGeoPosition(m_CenterMercator, m_WorldRelativeScale * scaleFactor);
		}

		public Vector3 GeoToWorldPosition(Vector2d latitudeLongitude) {
			float scaleFactor = Mathf.Pow(2f, InitialZoom - AbsoluteZoom);
			var worldPos = MRKMapUtils.GeoToWorldPosition(latitudeLongitude.x, latitudeLongitude.y, m_CenterMercator, WorldRelativeScale * scaleFactor).ToVector3xz();
			return transform.TransformPoint(worldPos);
		}

		public float GetDesiredTilesetEmission() {
			int idx = (int)EGRSettings.MapStyle;
			if (m_DesiredTilesetEmission.Length > idx)
				return m_DesiredTilesetEmission[idx];

			return 1f;
		}

		public void SetMapController(IMRKMapController controller) {
			m_MapController = controller;
		}
	}
}