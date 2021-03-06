﻿using UnityEngine;
using System.Collections;
using System.Drawing.Imaging;
using System.Drawing;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System;
using System.Diagnostics;
public class ProjectSaver : MonoBehaviour
{
    public MapRenderer Renderer;
    void Awake()
    {
        FindObjectOfType<MapLoader>().FinishedLoadingMap += () =>
        FindObjectOfType<MapModesAndControls>().RegisterCallback(SaveProject, "Save Project");
    }
    public Map Map;
    public void SaveProject()
    {
        Map = FindObjectOfType<MapLoader>().Map;
        UnityEngine.Debug.LogFormat("Map is {0}", Map);
        //Magick starts here
        Bitmap pixels = new Bitmap(Map.Width, Map.Height);
        int pId = 1;

        UnityEngine.Debug.LogFormat("Map.Provinces is {0}", Map.Provinces);
        foreach (var province in Map.Provinces)
        {
            province.ID = pId++;
            province.MapUniqueColor = province.SerializedColor();
        }
        UnityEngine.Debug.LogFormat("Map.Tiles is {0}", Map.Tiles);
        for ( int i = 0; i < Map.Tiles.GetLength(0);i++)
            for ( int j = 0; j < Map.Tiles.GetLength(1); j++)
            {
                var tile = Map.Tiles[i, j];
                pixels.SetPixel(tile.X, Map.Height - 1 - tile.Y, tile.Province.MapUniqueColor);
            }
        var dir = PlayerPrefs.GetString("directory");
        UnityEngine.Debug.LogFormat("dir is {0}", dir);
        pixels.Save(dir + "/map/provinces.png", ImageFormat.Png);
        var path = Application.dataPath + "/StreamingAssets/";
        Process process = new Process();
        process.StartInfo.FileName = path + "PngToBmpEncoder.exe";
        process.StartInfo.Arguments = String.Format("{0} {1} {2}", "\"" + dir + "\"", "map/provinces.png", "map/provinces.bmp");
        UnityEngine.Debug.Log(process.StartInfo.Arguments);
        process.StartInfo.CreateNoWindow = true;
        process.StartInfo.UseShellExecute = false;
        process.Start();
        process.Exited += (o,e) => File.Delete(dir + "/map/provinces.png");
        //Ends here



        var defStream = File.Create(dir + "/map/definition.csv");
        var adjStream = File.Create(dir + "/map/adjacencies.csv");
        
        var encoder = new UTF8Encoding();
        StringBuilder builder = new StringBuilder();
        var firstLineBytes = encoder.GetBytes("0;0;0;0;land;true;unknown;2" + Environment.NewLine);
        defStream.Write(firstLineBytes, 0, firstLineBytes.Length);
        int idOffset = 0;
        foreach(var province in Map.Provinces)
        {
            if (province.Tiles.Count == 0)
            {
                idOffset--;
                continue;
            }
            builder.Length = 0;
            province.ID += idOffset;
            builder.Append(province.ID).Append(';');
            builder.Append(province.MapUniqueColor.R).Append(';');
            builder.Append(province.MapUniqueColor.G).Append(';');
            builder.Append(province.MapUniqueColor.B).Append(';');
            builder.Append(province.Category).Append(';');
            builder.Append(province.SomeBool?"true":"false").Append(';');
            builder.Append(province.Type).Append(';');
            builder.Append(province.Continent).AppendLine();
            var lineBytes = encoder.GetBytes(builder.ToString());
            defStream.Write(lineBytes,0, lineBytes.Length);
            
        }
        defStream.Close();

        builder.Length = 0;
        builder.Append("From;To;Type;Through;start_x;start_y;stop_x;stop_y;adjacency_rule_name;Comment").AppendLine();
        var header = encoder.GetBytes(builder.ToString());
        adjStream.Write(header, 0, header.Length);
        HashSet<Adjacency> alreadySaved = new HashSet<Adjacency>();
        foreach ( var province in Map.Provinces)
        {
            foreach ( var adjacency in province.Adjacencies.Values)
            {
                if(alreadySaved.Add(adjacency))
                {
                    builder.Length = 0;
                    builder.Append(adjacency.From.ID).Append(';');
                    builder.Append(adjacency.To.ID).Append(';');
                    builder.Append(adjacency.Type == AdjacencyType.Sea ? "sea" : "").Append(';');
                    builder.Append(adjacency.Through == null? -1 : adjacency.Through.ID).Append(';');
                    builder.Append(adjacency.StartTile == null? -1 : adjacency.StartTile.X).Append(';').Append(adjacency.StartTile == null ? -1 : adjacency.StartTile.Y).Append(';');
                    builder.Append(adjacency.StopTile == null ? -1 : adjacency.StopTile.X).Append(';').Append(adjacency.StopTile == null ? -1 : adjacency.StopTile.Y).Append(';');
                    builder.Append(adjacency.AdjacencyRule == null ? "" : adjacency.AdjacencyRule).Append(';');
                    builder.Append(adjacency.Comment).AppendLine();
                    var lineBytes = encoder.GetBytes(builder.ToString());
                    adjStream.Write(lineBytes, 0, lineBytes.Length);
                }
            }
        }
        var lastLine = encoder.GetBytes("-1;-1;;-1;-1;-1;-1;-1;-1");
        adjStream.Write(lastLine, 0, lastLine.Length);
        adjStream.Close();

        int stateId = 1;
        foreach(var state in Map.States)
        {
            state.ID = stateId++;
            state.Name = "\"STATE_" + state.ID + "\"";
        }

        int regionID = 1;
        foreach (var region in Map.StrategicRegions)
        {
            region.ID = regionID++;
            region.Name = "\"REGION_" + region.ID + "\"";
        }

        int supplyID = 1;
        foreach (var area in Map.SupplyAreas)
        {
            area.ID = supplyID++;
            area.Name = "\"SUPPLYAREA_" + area.ID + "\"";
        }
        var curDir = Directory.GetCurrentDirectory();
        var states = new DirectoryInfo(dir + "/history/states");
        states.Empty();
        StringBuilder formatBuilder = new StringBuilder();
        idOffset = 0;
        foreach ( var state in Map.States)
        {
            Directory.SetCurrentDirectory(dir + "/history/states");
            if (state.Provinces.Count == 0)
            {
                idOffset--;
                continue;
            }
            state.ID += idOffset;
            formatBuilder.Length = 0;
            state.Format(formatBuilder);
            File.WriteAllText(state.ID.ToString() + "-State.txt", formatBuilder.ToString());
        }
        var regions = new DirectoryInfo(dir + "/map/strategicregions");
        regions.Empty();
        idOffset = 0;
        foreach ( var region in Map.StrategicRegions)
        {
            Directory.SetCurrentDirectory(dir + "/map/strategicregions");
            if (region.Provinces.Count == 0)
            {
                idOffset--;
                continue;
            }
            region.ID += idOffset;
            formatBuilder.Length = 0;
            region.Format(formatBuilder);
            File.WriteAllText(region.ID.ToString() + "-Region.txt", formatBuilder.ToString());
        }
        var areas = new DirectoryInfo(dir + "/map/supplyareas");
        areas.Empty();
        idOffset = 0;
        foreach ( var area in Map.SupplyAreas)
        {
            Directory.SetCurrentDirectory(dir + "/map/supplyareas");
            if(area.States.Count == 0)
            {
                idOffset--;
                continue;
            }
            area.ID += idOffset;
            formatBuilder.Length = 0;
            area.Format(formatBuilder);
            
            File.WriteAllText(area.ID.ToString() + "-SupplyArea.txt", formatBuilder.ToString());
        }
        Directory.SetCurrentDirectory(curDir);

        Map.World.SaveTo(dir);

        var buildings_raw = File.ReadAllLines(dir + "/map/BUILDINGS_GENERATION_TEMPLATE.txt");
        List<Building> buildings = new List<Building>();
        foreach(var line in buildings_raw)
        {
            Building b = new Building();
            var parts = line.Split(' ');
            b.Name = parts[0];
            b.Count = int.Parse(parts[1]);
            b.Coastal = parts.Length > 2 ? parts[2] == "coastal" : false;
            b.Port = parts.Length > 3 ? parts[3] == "port" : false;
            buildings.Add(b);
        }

        //var buildingsStream = File.Create(dir + "/map/buildings.txt");
        formatBuilder.Length = 0;
        System.Random random = new System.Random(0);
        foreach(var state in Map.States)
        {
            var coastLine = CoastLine(state);
            foreach(var building in buildings)
            {
                if (building.Coastal)
                    continue;
                for ( int i = 0; i < building.Count; i++)
                {
                    formatBuilder.Append(state.ID).Append(";").Append(building.Name).Append(";");
                    var province = state.Provinces[random.Next(state.Provinces.Count)];
                    int tileId = random.Next(province.Tiles.Count);
                    int tileIndex = 0;
                    Tile chosenTile = null;
                    foreach ( var tile in province.Tiles)
                    {
                        if (tileIndex == tileId)
                        {
                            chosenTile = tile;
                            break;
                        }
                        tileIndex++;
                    }
                    
                    int probablyX = chosenTile.X;
                    int Z = 0;
                    int probablyY = chosenTile.Y;
                    int probablyRotation = random.Next(-1, 5);
                    formatBuilder.
                        Append(probablyX).Append(".00;").Append(Z).Append(".00;").
                        Append(probablyY).Append(".00;").Append(probablyRotation).Append(".00;");
                    formatBuilder.Append("0");
                    formatBuilder.AppendLine();
                }
            }
            if(coastLine.Count > 0)
            foreach (var building in buildings)
            {
                if (!building.Coastal)
                    continue;
                for (int i = 0; i < building.Count; i++)
                {
                    formatBuilder.Append(state.ID).Append(";").Append(building.Name).Append(";");
                    Tile chosenTile = coastLine[random.Next(coastLine.Count)];
                    
                    int probablyX = chosenTile.X;
                    int probablyScale = random.Next(9, 15);
                    int probablyY = chosenTile.Y;
                    int probablyRotation = random.Next(-1, 5);
                    formatBuilder.
                        Append(probablyX).Append(".00;").Append(probablyScale).Append(".00;").
                        Append(probablyY).Append(".00;").Append(probablyRotation).Append(".00;");
                    if(building.Port)
                        formatBuilder.Append(chosenTile.Province.ID);
                    else
                        formatBuilder.Append("0");
                    formatBuilder.AppendLine();
                }
            }
        }
        File.WriteAllText(dir + "/map/buildings.txt", formatBuilder.ToString());
        //buildingsStream.Close();
        Renderer.FullRedraw();
    }

