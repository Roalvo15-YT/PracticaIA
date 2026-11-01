using System;
using System.Collections.Generic;
using Components;
using NavigationDJIA.World;
using UnityEngine;

namespace GrupoA
{
    /// <summary>
    /// Genera un mapa de muros aleatorio cada vez que se ejecuta la escena.
    /// Se ejecuta ANTES que WorldManager.Awake (que construye el WorldInfo leyendo las paredes),
    /// por eso el ExecutionOrder negativo. Las paredes de la escena solo se sustituyen en Play;
    /// la escena guardada no se modifica.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class RandomMapGenerator : MonoBehaviour
    {
        [Tooltip("Arrastra aquí tu Wall (de la escena o un prefab). Se clona una vez por muro, ajustado al tamaño de una celda. " +
                 "Si está vacío se usa la primera pared que haya en la escena (o un cubo si no hay ninguna).")]
        public GameObject wallPrefab;

        [Tooltip("0 = mapa distinto en cada ejecución. Otro valor = siempre el mismo mapa (la semilla usada sale en la consola).")]
        public int seed = 0;

        [Header("Muros (segmentos rectos)")]
        public int minSegments = 8;
        public int maxSegments = 16;
        public int minLength = 3;
        public int maxLength = 8;

        [Header("Restricciones")]
        [Tooltip("Fracción mínima de celdas libres. Si el mapa generado tiene menos, se vuelve a generar.")]
        [Range(0.2f, 0.95f)] public float minFreeFraction = 0.6f;
        [Tooltip("Grosor de cada muro respecto al tamaño de celda (menor que 1 para no tocar las celdas vecinas).")]
        [Range(0.3f, 1f)] public float wallThickness = 0.9f;

        private static readonly (int, int)[] Dirs = { (1, 0), (-1, 0), (0, 1), (0, -1) };

        private void Awake()
        {
            WorldManager manager = FindFirstObjectByType<WorldManager>();
            if (manager == null)
            {
                Debug.LogError("[RandomMapGenerator] No hay ningún WorldManager en la escena.");
                return;
            }

            // WorldInfo temporal: solo para saber el tamaño del tablero y dónde está cada celda.
            var info = new WorldInfo(manager.gameObject, AllowedDirections.FourDirections, manager.worldResolution);
            int w = (int)info.WorldSize.x;
            int h = (int)info.WorldSize.y;
            if (w < 2 || h < 2)
            {
                Debug.LogError($"[RandomMapGenerator] Tamaño de tablero inesperado ({w}x{h}); no se toca nada.");
                return;
            }
            Vector3 origin = info.ToWorldPosition(info[0, 0]);
            float cellSize = Vector3.Distance(origin, info.ToWorldPosition(info[1, 0]));
            if (cellSize < 1e-4f)
            {
                Debug.LogError("[RandomMapGenerator] Tamaño de celda 0; no se toca nada.");
                return;
            }

            // Paredes actuales de la escena.
            Transform parent = manager.transform;
            GameObject firstOld = null;
            var oldWalls = new List<GameObject>();
            foreach (Transform t in manager.GetComponentsInChildren<Transform>(true))
            {
                if (t == manager.transform || !t.CompareTag("Wall")) continue;
                if (firstOld == null) { firstOld = t.gameObject; parent = t.parent; }
                oldWalls.Add(t.gameObject);
            }

            // Plantilla: copia oculta fuera de WorldManager, para que siga existiendo aunque se borre la pared original.
            GameObject source = wallPrefab != null ? wallPrefab : firstOld;
            GameObject template;
            float templateY;
            if (source != null)
            {
                templateY = source.transform.position.y;
                template = Instantiate(source);
            }
            else
            {
                templateY = origin.y + 0.5f;
                template = GameObject.CreatePrimitive(PrimitiveType.Cube);
            }
            template.transform.SetParent(null);
            template.SetActive(false);

            int usedSeed = seed != 0 ? seed : Environment.TickCount;
            bool[,] wall = Generate(w, h, new System.Random(usedSeed));

            // Primero se crean las paredes nuevas; las antiguas solo se borran si todo ha ido bien.
            var created = new List<GameObject>();
            try
            {
                for (int x = 0; x < w; x++)
                {
                    for (int y = 0; y < h; y++)
                    {
                        if (!wall[x, y]) continue;

                        GameObject go = Instantiate(template, parent);
                        created.Add(go);
                        go.SetActive(true);
                        go.name = $"Wall_{x}_{y}";
                        go.tag = "Wall";
                        go.transform.rotation = Quaternion.identity;
                        FitToCell(go, info.ToWorldPosition(info[x, y]), cellSize * wallThickness, templateY);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[RandomMapGenerator] Error al crear las paredes; se conservan las originales. {e}");
                foreach (var c in created) if (c != null) DestroyImmediate(c);
                DestroyImmediate(template);
                return;
            }

            // DestroyImmediate: con Destroy las paredes seguirían existiendo cuando WorldManager construya el WorldInfo.
            foreach (var old in oldWalls)
                if (old != null) DestroyImmediate(old);
            DestroyImmediate(template);
            Physics.SyncTransforms();

            Debug.Log($"[RandomMapGenerator] semilla {usedSeed}: {created.Count} muros, {w * h - created.Count} celdas libres de {w * h}. " +
                      $"Tablero {w}x{h}, celda {cellSize:F2}, origen {origin}.");
            if (created.Count > 0)
            {
                var r0 = created[0].GetComponentInChildren<Renderer>();
                Debug.Log($"[RandomMapGenerator] Primera pared: posición {created[0].transform.position}, " +
                          $"tamaño {(r0 != null ? r0.bounds.size.ToString() : "SIN RENDERER")}.");
            }
        }

        /// <summary>
        /// Escala la pared (solo en X y Z, la altura no cambia) para que ocupe una celda y la centra en ella.
        /// </summary>
        private static void FitToCell(GameObject go, Vector3 cellCenter, float size, float y)
        {
            var r = go.GetComponentInChildren<Renderer>();
            if (r == null)
            {
                go.transform.position = new Vector3(cellCenter.x, y, cellCenter.z);
                return;
            }

            Vector3 b = r.bounds.size;
            Vector3 s = go.transform.localScale;
            if (b.x > 1e-4f && b.z > 1e-4f)
                go.transform.localScale = new Vector3(s.x * size / b.x, s.y, s.z * size / b.z);

            Vector3 c = r.bounds.center;
            Vector3 pos = go.transform.position;
            go.transform.position = new Vector3(pos.x + cellCenter.x - c.x, y, pos.z + cellCenter.z - c.z);
        }

        private bool[,] Generate(int w, int h, System.Random rng)
        {
            bool[,] wall = null;
            int segMax = Mathf.Max(minSegments, maxSegments);
            int lenMax = Mathf.Max(minLength, maxLength);

            for (int attempt = 0; attempt < 200; attempt++)
            {
                wall = new bool[w, h];
                int segments = rng.Next(minSegments, segMax + 1);

                for (int s = 0; s < segments; s++)
                {
                    int x = rng.Next(w);
                    int y = rng.Next(h);
                    int len = rng.Next(minLength, lenMax + 1);
                    bool horizontal = rng.Next(2) == 0;

                    for (int i = 0; i < len; i++)
                    {
                        int cx = horizontal ? x + i : x;
                        int cy = horizontal ? y : y + i;
                        if (cx < w && cy < h) wall[cx, cy] = true;
                    }
                }

                int free = FillIsolatedAreas(wall, w, h);
                if (free >= minFreeFraction * w * h) break;
            }
            return wall;
        }

        /// <summary>
        /// Convierte en muro todo lo que no pertenezca a la zona libre más grande, para que no
        /// haya zonas aisladas (el perseguidor no podría llegar). Devuelve las celdas libres finales.
        /// </summary>
        private static int FillIsolatedAreas(bool[,] wall, int w, int h)
        {
            var label = new int[w, h];
            var stack = new Stack<(int, int)>();
            int next = 0, best = 0, bestSize = 0;

            for (int sx = 0; sx < w; sx++)
            {
                for (int sy = 0; sy < h; sy++)
                {
                    if (wall[sx, sy] || label[sx, sy] != 0) continue;

                    next++;
                    int size = 0;
                    label[sx, sy] = next;
                    stack.Push((sx, sy));

                    while (stack.Count > 0)
                    {
                        var (x, y) = stack.Pop();
                        size++;
                        foreach (var (dx, dy) in Dirs)
                        {
                            int nx = x + dx, ny = y + dy;
                            if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
                            if (wall[nx, ny] || label[nx, ny] != 0) continue;
                            label[nx, ny] = next;
                            stack.Push((nx, ny));
                        }
                    }

                    if (size > bestSize) { bestSize = size; best = next; }
                }
            }

            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                    if (!wall[x, y] && label[x, y] != best) wall[x, y] = true;

            return bestSize;
        }
    }
}