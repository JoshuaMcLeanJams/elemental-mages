﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Linq;
using TMPro;
using JoshuaMcLean;

public class WorldGenerator : MonoBehaviour
{
    static public WorldGenerator instance = null;

    [SerializeField] private Tilemap m_tileMap = null;

    [Header( "Levels" )]
    [SerializeField] private int m_startLevelIndex = 0;
    [SerializeField] private List<Tilemap> m_levelTileMaps = new List<Tilemap>();

    [Header("Prefabs")]
    [SerializeField] private GameObject m_exitPrefab = null;
    [SerializeField] private GameObject m_firePrefab = null;
    [SerializeField] private GameObject m_treePrefab = null;
    [SerializeField] private GameObject m_waterPrefab = null;

    [Header( "Game Balance" )]
    [SerializeField] private float m_firePropogationTimeSec = 0.5f;

    [SerializeField, Tooltip("In addition to propogation time") ]
    private float m_fireBurnDownTimeSec = 0.5f;

    [Header( "UI" )]
    [SerializeField] private TextMeshProUGUI m_mageInfoTextMesh = null;

    public string MageInfo {
        set {
            if ( m_mageInfoTextMesh == null ) return;
            m_mageInfoTextMesh.text = value;
        }
    }

    public Tilemap TileMap {  get { return m_tileMap; } }

    public float FireBurnDownTimeSecTotal { get { return m_firePropogationTimeSec + m_fireBurnDownTimeSec; } }

    private GameObject[,] m_tileObjects = null;
    private int m_levelIndex = 0;

    public void NextLevel() {
        Debug.Log( "Level complete!" );
        ++m_levelIndex;
        StartLevel();
        // TODO
    }

    private void StartLevel() {
        for ( var x = 0; x < m_tileMap.size.x; ++x ) {
            for ( var y = 0; y < m_tileMap.size.y; ++y ) {
                if ( m_tileObjects[x, y] == null ) continue;

                Destroy( m_tileObjects[x, y].gameObject );
                m_tileObjects[x, y] = null;
            }
        }

        m_tileMap.gameObject.SetActive( false );
        if ( m_levelIndex >= m_levelTileMaps.Count ) {
            m_tileMap = null;
            return;
        }

        m_tileMap = m_levelTileMaps[m_levelIndex];
        m_tileMap.gameObject.SetActive( true );

        PlayerController.DisableAll();
        UpdateTileObjects( true );
    }

    private TileComponent3d GetTileComponent3D( Vector2Int a_tilePos ) {
        var tilePos = new Vector3Int( a_tilePos.x, a_tilePos.y, 0 );
        var tile = m_tileMap.GetTile( tilePos ) as SpecialTile;
        if ( tile == null ) return null;

        var ix = tilePos.x - m_tileMap.origin.x;
        var iy = tilePos.y - m_tileMap.origin.y;
        if ( ( ix < 0 || ix >= m_tileMap.size.x ) || ( iy < 0 || iy >= m_tileMap.size.y ) ) {
            Debug.LogError( "Tile index out of range" );
            return null;
        }

        var tileObj = m_tileObjects[ix, iy];
        if ( tileObj == null ) return null;

        TileComponent3d tileComp = null;
        if ( tile.TileType == TileType.Exit ) tileComp = tileObj.GetComponentInChildren<Exit>();
        else if ( tile.TileType == TileType.Fire ) tileComp = tileObj.GetComponentInChildren<Fire>();
        else if ( tile.TileType == TileType.Tree ) tileComp = tileObj.GetComponentInChildren<Plant>();
        else if ( tile.TileType == TileType.Water ) tileComp = tileObj.GetComponentInChildren<Water>();

        return tileComp;
    }

    private List<TileComponent3d> GetAdjacentTiles(Vector2Int a_centerTilePos ) {
        var list = new List<TileComponent3d>();

        for ( var x = -1; x <= 1; ++x ) {
            if ( x == 0 ) continue;
            var tilePos = a_centerTilePos;
            tilePos.x += x;
            var tileComp = GetTileComponent3D( tilePos );
            if ( tileComp != null ) list.Add( tileComp );
        }

        for ( var y = -1; y <= 1; ++y ) {
            if ( y == 0 ) continue;
            var tilePos = a_centerTilePos;
            tilePos.y += y;
            var tileComp = GetTileComponent3D( tilePos );
            if ( tileComp != null ) list.Add( tileComp );
        }

        return list;
    }

    private void Awake() {
        instance = this;
    }

    private void Start() {
        foreach ( var tileMap in m_levelTileMaps )
            tileMap.gameObject.SetActive( false );

        m_tileObjects = new GameObject[m_tileMap.size.x, m_tileMap.size.y];
        m_levelIndex = m_startLevelIndex;
        StartLevel();
    }

