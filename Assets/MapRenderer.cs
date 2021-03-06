﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Text;
using System.IO;
public class MapRenderer : MonoBehaviour
{
    public MapLoader MapLoader;
    public LoaderProgressUI ProgressBar;
    public Material mapMaterial;
    int materialProvinceProp;
    int materialStateProp;
    int materialRegionProp;
    int materialSupplyProp;
    public Texture2D Overlay;
    public Image TargetOverlayImage;
    int stateOverlayProp;
    bool showBorders = true;
    public enum RenderMode { Normal, ProvinceType, StateType, Owner, ProvinceCategory, BordersCount, Chunks, IllegalCrossings }
    Dictionary<string, Color32> typeColor = new Dictionary<string, Color32>();
    void Awake()
    {
        MapLoader.FinishedLoadingMap += () =>
        {
            map = MapLoader.Map; LoadColors(); FullRedraw();
            mapMaterial.SetFloat("_ShowOnlySupply", 0f);
            mapMaterial.SetFloat("_ShowOnlyRegions", 0f);
        };
        materialProvinceProp = Shader.PropertyToID("_SelectedProvinceColor");
        materialStateProp = Shader.PropertyToID("_SelectedStateColor");
        materialRegionProp = Shader.PropertyToID("_SelectedRegionColor");
        materialSupplyProp = Shader.PropertyToID("_SelectedSupplyColor");
        stateOverlayProp = Shader.PropertyToID("_StateOverlay");
    }

