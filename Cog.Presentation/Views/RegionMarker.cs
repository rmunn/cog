﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using GMap.NET;
using GMap.NET.WindowsPresentation;
using SIL.Cog.Application.ViewModels;
using SIL.Cog.Presentation.Converters;

namespace SIL.Cog.Presentation.Views
{
	public class RegionMarker : GMapPolygon, IDisposable
	{
		public event EventHandler Click;

		private RegionHitMarker _regionHitMarker;
		private readonly List<RegionPointMarker> _regionPoints;
		private readonly List<RegionPointMarker> _regionMidpoints;
		private int _currentPointIndex;
		private bool _isMidpoint;
		private readonly GeographicalRegionViewModel _region;

		public RegionMarker(GeographicalRegionViewModel region)
			: base(Enumerable.Empty<PointLatLng>())
		{
			_region = region;
			Points.AddRange(region.Coordinates.Select(coord => new PointLatLng(coord.Item1, coord.Item2)));
			Position = Points[0];
			_regionPoints = new List<RegionPointMarker>();
			_regionMidpoints = new List<RegionPointMarker>();
		}

		public override void RegenerateShape(GMapControl map)
		{
			if (map != null)
			{
				if (Points.Count > 1)
				{
					var localPath = new List<Point>();
					var offset = map.FromLatLngToLocal(Points[0]);
					foreach (PointLatLng i in Points)
					{
						var p = map.FromLatLngToLocal(new PointLatLng(i.Lat, i.Lng));
						localPath.Add(new Point(p.X - offset.X, p.Y - offset.Y));
					}

					// Create a StreamGeometry to use to specify myPath.
					var geometry = new StreamGeometry();

					using (StreamGeometryContext ctx = geometry.Open())
					{
						ctx.BeginFigure(localPath[0], true, true);

						// Draw a line to the next specified point.
						ctx.PolyLineTo(localPath, true, true);
					}

					// Freeze the geometry (make it unmodifiable)
					// for additional performance benefits.
					geometry.Freeze();

					var fillBrush = new SolidColorBrush();
					BindingOperations.SetBinding(fillBrush, SolidColorBrush.ColorProperty, new Binding("Variety.ClusterIndex") {Converter = new IndexToColorConverter(), ConverterParameter = Colors.CornflowerBlue});
					var strokeBrush = new SolidColorBrush();
					BindingOperations.SetBinding(strokeBrush, SolidColorBrush.ColorProperty, new Binding("Color") {Source = fillBrush, Converter = new ColorBrightnessConverter(), ConverterParameter = -0.15});
					// Create a path to draw a geometry with.
					var path = new Path
						{
							Data = geometry,
							Effect = new BlurEffect {KernelType = KernelType.Gaussian, Radius = 3.0, RenderingBias = RenderingBias.Quality},
							Stroke = strokeBrush,
							Fill = fillBrush,
							StrokeThickness = 3,
							Opacity = 0.5,
							IsHitTestVisible = true,
							DataContext = _region,
							Visibility = Shape == null ? Visibility.Visible : Shape.Visibility
						};
					Shape = path;
					Shape.MouseEnter += Shape_MouseEnter;
					Shape.MouseLeave += Region_MouseLeave;
				}
				else
				{
					Shape = null;
				}
			}
		}

		public bool IsSelectable { get; set; }

		public GeographicalRegionViewModel Region
		{
			get { return _region; }
		}

		private void Shape_MouseEnter(object sender, MouseEventArgs e)
		{
			if (Map != null && IsSelectable)
			{
				var path = (Path) sender;
				var marker = (RegionMarker) Map.Markers.First(m => m.Shape == path);
				for (int i = 0; i < marker.Points.Count; i++)
				{
					PointLatLng curPoint = marker.Points[i];
					PointLatLng nextPoint = i == marker.Points.Count - 1 ? marker.Points[0] : marker.Points[i + 1];
					CreateMidpoint(i, curPoint, nextPoint);
					CreatePoint(i, curPoint);
				}

				_regionHitMarker = new RegionHitMarker(marker.Points);
				_regionHitMarker.RegenerateShape(Map);
				_regionHitMarker.Shape.MouseLeave += Region_MouseLeave;
				_regionHitMarker.Shape.MouseLeftButtonUp += RegionHit_MouseLeftButtonUp;
				Map.OnMapZoomChanged += Map_OnMapZoomChanged;
				Map.Markers.Add(_regionHitMarker);
			}
		}

		private void Map_OnMapZoomChanged()
		{
			CleanupSelection();
		}

		private void Region_MouseLeave(object sender, MouseEventArgs e)
		{
			if (CheckLeaving())
				CleanupSelection();
		}

		private bool CheckLeaving()
		{
			return _regionHitMarker != null && !_regionHitMarker.Shape.IsMouseOver
			       && _regionPoints.All(pm => !pm.Shape.IsMouseOver) && _regionMidpoints.All(pm => !pm.Shape.IsMouseOver);
		}

		public void CleanupSelection()
		{
			foreach (RegionPointMarker pm in _regionPoints)
				pm.Dispose();
			_regionPoints.Clear();
			foreach (RegionPointMarker pm in _regionMidpoints)
				pm.Dispose();
			_regionMidpoints.Clear();

			if (_regionHitMarker != null)
			{
				_regionHitMarker.Dispose();
				_regionHitMarker = null;
			}
			Map.OnMapZoomChanged -= Map_OnMapZoomChanged;
		}

