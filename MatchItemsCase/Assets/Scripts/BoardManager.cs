using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

public class BoardManager : MonoBehaviour
{
    [SerializeField] private Transform _BlocksParent;
    [SerializeField, ReadOnly] private int _MinNumberToBlast = 2;
    [SerializeField] [Range(2, 10)] private int _RowCount;
    [SerializeField] [Range(2, 10)] private int _ColumnCount;
    [SerializeField] [Range(1, 6)] private int _ColorNumber;
    [SerializeField] private Vector2Int _TileSize;
    [SerializeField] private Vector2Int _StartPos;
    [SerializeField] private int _ConditionACount;
    [SerializeField] private int _ConditionBCount;
    [SerializeField] private int _ConditionCCount;
    [SerializeField] private float _DelayBetweenBoard;
    [SerializeField] private float _TileMinScaleFactor;
    [SerializeField] private float _TileCreationTime;
    [SerializeField] private float _TileRemoveTime;
    [SerializeField] private float _TileFallSpeed;
    [SerializeField] private float _TileFallWaitTime;
    [SerializeField] private float _BoardTopRefillWaitDuration;
    [SerializeField] private List<Tiles> _BlockPrefabs = new List<Tiles>();

    private Tiles[,] _tiles;

    private bool _isInputAllowed;

    private void Awake()
    {
        CreateBoard(false);
        _isInputAllowed = true;
    }

    private void CreateBoard(bool isAnimated)
    {
        _tiles = new Tiles[_RowCount, _ColumnCount];

        for (var x = 0; x < _RowCount; x++)
        {
            for (var y = 0; y < _ColumnCount; y++)
            {
                CreateRandomBlockAtPos(x, y, isAnimated);
            }
        }

        EvaluateBoard();
    }

    private void CreateRandomBlockAtPos(int x, int y, bool isAnimated)
    {
        var newBlock = GetRandomBlock();
        _tiles[x, y] = newBlock;

        newBlock.GridPos = new Vector2Int(x, y);
        newBlock.transform.localPosition = GetLocalPosForGridPos(x, y);
        newBlock.DidGetClicked += OnBlockClick;

        if (!isAnimated) return;
        newBlock.transform.localScale = Vector3.one * _TileMinScaleFactor;
        LeanTween.scale(newBlock.gameObject, Vector3.one, _TileCreationTime);
    }

    private Tiles GetRandomBlock()
    {
        var randomBlockPrefab = _BlockPrefabs[Random.Range(0, _ColorNumber)];
        var newObject = Instantiate(randomBlockPrefab, _BlocksParent, false);
        return newObject;
    }

    private Vector2 GetLocalPosForGridPos(int x, int y)
    {
        return new Vector2((_TileSize.x * x) + _StartPos.x, (_TileSize.y * y) + _StartPos.y);
    }

    private void OnBlockClick(Vector2Int gridPos)
    {
        if (!_isInputAllowed) return;

        var matchingBlocks = FloodFill(gridPos.x, gridPos.y);

        if (matchingBlocks.Count < 2) return;

        _isInputAllowed = false;
        foreach (var block in matchingBlocks)
        {
            EraseBlock(block.GridPos, true);
        }

        StartCoroutine(AfterBlockRemovalRoutine());
    }

    private IEnumerator AfterBlockRemovalRoutine()
    {
        yield return new WaitForSeconds(_TileRemoveTime);

        DropAllBlocks();

        yield return new WaitForSeconds(_TileFallWaitTime);

        RefillTheBoard();

        yield return new WaitForSeconds(_BoardTopRefillWaitDuration);

        EvaluateBoard();
    }

    private void EraseBlock(Vector2Int gridPos, bool isAnimated)
    {
        void DestroyBlock()
        {
            _tiles[gridPos.x, gridPos.y].DidGetClicked -= OnBlockClick;
            Destroy(_tiles[gridPos.x, gridPos.y].gameObject);
            _tiles[gridPos.x, gridPos.y] = null;
        }

        if (isAnimated)
        {
            LeanTween.scale(_tiles[gridPos.x, gridPos.y].gameObject, Vector3.one * _TileMinScaleFactor, _TileRemoveTime).setOnComplete(DestroyBlock);
        }
        else DestroyBlock();
    }