    void LoadColors()
    {

        var pathToColors = MapLoader.ChosenProjectDir + "/map/MAP_EDITOR_TYPE_COLORS.txt";
        var lines = File.ReadAllLines(pathToColors);
        foreach (var line in lines)
        {
            var part = line.Split(' ');
            Color32 c = new Color32(byte.Parse(part[1]), byte.Parse(part[2]), byte.Parse(part[3]), 255);
            typeColor.Add(part[0], c);
        }
        foreach (var country in map.World.CountriesByTag.Values)
        {
            typeColor.Add(country.Tag, country.CultureColor);
            //Debug.Log(country.Tag + " " + country.CultureColor);
        }
        var pathToOverlay = MapLoader.ChosenProjectDir + "/map/OVERLAY.png";
        if (File.Exists(pathToOverlay))
        {
            var bytes = File.ReadAllBytes(pathToOverlay);
            Overlay = new Texture2D(2, 2);
            Overlay.LoadImage(bytes);
            TargetOverlayImage.sprite = Sprite.Create(Overlay, Rect.MinMaxRect(0, 0, Overlay.width, Overlay.height), Vector2.one / 2);
            var rectTransform = TargetOverlayImage.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(Overlay.width, Overlay.height);
            rectTransform.position = rectTransform.sizeDelta / 2 - new Vector2(chunkSize / 2, chunkSize / 2);
            TargetOverlayImage.color = new Color(1, 1, 1, alpha);
        }
    }
    Vector2 scrollPos = Vector2.zero;
    bool showKeyBindings = true;
    bool showOverlay = true;
    private void OnGUI()
    {
        if (showHelp == false)
            return;
        Rect rect = Rect.MinMaxRect(0, 0, 100, 400);
        scrollPos = GUI.BeginScrollView(rect, scrollPos, Rect.MinMaxRect(0, 0, 100, typeColor.Count * 30));
        var color = GUI.color;
        int index = 0;
        foreach (var colorPair in typeColor)
        {
            GUI.color = colorPair.Value;
            GUI.Label(Rect.MinMaxRect(0, index * 30, 100, index * 30 + 30), colorPair.Key);
            index++;
        }
        GUI.EndScrollView();

        GUI.color = color;
        if (showKeyBindings)
        {
            index = 0;
            GUI.Label(Rect.MinMaxRect(200, index * 30, 500, index * 30 + 30), showBorders ? "Shift + B => hide borders" : "Shift+B => show borders");
            index++;
            GUI.Label(Rect.MinMaxRect(200, index * 30, 500, index * 30 + 30), "Shift + 0 => Normal render mode");
            index++;
            GUI.Label(Rect.MinMaxRect(200, index * 30, 500, index * 30 + 30), "Shift + 9 => State type render mode");
            index++;
            GUI.Label(Rect.MinMaxRect(200, index * 30, 500, index * 30 + 30), "Shift + 8 => Province type render mode");
            index++;
            GUI.Label(Rect.MinMaxRect(200, index * 30, 500, index * 30 + 30), "Shift + 7 => Countries render mode");
            index++;
            GUI.Label(Rect.MinMaxRect(200, index * 30, 500, index * 30 + 30), "Shift + 6 => Province category render mode");
            index++;
            GUI.Label(Rect.MinMaxRect(200, index * 30, 500, index * 30 + 30), "Shift + 5 => Bordering render mode");
            index++;
            GUI.Label(Rect.MinMaxRect(200, index * 30, 500, index * 30 + 30), "Shift + 4 => Chunks render mode");
            index++;
            GUI.Label(Rect.MinMaxRect(200, index * 30, 500, index * 30 + 30), "Shift + 3 => Illegal crossings render mode");
            index++;
            GUI.Label(Rect.MinMaxRect(200, index * 30, 500, index * 30 + 30), "Shift + K => hide key bindings");
            index++;
            GUI.Label(Rect.MinMaxRect(200, index * 30, 500, index * 30 + 30), "Shift + R => change border mode to " + (borderShowType == BorderShowType.All ? "Only Strategic Regions" : borderShowType == BorderShowType.OnlyRegions ? "Only Supply Areas" : "Both regions and areas"));
            index++;
            GUI.Label(Rect.MinMaxRect(200, index * 30, 500, index * 30 + 30), "Shift + O => " + (showOverlay ? "Hide overlay" : "Show overlay"));

        }
        else
        {
            index = 0;
            GUI.Label(Rect.MinMaxRect(200, index * 30, 500, index * 30 + 30), "Shift + K => show key bindings");
        }

    }
    public RenderMode mode = RenderMode.Normal;
    public void ChangeMode(RenderMode mode)
    {
        Debug.Log("Changing mode to " + mode);
        this.mode = mode;
        if (mode != RenderMode.Normal)
            LitUpProvince(null);
        FullRedraw();
    }
    bool showHelp = false;
    enum BorderShowType { All, OnlySupply, OnlyRegions }
    BorderShowType borderShowType = BorderShowType.All;
    float alpha = 0.25f;
    void Update()
    {
        if (Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyUp(KeyCode.UpArrow))
        {
            alpha = Mathf.Clamp01(alpha + 0.1f);
            if (showOverlay)
                TargetOverlayImage.color = new Color(1, 1, 1, alpha);
        }
        if (Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyUp(KeyCode.DownArrow))
        {
            alpha = Mathf.Clamp01(alpha - 0.1f);
            if (showOverlay)
                TargetOverlayImage.color = new Color(1, 1, 1, alpha);
        }
        if (Input.GetKey(KeyCode.LeftShift))
        {

            if (Input.GetKeyUp(KeyCode.H))
                showHelp = !showHelp;
            if (Input.GetKeyUp(KeyCode.Alpha0))
                ChangeMode(RenderMode.Normal);
            if (Input.GetKeyUp(KeyCode.Alpha9))
                ChangeMode(RenderMode.StateType);
            if (Input.GetKeyUp(KeyCode.Alpha8))
                ChangeMode(RenderMode.ProvinceType);
            if (Input.GetKeyUp(KeyCode.Alpha7))
                ChangeMode(RenderMode.Owner);
            if (Input.GetKeyUp(KeyCode.Alpha6))
                ChangeMode(RenderMode.ProvinceCategory);
            if (Input.GetKeyUp(KeyCode.Alpha5))
                ChangeMode(RenderMode.BordersCount);
            if (Input.GetKeyUp(KeyCode.Alpha4))
                ChangeMode(RenderMode.Chunks);
            if (Input.GetKeyUp(KeyCode.Alpha3))
                ChangeMode(RenderMode.IllegalCrossings);
            if (Input.GetKeyUp(KeyCode.O))
            {
                showOverlay = !showOverlay;
                if (showOverlay)
                    TargetOverlayImage.color = new Color(1, 1, 1, alpha);
                else
                    TargetOverlayImage.color = Color.clear;
            }
            if (Input.GetKeyUp(KeyCode.B))
            {
                showBorders = !showBorders;
                ChangeMode(mode);
            }
            if (Input.GetKeyUp(KeyCode.K))
            {
                showKeyBindings = !showKeyBindings;
            }
            if (Input.GetKeyUp(KeyCode.R))
            {
                if (borderShowType == BorderShowType.All)
                    borderShowType = BorderShowType.OnlyRegions;
                else if (borderShowType == BorderShowType.OnlyRegions)
                    borderShowType = BorderShowType.OnlySupply;
                else
                    borderShowType = BorderShowType.All;

                switch (borderShowType)
                {
                    case BorderShowType.All:
                        mapMaterial.SetFloat("_ShowOnlySupply", 0f);
                        mapMaterial.SetFloat("_ShowOnlyRegions", 0f);
                        break;
                    case BorderShowType.OnlyRegions:
                        mapMaterial.SetFloat("_ShowOnlySupply", 0f);
                        mapMaterial.SetFloat("_ShowOnlyRegions", 1f);
                        break;
                    case BorderShowType.OnlySupply:
                        mapMaterial.SetFloat("_ShowOnlySupply", 1f);
                        mapMaterial.SetFloat("_ShowOnlyRegions", 0f);
                        break;
                }

            }
        }
        if (map == null)
        {
            if (MapLoader.Map != null)
                lock (MapLoader.Map)
                {
                    if (MapLoader.Map.Width > 0 && MapLoader.Map.Height > 0)
                    {
                        map = MapLoader.Map;
                        StartCoroutine(CreateChunks());
                    }
                }
        }
        else if (ready)
        {
            foreach (var chunk in forUpdate)
                chunk.Apply();
            forUpdate.Clear();
        }
    }
    public GameObject ChunkProto;
    Map map;
    bool chunksReady = false;
    bool ready = false;
    Texture2D[,] chunks;
    List<GameObject> chunkGOs = new List<GameObject>();
    HashSet<Texture2D> forUpdate = new HashSet<Texture2D>();
    public int chunkSize = 512;
    IEnumerator CurrentCoroutine;