		private void CreateMidpoint(int index, PointLatLng p1, PointLatLng p2)
		{
			GPoint lp1 = Map.FromLatLngToLocal(p1);
			GPoint lp2 = Map.FromLatLngToLocal(p2);
			var midpointMarker = new RegionPointMarker(Map.FromLocalToLatLng((int) (lp2.X + lp1.X) / 2, (int) (lp2.Y + lp1.Y) / 2)) {IsMidpoint = true};
			midpointMarker.Shape.MouseLeftButtonDown += RegionPoint_MouseLeftButtonDown;
			midpointMarker.Shape.MouseLeave += Region_MouseLeave;
			_regionMidpoints.Insert(index, midpointMarker);
			Map.Markers.Add(midpointMarker);
		}

		private void CreatePoint(int index, PointLatLng p)
		{
			var pointMarker = new RegionPointMarker(p);
			pointMarker.Shape.MouseLeftButtonDown += RegionPoint_MouseLeftButtonDown;
			pointMarker.Shape.MouseLeave += Region_MouseLeave;
			_regionPoints.Insert(index, pointMarker);
			Map.Markers.Add(pointMarker);
		}

		private void RegionHit_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			if (IsSelectable)
			{
				EventHandler handler = Click;
				if (handler != null)
					handler(this, new EventArgs());
			}
		}

		private void RegionPoint_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			_isMidpoint = false;
			_currentPointIndex = _regionPoints.FindIndex(rm => rm.Shape == sender);
			if (_currentPointIndex == -1)
			{
				_currentPointIndex = _regionMidpoints.FindIndex(rm => rm.Shape == sender);
				_isMidpoint = true;
			}
			var pointShape = (UIElement) sender;
			pointShape.CaptureMouse();
			pointShape.MouseMove += RegionPoint_MouseMove;
			pointShape.MouseLeftButtonUp += RegionPoint_MouseLeftButtonUp;
			e.Handled = true;
		}

		private void RegionPoint_MouseMove(object sender, MouseEventArgs e)
		{
			Point p = e.GetPosition(Map);
			PointLatLng pll = Map.FromLocalToLatLng((int) p.X, (int) p.Y);
			if (_isMidpoint)
			{
				Points.Insert(_currentPointIndex + 1, pll);
				_regionHitMarker.Points.Insert(_currentPointIndex + 1, pll);
				_regionPoints.Insert(_currentPointIndex + 1, _regionMidpoints[_currentPointIndex]);
				_regionMidpoints[_currentPointIndex].IsMidpoint = false;
				_regionMidpoints.RemoveAt(_currentPointIndex);
				_currentPointIndex++;
				_isMidpoint = false;
			}
			else
			{
				if (_currentPointIndex == 0)
					Position = pll;
				Points[_currentPointIndex] = pll;
				if (_regionMidpoints.Count == _regionPoints.Count)
				{
					for (int i = _currentPointIndex - 1; i <= _currentPointIndex; i++)
					{
						int curIndex = i;
						if (curIndex == -1)
							curIndex = _regionPoints.Count - 1;
						GPoint curPoint = Map.FromLatLngToLocal(_regionPoints[curIndex].Position);
						GPoint nextPoint = Map.FromLatLngToLocal(i == _regionPoints.Count - 1 ? _regionPoints[0].Position : _regionPoints[i + 1].Position);
						_regionMidpoints[curIndex].Position = Map.FromLocalToLatLng((int) (nextPoint.X + curPoint.X) / 2, (int) (nextPoint.Y + curPoint.Y) / 2);
					}
				}
			}
			_regionPoints[_currentPointIndex].Position = pll;
			RegenerateShape(Map);
			_regionHitMarker.Points[_currentPointIndex] = pll;
			_regionHitMarker.RegenerateShape(Map);
			_regionHitMarker.Shape.MouseLeave += Region_MouseLeave;
			_regionHitMarker.Shape.MouseLeftButtonUp += RegionHit_MouseLeftButtonUp;
		}

		private void RegionPoint_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			if (_regionMidpoints.Count < _regionPoints.Count)
			{
				for (int i = _currentPointIndex - 1; i <= _currentPointIndex; i++)
				{
					PointLatLng curPoint = _regionPoints[i].Position;
					PointLatLng nextPoint = i == _regionPoints.Count - 1 ? _regionPoints[0].Position : _regionPoints[i + 1].Position;
					CreateMidpoint(i, curPoint, nextPoint);
				}
				_region.Coordinates.Insert(_currentPointIndex, Tuple.Create(Points[_currentPointIndex].Lat, Points[_currentPointIndex].Lng));
			}
			else if (Math.Abs(_region.Coordinates[_currentPointIndex].Item1 - Points[_currentPointIndex].Lat) > double.Epsilon
				|| Math.Abs(_region.Coordinates[_currentPointIndex].Item2 - Points[_currentPointIndex].Lng) > double.Epsilon)
			{
				_region.Coordinates[_currentPointIndex] = Tuple.Create(Points[_currentPointIndex].Lat, Points[_currentPointIndex].Lng);
			}
			else
			{
				EventHandler handler = Click;
				if (handler != null)
					handler(this, new EventArgs());
			}
			var pointShape = (UIElement) sender;
			pointShape.MouseMove -= RegionPoint_MouseMove;
			pointShape.MouseLeftButtonUp -= RegionPoint_MouseLeftButtonUp;
			pointShape.ReleaseMouseCapture();
		}

		public void Dispose()
		{
			CleanupSelection();

			Shape.MouseEnter -= Shape_MouseEnter;
			Clear();
			Map.Markers.Remove(this);
		}
	}
}