    private void DropAllBlocks()
    {
        for (var x = 0; x < _RowCount; x++)
        {
            for (var y = 0; y < _ColumnCount; y++)
            {
                if (_tiles[x, y] != null) continue;

                DropToEmptySpace(x, y);
                break;
            }
        }
    }
    private void DropToEmptySpace(int posX, int posY)
    {
        var nullCount = 1;

        for (var y = posY + 1; y < _ColumnCount; y++)
        {
            var block = _tiles[posX, y];

            if (block == null)
            {
                nullCount++;
            }
            else
            {
                var newYPos = y - nullCount;
                _tiles[posX, newYPos] = block;
                _tiles[posX, y] = null;

                block.GridPos = new Vector2Int(posX, newYPos);

                var blockLocalPos = GetLocalPosForGridPos(posX, newYPos);

                var distance = Vector2.Distance(block.transform.localPosition, blockLocalPos);
                var duration = distance / _TileFallSpeed;
                LeanTween.moveLocal(block.gameObject, blockLocalPos, duration);
            }
        }
    }

    private void RefillTheBoard()
    {
        for (var x = 0; x < _RowCount; x++)
        {
            for (var y = 0; y < _ColumnCount; y++)
            {
                var tile = _tiles[x, y];

                if (!tile)
                {
                    CreateRandomBlockAtTopFromGridPos(x, y);
                }
            }
        }
    }
    private void CreateRandomBlockAtTopFromGridPos(int x, int y)
    {
        var newBlock = GetRandomBlock();
        _tiles[x, y] = newBlock;

        newBlock.GridPos = new Vector2Int(x, y);
        var finalLocalPos = GetLocalPosForGridPos(x, y);
        var firstLocalPos = finalLocalPos + new Vector2(0, _TileSize.y * ((_ColumnCount - y) + (_ColumnCount / 2)));
        newBlock.transform.localPosition = firstLocalPos;
        newBlock.DidGetClicked += OnBlockClick;

        var distance = Vector2.Distance(firstLocalPos, finalLocalPos);
        var duration = distance / _TileFallSpeed;
        LeanTween.moveLocal(newBlock.gameObject, finalLocalPos, duration);
    }

    private void EvaluateBoard()
    {
        StartCoroutine(CheckMatchingBlocks());
    }

    private IEnumerator CheckMatchingBlocks()
    {
        var isAnyMatchOnRightFirst = RightFirst();
        var isAnyMatchOnUpFirst = UpFirst();

        if (!(isAnyMatchOnRightFirst || isAnyMatchOnUpFirst))
        {
            RecreateBoard();
        }

        yield return new WaitForSeconds(.5f);

        _isInputAllowed = true;
    }

    private bool RightFirst()
    {
        var sameBlocks = new List<Tiles>();
        var isAnyMatch = false;

        for (var y = 0; y < _ColumnCount; y++)
        {
            for (var x = 0; x < _RowCount; x++)
            {
                if (x > _RowCount - _MinNumberToBlast)
                    continue;

                var currentBlock = _tiles[x, y];
                currentBlock.SetMatchGroupType(TileGroupType.Default);

                var isAllMatch = true;
                for (var i = 1; i < _MinNumberToBlast; i++)
                {
                    var blockToCheck = _tiles[x + i, y];

                    var isMatch = currentBlock.Color == blockToCheck.Color;
                    isAllMatch = isMatch;
                    if (!isMatch) break;
                }

                if (isAllMatch)
                {
                    isAnyMatch = true;
                    sameBlocks.Add(currentBlock);

                    var newBlocks = FloodFill(x, y);

                    foreach (var block in newBlocks.Where(block => !sameBlocks.Contains(block)))
                    {
                        sameBlocks.Add(block);
                    }

                    var number = sameBlocks.Count;
                    var matchType = TileGroupType.Default;

                    if (number > _ConditionACount)
                    {
                        matchType = TileGroupType.A;

                        if (number > _ConditionBCount)
                        {
                            matchType = TileGroupType.B;

                            if (number > _ConditionCCount)
                            {
                                matchType = TileGroupType.C;
                            }
                        }
                    }

                    foreach (var block in sameBlocks)
                    {
                        block.SetMatchGroupType(matchType);
                    }

                }

                sameBlocks.Clear();
            }
        }

        return isAnyMatch;
    }