    IEnumerator CreateChunks()
    {
        chunksReady = false;
        foreach (var chunkGO in chunkGOs)
            Destroy(chunkGO);
        chunkGOs.Clear();
        int chunksWidth = (map.Width / chunkSize + (map.Width % chunkSize == 0 ? 0 : 1));
        int chunksHeight = (map.Height / chunkSize + (map.Height % chunkSize == 0 ? 0 : 1));
        chunks = new Texture2D[chunksWidth, chunksHeight];
        var time = Time.realtimeSinceStartup;
        for (int i = 0; i < chunksWidth; i++)
            for (int j = 0; j < chunksHeight; j++)
            {
                var chunkTex = new Texture2D(chunkSize, chunkSize);
                chunkTex.filterMode = FilterMode.Point;
                chunkTex.anisoLevel = 0;
                chunkTex.mipMapBias = 0f;
                chunks[i, j] = chunkTex;
                GameObject chunkGo = GameObject.Instantiate(ChunkProto);
                var chunkData = chunkGo.GetComponent<ChunkMapController>();
                chunkData.MapOffsetX = i * chunkSize;
                chunkData.MapOffsetY = j * chunkSize;
                chunkData.Size = chunkSize;
                var image = chunkGo.GetComponent<Image>();
                image.sprite = Sprite.Create(chunkTex, Rect.MinMaxRect(0, 0, chunkSize, chunkSize), Vector2.zero);
                chunkGo.transform.SetParent(transform, false);
                chunkGOs.Add(chunkGo);
                if (Time.realtimeSinceStartup - time > 12f)
                    yield return null;
            }
        chunksReady = true;
        Camera.main.transform.position = new Vector3(map.Width / 2, map.Height / 2, -10);
    }

