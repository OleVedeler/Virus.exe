﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MapEditor.Classes;
using Microsoft.Win32;

namespace MapEditor.Handlers
{
	class GameGridHandler
	{
		private const int PixelWidth = 25;
		private const int PixelHeight = 25;
        private bool _showCollision;
        private readonly Grid _editorGrid;
		private readonly AssetDatabaseHandler _assetDatabaseHandler;
		private readonly TreeViewHandler _treeViewHandler;
        private readonly JsonHandler _jsonHandler;
        private readonly PropertyHandler _propertyHandler;

		public GameGridHandler(
							Grid editorGrid, 
							AssetDatabaseHandler assetDatabaseHandler, 
							TreeViewHandler treeViewHandler,
                            PropertyHandler propertyHandler)
		{
            _propertyHandler = propertyHandler;
			_editorGrid = editorGrid;
			_assetDatabaseHandler = assetDatabaseHandler;
			_treeViewHandler = treeViewHandler;
            _jsonHandler = new JsonHandler();
			InitGameGrid();
		}

		public int Size()
		{
			return Width() * Height();
		}

		public int Width()
		{
			return (int)(_editorGrid.Width/PixelWidth);
		}

		public int Height()
		{
			return (int) (_editorGrid.Height/PixelHeight);
		}

		/// <summary>
		/// adds the asset to the database, then updates the treeview
		/// </summary>
		/// <param name="imp"></param>
        public void Import(ImportObject imp)
        {
            _assetDatabaseHandler.Add(imp);
            _treeViewHandler.Update();
        }

		/// <summary>
		/// askes for path to save it to and saves as a json file
		/// </summary>
        public void Save()
        {
	        string json = _jsonHandler.JsonString();

            SaveFileDialog save = new SaveFileDialog();

            save.FileName = "Map"; // Default file name
            save.DefaultExt = ".json"; // Default file extension
            save.Filter = "JSON files (.json)|*.json"; // Filter files by extension

            // Show save file dialog box
            Nullable<bool> result = save.ShowDialog();

            // Process save file dialog box results
            if (result == true)
            {
                try
                {
                // Save document
                string filename = save.FileName;
                    File.WriteAllText(filename, json);
                }
                catch(Exception ex)
                {
                    Console.WriteLine("Error: Could not read to file. Original error: " + ex.Message);
                }
            }
        }

		/// <summary>
		/// Askes for a filepath, then loads the choosen .json file and drawes it to the map
		/// </summary>
        public void Load()
        {
            Stream stream = null;
            OpenFileDialog open = new OpenFileDialog();
            string jsonStr = "";

            open.Filter = "JSON files (.json)|*.json";
            open.FilterIndex = 2;

            Nullable<bool> result = open.ShowDialog();

            if (result == true)
            {
                try
                {
                    
                    if ((stream = open.OpenFile()) != null)
                    {
                        using (stream)
                        {
                            StreamReader reader = new StreamReader(stream);
                            jsonStr = reader.ReadToEnd();
                            reader.Close();
                        }
                    }

                    NewMap();
					_jsonHandler.Deserialize(jsonStr);
                    DrawToGridEvent();
            }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: Could not read file from disk. Original error: " + ex.Message);
                }

                
            }
        }


		/// <summary>
		/// initializes the editor grid again
		/// </summary>
        public void NewMap()
        {
            InitGameGrid();
        }

		/// <summary>
		/// sets low opacity on the grids that are obstacles
		/// </summary>
		/// <param name="show"></param>
        public void ShowCollisionmap(bool show) 
        {
            _showCollision = show;
            for (int i = 0; i < _jsonHandler.Tiles().Count; i++)
            {
                Grid currentGrid = (Grid)_editorGrid.Children[i];
                if (_showCollision)
                {
                    if ((_jsonHandler.Tiles()[i].isObstacle == 0) && (_jsonHandler.Tiles()[i].id != 0))
                        currentGrid.Background.Opacity = 0.3;
                }
                else
                {
                    if(_jsonHandler.Tiles()[i].id != 0)
                        currentGrid.Background.Opacity = 1;
                }
            }
        }

		/// <summary>
		/// clears the grid and the json file and draws everything black
		/// </summary>
		private void InitGameGrid()
		{
            _jsonHandler.Tiles().Clear();
            _editorGrid.Children.Clear();

			for (int column = 0; column < Height(); column++)
			{
				for (int row = 0; row < Width(); row++)
				{
					Grid childGrid = new Grid
					{
						Height = PixelHeight,
						Width = PixelWidth,
						HorizontalAlignment = HorizontalAlignment.Left,
						VerticalAlignment = VerticalAlignment.Top,
						Background = Brushes.Black,
						Margin = new Thickness(PixelHeight*row, PixelWidth*column, 0, 0)
					};

					childGrid.MouseEnter += DrawToGridEvent;
					childGrid.MouseLeftButtonDown += DrawToGridEvent;

					_editorGrid.Children.Add(childGrid);
                    _jsonHandler.Tiles().Add(new Tile());
				}
			}
		}

		/// <summary>
		/// finds the grid that was clicked and draws it with the choosen assets image
		/// </summary>
		/// <param name="o"></param>
		/// <param name="e"></param>
		private void DrawToGridEvent(Object o, MouseEventArgs e)
		{
			if ((_treeViewHandler.SelectedItem()) == null) return;
        
			string selectedName = ((TreeViewItem)_treeViewHandler.SelectedItem()).Header.ToString();

			var assetData = _assetDatabaseHandler.GetRowBy(selectedName);

			if(assetData == null) return;

			Grid currentGrid = (Grid)o;
			if (e.LeftButton == MouseButtonState.Pressed)
			{
				currentGrid.Background = new ImageBrush(_assetDatabaseHandler.DecodeImage(assetData.Image.ToArray()));
                if(_showCollision && !((bool)_propertyHandler.ObsticalCheckBox.IsChecked))
                {
                    currentGrid.Background.Opacity = 0.3;
                }
                AddToJsonList(currentGrid, assetData);
			}
		}

        /// <summary>
        ///  Draws from the json input instead of user input
        /// </summary>
        private void DrawToGridEvent()
        {
            var assetData = _assetDatabaseHandler.GetAllRows();
            
            for(int i = 0; i < _jsonHandler.Tiles().Count; i++) {
                if (assetData == null) return;

                Grid currentGrid = (Grid)_editorGrid.Children[i];

                using (var enumerator = assetData.GetEnumerator())
				while (enumerator.MoveNext()) {
                    if(_jsonHandler.Tiles()[i].id == enumerator.Current.Id) {
                        currentGrid.Background = new ImageBrush(_assetDatabaseHandler.DecodeImage(enumerator.Current.Image.ToArray()));
                    }
                }
            }
        }

		/// <summary>
		/// Adds the position in the editor grid the asset id will be stored
		/// adds if its a obstacle or not
		/// </summary>
		/// <param name="g"></param>
		/// <param name="assetData"></param>
        private void AddToJsonList(Grid g, Asset assetData)
        {
            //id
            _jsonHandler.Tiles()[_editorGrid.Children.IndexOf(g)].id = assetData.Id;

            //isObstacle
            if ((bool)_propertyHandler.ObsticalCheckBox.IsChecked)
            {
                _jsonHandler.Tiles()[_editorGrid.Children.IndexOf(g)].isObstacle = 1;
            }
            else
            {
                _jsonHandler.Tiles()[_editorGrid.Children.IndexOf(g)].isObstacle = 0;
            }
        }

	}
}