    List<Tile> coastTiles = new List<Tile>();
    List<Tile> CoastLine(State state)
    {
        coastTiles.Clear();
        foreach(var province in state.Provinces)
        {
            foreach(var tile in province.Tiles)
            {
                if(tile.X > 0)
                {
                    var nextTile = Map.Tiles[tile.X - 1, tile.Y];
                    if (nextTile.Province.Category == "sea")
                    {
                        coastTiles.Add(tile);
                        break;
                    }
                }
                if(tile.Y > 0)
                {

                    var nextTile = Map.Tiles[tile.X, tile.Y -1];
                    if (nextTile.Province.Category == "sea")
                    {
                        coastTiles.Add(tile);
                        break;
                    }
                }
                if(tile.X < Map.Width - 1)
                {

                    var nextTile = Map.Tiles[tile.X + 1, tile.Y];
                    if (nextTile.Province.Category == "sea")
                    {
                        coastTiles.Add(tile);
                        break;
                    }
                }
                if(tile.Y < Map.Height - 1)
                {

                    var nextTile = Map.Tiles[tile.X , tile.Y + 1];
                    if (nextTile.Province.Category == "sea")
                    {
                        coastTiles.Add(tile);
                        break;
                    }
                }
            }
        }

        return coastTiles;
    }
    
}

class Building
{
    public string Name;
    public int Count;
    public bool Coastal;
    public bool Port;
}

public static class DirExt
{
    public static void Empty(this System.IO.DirectoryInfo directory)
    {
        foreach (System.IO.FileInfo file in directory.GetFiles()) file.Delete();
        foreach (System.IO.DirectoryInfo subDirectory in directory.GetDirectories()) subDirectory.Delete(true);
    }
}