    IEnumerator RedrawCoroutine()
    {

        ProgressBar.Text = "Creating chunks. Please wait...";
        ProgressBar.MaxProgress = chunks.GetLength(0) * chunks.GetLength(1);
        ProgressBar.Progress = 0;
        while (!chunksReady)
        {
            ProgressBar.Progress = chunkGOs.Count;
            yield return null;
        }
        ProgressBar.Text = "Writing provinces pixel data. Please wait...";
        ProgressBar.MaxProgress = map.Provinces.Count;
        ProgressBar.Progress = 0;
        foreach (var province in map.Provinces)
        {
            foreach (var tile in province.Tiles)
            {
                Update(tile.X, tile.Y);
            }
            ProgressBar.Progress++;
        }
        yield return null;
        ProgressBar.Text = "Creating textures. Please wait...";
        ProgressBar.MaxProgress = forUpdate.Count;
        ProgressBar.Progress = 0;
        foreach (var texToUpdate in forUpdate)
        {
            texToUpdate.Apply();
            ProgressBar.Progress++;
            yield return null;
        }
        ProgressBar.MaxProgress = 0;
        forUpdate.Clear();
        ready = true;
    }
    public void FullRedraw()
    {
        ready = false;
        if (CurrentCoroutine != null)
            StopCoroutine(CurrentCoroutine);
        StartCoroutine(CurrentCoroutine = RedrawCoroutine());
    }

    public void Update(Tile tile)
    {
        Update(tile.X, tile.Y);
    }

    public void Update(State state)
    {
        for (int i = 0; i < state.Provinces.Count; i++)
            Update(state.Provinces[i]);
    }
    public void Update(Province province)
    {
        foreach (var tile in province.Tiles)
        {
            Update(tile);
            if (tile.BorderCount > 0)
            {
                if (tile.X > 0)
                    Update(tile.X - 1, tile.Y);
                if (tile.X < map.Width - 1)
                    Update(tile.X + 1, tile.Y);
                if (tile.Y > 0)
                    Update(tile.X, tile.Y - 1);
                if (tile.Y < map.Height - 1)
                    Update(tile.X, tile.Y + 1);
            }
        }
    }
    public void Update(int x, int y)
    {
        if (y < 0 && y >= map.Height)
            return;
        if (x < 0 && x >= map.Width)
            return;
        int chunkOffsetX;
        int chunkOffsetY;
        var chunkTex = GetChunk(x, y, out chunkOffsetX, out chunkOffsetY);

        Color color = GetColorForTile(map.Tiles[x, y]);

        chunkTex.SetPixel(chunkOffsetX, chunkOffsetY, color);

        forUpdate.Add(chunkTex);

    }
    Dictionary<Chunk, Color32> colorsPerChunk = new Dictionary<Chunk, Color32>();
    Color32 GetColorForTile(Tile tile)
    {
        Color32 color = Color.clear;
        if (mode == RenderMode.Normal)
        {
            if (tile.BorderCount > 0 && showBorders)
            {
                if (tile.Province.StrategicRegion != null && tile.Province.State != null && tile.Province.State.Supply != null)
                {

                    tile.Province.StrategicRegion.TextureColor(ref color);
                    tile.Province.State.Supply.TextureColor(ref color);
                }
                else if (tile.Province.State != null && tile.Province.State.Supply == null)
                {
                    color = Color.yellow;
                }
                else if (tile.Province.StrategicRegion == null)
                {
                    color = Color.gray;
                }
                else if (tile.Province.StrategicRegion != null && tile.Province.State == null)
                {
                    tile.Province.StrategicRegion.TextureColor(ref color);
                }
            }
            else
            {
                tile.Province.TextureColor(ref color);
                if (tile.Province.State != null)
                    tile.Province.State.TextureColor(ref color);
                else
                    color.a = 255;
            }
        }
        else if (mode == RenderMode.IllegalCrossings)
        {
            if (tile.BorderCount > 0)
            {
                if (tile.X > 0 && tile.Y > 0)
                {
                    var leftTile = map.Tiles[tile.X - 1, tile.Y];
                    var topTile = map.Tiles[tile.X, tile.Y - 1];
                    var topLeftTile = map.Tiles[tile.X - 1, tile.Y - 1];
                    if (tile.Province != leftTile.Province && tile.Province != topTile.Province && tile.Province != topLeftTile.Province &&
                       leftTile.Province != topLeftTile.Province && topTile.Province != topLeftTile.Province && leftTile.Province != topTile.Province)
                    {
                        color = Color.red;
                        return color;
                    }
                }
            }
            byte value = (byte)(tile.Province.ID % 255 / 2);
            color = new Color32(value, value, value, 255);
        }
        else if (mode == RenderMode.Chunks)
        {
            if (!colorsPerChunk.TryGetValue(tile.Chunk, out color))
            {
                color = Random.ColorHSV();
                color.a = 255;
                colorsPerChunk.Add(tile.Chunk, color);
            }
        }
        else if (mode == RenderMode.BordersCount)
        {
            if (tile.BorderCount == 0)
                color = Color.black;
            else if (tile.BorderCount == 1)
                color = new Color32(20, 20, 20, 255);
            else if (tile.BorderCount == 2)
                color = new Color32(60, 60, 60, 255);
            else if (tile.BorderCount == 3)
                color = new Color32(140, 140, 140, 255);
            else
                color = (Color32)Color.red;
        }
        else
        {
            if (tile.BorderCount > 0 && showBorders)
                color = Color.black;
            else
            {
                if (mode == RenderMode.ProvinceType)
                    typeColor.TryGetValue(tile.Province.Type, out color);
                else if (mode == RenderMode.Owner)
                {

                    if (tile.Province.State != null && tile.Province.State.Owner != null)
                        typeColor.TryGetValue(tile.Province.State.Owner.Tag, out color);
                }
                else if (mode == RenderMode.ProvinceCategory)
                {
                    typeColor.TryGetValue(tile.Province.Category, out color);
                }
                else if (tile.Province.State != null)
                    typeColor.TryGetValue(tile.Province.State.StateCategory, out color);
            }
        }
        return color;
    }
    Texture2D GetChunk(int x, int y, out int chunkOffsetX, out int chunkOffsetY)
    {
        int chunkX = x / chunkSize;
        chunkOffsetX = x - chunkX * chunkSize;
        int chunkY = y / chunkSize;
        chunkOffsetY = y - chunkY * chunkSize;
        return chunks[chunkX, chunkY];
    }

