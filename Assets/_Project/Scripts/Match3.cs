using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Match3
{
    public class Match3 : MonoBehaviour
    {
        [SerializeField] int width = 8;
        [SerializeField] int height = 8;
        [SerializeField] float cellSize = 1f;
        [SerializeField] Vector3 originPosition = Vector3.zero;
        [SerializeField] bool debug = true;

        [SerializeField] Candy candyPrefab;
        [SerializeField] CandyType[] candyTypes;
        [SerializeField] Ease ease = Ease.InQuad;

        GridSystem2D<GridObject<Candy>> grid;

        InputReader inputReader;
        Vector2Int selectedCandyPos = Vector2Int.one * -1;

        void Awake()
        {
            inputReader = GetComponent<InputReader>();
        }

        void Start()
        {
            InitializeGrid();
            inputReader.Fire += OnSelectCandy;
        }

        void OnDestroy()
        {
            inputReader.Fire -= OnSelectCandy;
        }

        void InitializeGrid()
        {
            grid = GridSystem2D<GridObject<Candy>>.VerticalGrid(width, height, cellSize, originPosition, debug);

            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    CreateCandy(x, y);
                }
            }
        }

        void CreateCandy(int x, int y)
        {
            Candy candy = Instantiate(candyPrefab, grid.GetWorldPositionCenter(x, y), Quaternion.identity, transform);
            candy.SetType(candyTypes[Random.Range(0, candyTypes.Length)]);
            var gridObject = new GridObject<Candy>(grid, x, y);
            gridObject.SetValue(candy);
            grid.SetValue(x, y, gridObject);
        }

        void OnSelectCandy()
        {
            // Perspective Camera
            // var selectedPos = inputReader.SelectedPosition;
            // var gridPos = grid.GetXY(Camera.main.ScreenToWorldPoint(new Vector3(selectedPos.x, selectedPos.y, 10)));

            // Orthographic Camera
            var gridPos = grid.GetXY(Camera.main.ScreenToWorldPoint(inputReader.SelectedPosition));

            if (!IsValidPosition(gridPos) || IsEmptyPosition(gridPos)) return;

            if (selectedCandyPos == gridPos)
            {
                DeselectCandy();
            }
            else if (selectedCandyPos == Vector2Int.one * -1)
            {
                SelectCandy(gridPos);
            }
            else
            {
                StartCoroutine(RunGameLoop(selectedCandyPos, gridPos));
            }
        }

        IEnumerator RunGameLoop(Vector2Int gridPosA, Vector2Int gridPosB)
        {
            yield return StartCoroutine(SwapCandies(gridPosA, gridPosB));

            // Matches?
            List<Vector2Int> matches = FindMatches();

            // Make Candies explode
            yield return StartCoroutine(ExplodeCandies(matches));

            //Make Candies fall
            yield return StartCoroutine(MakeCandiesFall());

            // Fill Empty Spots
            yield return StartCoroutine(FillEmptySpots());

            // Replace empty slot
            DeselectCandy();

            // Is Game over?
            yield return null;
        }

        IEnumerator FillEmptySpots()
        {
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    if (grid.GetValue(x, y) == null)
                    {
                        CreateCandy(x, y);
                        // SFX play sound?
                        yield return new WaitForSeconds(0.1f);
                    }
                }
            }
        }

        IEnumerator MakeCandiesFall()
        {
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    if (grid.GetValue(x, y) == null)
                    {
                        for (var i = y + 1; i < height; i++)
                        {
                            if (grid.GetValue(x, i) != null)
                            {
                                var candy = grid.GetValue(x, i).GetValue();
                                grid.SetValue(x, y, grid.GetValue(x, i));
                                grid.SetValue(x, i, null);
                                candy.transform
                                    .DOLocalMove(grid.GetWorldPositionCenter(x, y), 0.5f)
                                    .SetEase(ease);
                                // SFX play woosh sound
                                yield return new WaitForSeconds(0.1f);
                                break;
                            }
                        }
                    }
                }
            }
        }

        IEnumerator ExplodeCandies(List<Vector2Int> matches)
        {
            //SFX play sound

            foreach (var match in matches)
            {
                var candy = grid.GetValue(match.x, match.y).GetValue();
                grid.SetValue(match.x, match.y, null);

                // ExplodeVFX(match);

                candy.transform.DOPunchScale(Vector3.one * 0.1f, 0.1f, 1, 0.5f);

                yield return new WaitForSeconds(0.1f);

                candy.DestroyCandy();
            }
        }

        List<Vector2Int> FindMatches()
        {
            HashSet<Vector2Int> matches = new();

            // Horizontal
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width - 2; x++)
                {
                    var candyA = grid.GetValue(x, y);
                    var candyB = grid.GetValue(x + 1, y);
                    var candyC = grid.GetValue(x + 2, y);

                    if (candyA == null || candyB == null || candyC == null) continue;

                    if (candyA.GetValue().GetType() == candyB.GetValue().GetType() &&
                        candyB.GetValue().GetType() == candyC.GetValue().GetType())
                    {
                        matches.Add(new Vector2Int(x, y));
                        matches.Add(new Vector2Int(x + 1, y));
                        matches.Add(new Vector2Int(x + 2, y));
                    }
                }
            }

            // Vertical
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height - 2; y++)
                {
                    var candyA = grid.GetValue(x, y);
                    var candyB = grid.GetValue(x, y + 1);
                    var candyC = grid.GetValue(x, y + 2);

                    if (candyA == null || candyB == null || candyC == null) continue;

                    if (candyA.GetValue().GetType() == candyB.GetValue().GetType() &&
                        candyB.GetValue().GetType() == candyC.GetValue().GetType())
                    {
                        matches.Add(new Vector2Int(x, y));
                        matches.Add(new Vector2Int(x, y + 1));
                        matches.Add(new Vector2Int(x, y + 2));
                    }
                }
            }

            return new List<Vector2Int>(matches);
        }

        IEnumerator SwapCandies(Vector2Int gridPosA, Vector2Int gridPosB)
        {
            const float swapDuration = 0.5f;
            var gridObjectA = grid.GetValue(gridPosA.x, gridPosA.y);
            var gridObjectB = grid.GetValue(gridPosB.x, gridPosB.y);

            gridObjectA.GetValue().transform
                .DOLocalMove(grid.GetWorldPositionCenter(gridPosB.x, gridPosB.y), swapDuration)
                .SetEase(ease);
            gridObjectB.GetValue().transform
                .DOLocalMove(grid.GetWorldPositionCenter(gridPosA.x, gridPosA.y), swapDuration)
                .SetEase(ease);

            grid.SetValue(gridPosA.x, gridPosA.y, gridObjectB);
            grid.SetValue(gridPosB.x, gridPosB.y, gridObjectA);

            yield return new WaitForSeconds(swapDuration);
        }

        void DeselectCandy() => selectedCandyPos = new Vector2Int(-1, -1);

        void SelectCandy(Vector2Int gridPos) => selectedCandyPos = gridPos;


        // Init Grid

        // Read player input and swap gems

        // Start coroutine:
        // Swap animation
        // Matches?
        // Make Gems Explode
        // Replace empty spot
        // Is Game Over?

        bool IsValidPosition(Vector2Int gridPosition) => gridPosition.x >= 0 && gridPosition.x < width &&
                                                         gridPosition.y >= 0 && gridPosition.y < height;

        bool IsEmptyPosition(Vector2Int gridPosition) => grid.GetValue(gridPosition.x, gridPosition.y) == null;
    }
}