    private bool UpFirst()
    {
        var sameBlocks = new List<Tiles>();
        var isAnyMatch = false;

        for (var x = 0; x < _RowCount; x++)
        {
            for (var y = 0; y < _ColumnCount; y++)
            {
                if (y > _ColumnCount - _MinNumberToBlast)
                    continue;

                var currentBlock = _tiles[x, y];

                var isAllMatch = true;
                for (var i = 1; i < _MinNumberToBlast; i++)
                {
                    var blockToCheck = _tiles[x, y + i];

                    var isMatch = currentBlock.Color == blockToCheck.Color;
                    isAllMatch = isMatch;
                    if (!isMatch) break;
                }

                if (isAllMatch)
                {
                    isAnyMatch = true;

                    sameBlocks.Add(currentBlock);

                    var newBlocks = FloodFill(x, y);

                    foreach (var block in newBlocks.Where(block => !sameBlocks.Contains(block)))
                    {
                        sameBlocks.Add(block);
                    }

                    var number = sameBlocks.Count;
                    var matchType = TileGroupType.Default;

                    if (number > _ConditionACount)
                    {
                        matchType = TileGroupType.A;

                        if (number > _ConditionBCount)
                        {
                            matchType = TileGroupType.B;

                            if (number > _ConditionCCount)
                            {
                                matchType = TileGroupType.C;
                            }
                        }
                    }

                    foreach (var block in sameBlocks)
                    {
                        block.SetMatchGroupType(matchType);
                    }

                }

                sameBlocks.Clear();
            }
        }

        return isAnyMatch;
    }

    private List<Tiles> FloodFill(int x, int y)
    {
        var blockList = new List<Tiles>();

        var initialBlock = GetBlockAtPos(new Vector2Int(x, y));
        var lookupList = new List<Tiles> { initialBlock };

        while (lookupList.Count > 0)
        {
            var lookupPos = lookupList[lookupList.Count - 1].GridPos;
            var lookupBlock = GetBlockAtPos(lookupPos);

            lookupList.Remove(lookupBlock);
            blockList.Add(lookupBlock);

            var neighbors = new List<Tiles>();

            var left = GetBlockAtPos(lookupPos + Vector2Int.left);
            if (left) neighbors.Add(left);

            var right = GetBlockAtPos(lookupPos + Vector2Int.right);
            if (right) neighbors.Add(right);

            var up = GetBlockAtPos(lookupPos + Vector2Int.up);
            if (up) neighbors.Add(up);

            var down = GetBlockAtPos(lookupPos + Vector2Int.down);
            if (down) neighbors.Add(down);

            foreach (var neighbor in neighbors)
            {
                if (lookupList.Contains(neighbor)) continue;
                if (blockList.Contains(neighbor)) continue;
                if (neighbor.Color != lookupBlock.Color) continue;

                lookupList.Add(neighbor);
            }
        }
        return blockList;
    }

    private Tiles GetBlockAtPos(Vector2Int pos)
    {
        if (pos.x < 0) return null;
        if (pos.y < 0) return null;
        if (pos.x >= _RowCount) return null;
        if (pos.y >= _ColumnCount) return null;

        return _tiles[pos.x, pos.y];
    }

    private void RecreateBoard()
    {
        foreach (var block in _tiles)
        {
            EraseBlock(block.GridPos, true);
        }

        StartCoroutine(DoAfter(_DelayBetweenBoard, () => { CreateBoard(true); }));
    }

    private IEnumerator DoAfter(float waitTime, Action callback)
    {
        yield return new WaitForSeconds(waitTime);

        callback?.Invoke();
    }
}