    void Update() {
        if ( m_tileMap == null ) return;

        UpdateTileObjects();
        UpdateFirePropogation();

        Debug.Log( $"{PlayerController.ActiveMageCount} mages still active" );
        if ( PlayerController.ActiveMageCount == 0 )
            NextLevel();

        var mageInfo = "";
        foreach ( var mage in PlayerController.ActiveMageList ) {
            if ( mage == PlayerController.activePlayer )
                mageInfo += "<u>";
            mageInfo += $"<color={mage.ColorString}>{mage.Mana}</color> ";
            if ( mage == PlayerController.activePlayer )
                mageInfo += "</u>";
        }
        MageInfo = mageInfo;
    }

    private void PropogateFire( int a_x, int a_y ) {
        var absolutePos = new Vector2Int( a_x + m_tileMap.origin.x, a_y + m_tileMap.origin.y );
        var adjacentTiles = GetAdjacentTiles( absolutePos );
        foreach ( var tile in adjacentTiles ) {
            var tree = tile as Plant;
            if ( tree == null ) continue;
            tree.Burn();
        }
    }

    private void UpdateFirePropogation() {
        for( var x = 0; x < m_tileMap.size.x; ++x ) {
            for ( var y = 0; y < m_tileMap.size.y; ++y ) {
                var tileObj = m_tileObjects[x, y];
                if ( tileObj == null ) continue;

                var centerTree = tileObj.GetComponentInChildren<Plant>();
                if ( centerTree == null ) {
                    var fire = tileObj.GetComponentInChildren<Fire>();
                    if( fire == null || fire.FireState == FireState.Out ) continue;
                } else if ( centerTree.PlantState != PlantState.Burning 
                    || centerTree.BurnTime < m_firePropogationTimeSec )
                    continue;

                PropogateFire( x, y );
            }
        }
    }

    private GameObject CreateTileObject( Vector3Int a_tilePos, TileType a_type, bool m_includeStartPositions = false ) {
        if( m_includeStartPositions ) {
            var worldPos = TileToWorldPos( a_tilePos );
            switch ( a_type ) {
                case TileType.StartAir:
                    PlayerController.ActivateMage( PlayerType.Air, worldPos );
                    return null;
                case TileType.StartEarth:
                    PlayerController.ActivateMage( PlayerType.Earth, worldPos );
                    return null;
                case TileType.StartFire:
                    PlayerController.ActivateMage( PlayerType.Fire, worldPos );
                    return null;
                case TileType.StartWater:
                    PlayerController.ActivateMage( PlayerType.Water, worldPos );
                    return null;
            }
        }

        switch( a_type ) {
            case TileType.Exit: return CreateTileObject<Exit>( a_tilePos, m_exitPrefab );
            case TileType.Fire: return CreateTileObject<Fire>( a_tilePos, m_firePrefab );
            case TileType.Tree: return CreateTileObject<Plant>( a_tilePos, m_treePrefab );
            case TileType.Water: return CreateTileObject<Water>( a_tilePos, m_waterPrefab );
            default: return null;
        }
    }

    private void UpdateTileObjects( bool a_initial = false ) {
        var startPos = m_tileMap.origin;
        var endPos = m_tileMap.origin + m_tileMap.size;
        var tilePos = startPos;
        for ( tilePos.x = startPos.x; tilePos.x <= endPos.x; ++tilePos.x ) {
            for ( tilePos.y = startPos.y; tilePos.y <= endPos.y; ++tilePos.y ) {
                var tile = m_tileMap.GetTile( tilePos ) as SpecialTile;
                if ( tile == null ) continue;

                GameObject go = CreateTileObject( tilePos, tile.TileType, a_initial );
                if ( go == null ) continue;

                var ix = tilePos.x - m_tileMap.origin.x;
                var iy = tilePos.y - m_tileMap.origin.y;
                m_tileObjects[ix, iy] = go;
                //Debug.Log( $"New {tile.TileType} at {tilePos} ({ix}, {iy})" );
            }
        }
    }

    private bool CheckChanged<T>( Vector3Int a_tilePos ) where T: TileComponent3d {
        var ix = a_tilePos.x - m_tileMap.origin.x;
        var iy = a_tilePos.y - m_tileMap.origin.y;
        var obj = m_tileObjects[ix, iy];
        if ( obj == null ) return true;

        return obj.GetComponentInChildren<T>() == null;
    }

    private GameObject CreateTileObject<T>( Vector3Int a_tilePos, GameObject a_prefab ) where T: TileComponent3d {
        if ( CheckChanged<T>( a_tilePos ) == false ) return null;

        var worldPos = TileToWorldPos( a_tilePos );
        var go = Instantiate( a_prefab, worldPos, Quaternion.identity );
        var comp = go.GetComponentInChildren<T>();
        comp.tilePos = a_tilePos;

        return go;
    }

    private Vector3 TileToWorldPos( Vector3Int a_tilePos ) {
        return m_tileMap.CellToWorld( a_tilePos ) + new Vector3( 0.5f, 0.5f, -0.5f );
    }
}
