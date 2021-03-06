﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public abstract class TileComponent3d: MonoBehaviour
{
    [HideInInspector]
    public Vector3Int tilePos = Vector3Int.zero;

    public abstract void Burn();
    public abstract void Wet();
}

public class Water : TileComponent3d
{
    [SerializeField] private TileBase iceTile = null;
    [SerializeField] private TileBase waterTile = null;

    private Collider m_collider = null;
    private bool m_isIce = false;

    public void ResetTile() {
        WorldGenerator.instance.TileMap.SetTile( tilePos, waterTile );
    }

    private void OnTriggerEnter( Collider other ) {
        if ( m_isIce ) return;

        var player = other.GetComponent<PlayerController>();
        if ( player.PlayerType != PlayerType.Water ) return;

        Freeze();
    }

    private void OnTriggerExit( Collider other ) {
        if ( other.GetComponent<PlayerController>().PlayerType != PlayerType.Fire )
            return;

        Thaw();
    }

    private void Awake() {
        m_collider = GetComponent<Collider>();
    }

    private void Update() {
        if ( PlayerController.activePlayer == null ) return;

        /*
        bool canFreeze = PlayerController.activePlayer.PlayerType == PlayerType.Water 
            && WorldGenerator.instance.CanCast;
        m_collider.isTrigger = m_isIce || canFreeze;
        */

        m_collider.isTrigger = m_isIce;
    }

    public override void Burn() {
        Thaw();
    }

    public override void Wet() {
        Freeze();
    }

    private void Freeze() {
        WorldGenerator.instance.TileMap.SetTile( tilePos, iceTile );
        m_isIce = true;
    }

    private void Thaw() {
        WorldGenerator.instance.TileMap.SetTile( tilePos, waterTile );
        m_isIce = false;
    }
}