    StringBuilder builder = new StringBuilder();
    public void LitUpProvince(Province province)
    {
        if (mode != RenderMode.Normal)
            return;

        builder.Length = 0;
        if (province != null)
        {
            builder.Append("Province = ").Append(province.ID).Append(" ");
            Color32 color = Color.clear;
            province.TextureColor(ref color);
            mapMaterial.SetColor(materialProvinceProp, color);
            if (province.State != null)
            {

                builder.Append("State = ").Append(province.State.ID).Append(" ");
                province.State.TextureColor(ref color);
                mapMaterial.SetColor(materialStateProp, color);
                mapMaterial.SetFloat(stateOverlayProp, 1f);
                if (province.State.Supply != null)
                {
                    builder.Append("Supply Area = ").Append(province.State.Supply.ID).Append(" ");
                    province.State.Supply.TextureColor(ref color);
                    mapMaterial.SetColor(materialSupplyProp, color);
                }
                if (province.StrategicRegion != null)
                {

                    builder.Append("Strategic Region = ").Append(province.StrategicRegion.ID).Append(" ");
                    province.StrategicRegion.TextureColor(ref color);
                    mapMaterial.SetColor(materialRegionProp, color);
                }
                if (province.State.Supply == null && province.StrategicRegion == null)
                {
                    color = Color.white;
                    mapMaterial.SetColor(materialSupplyProp, color);
                    mapMaterial.SetColor(materialRegionProp, color);
                }
            }
            else
            {
                if (province.StrategicRegion != null)
                {

                    builder.Append("Strategic Region = ").Append(province.StrategicRegion.ID).Append(" ");
                    province.StrategicRegion.TextureColor(ref color);
                    mapMaterial.SetColor(materialRegionProp, color);
                }
                else
                {
                    color = Color.clear;
                    mapMaterial.SetColor(materialStateProp, color);
                    mapMaterial.SetFloat(stateOverlayProp, 0f);
                }
            }



        }
        else
        {

            Color32 color = Color.clear;
            mapMaterial.SetColor(materialProvinceProp, color);
            mapMaterial.SetColor(materialStateProp, color);
            color = Color.white;
            mapMaterial.SetColor(materialSupplyProp, color);
            mapMaterial.SetColor(materialRegionProp, color);
            mapMaterial.SetFloat(stateOverlayProp, 0f);
        }
        Debug.Log(builder.ToString());
    }

}
