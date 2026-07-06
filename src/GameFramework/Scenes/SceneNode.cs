using System.Drawing;
using System.Runtime.CompilerServices;

namespace GameFramework.Scenes;

public sealed class SceneNode {
	internal SceneNode(
		int width,
		int height
	) {
		Clip = new Rectangle( 0, 0, width, height );
		Width = width;
		Height = height;
	}

	internal SceneNode(
		SceneNode parent,
		int left,
		int top,
		int width,
		int height,
		ISceneNodeHandler handler
	) {
		Rectangle r = new Rectangle( parent.Clip.X + left, parent.Clip.Y + top, width, height );
		Clip = Rectangle.Intersect( parent.Clip, r );
		Left = left;
		Top = top;
		Width = Clip.Width;
		Height = Clip.Height;
		Handler = handler;
		parent.Children.Add( this );
	}

	public void AddChild(
		int left,
		int top,
		int width,
		int height,
		ISceneNodeHandler handler
	) {
		SceneNode child = new SceneNode( this, left, top, width, height, handler );

		Children.Add( child );
	}

	public int Left { get; }
	public int Top { get; }
	public int Width { get; }
	public int Height { get; }
	public Rectangle Clip { get; }
#pragma warning disable CA1002
	public List<SceneNode> Children { get; } = [];
#pragma warning restore CA1002


	public ISceneNodeHandler? Handler { get; set; }

	[MethodImpl( MethodImplOptions.AggressiveInlining )]
	public bool MouseUp(
		Coordinate coordinate,
		int button,
		bool isHandled
	) =>
		  Handler?.OnMouseUp( this, coordinate, button, isHandled ) ?? false;

	[MethodImpl( MethodImplOptions.AggressiveInlining )]
	public void MousePressed(
		int button
	) =>
		Handler?.OnMousePressed( this, button );

	[MethodImpl( MethodImplOptions.AggressiveInlining )]
	public bool MouseDown(
		Coordinate coordinate,
		int button,
		bool isHandled
	) =>
		  Handler?.OnMouseDown( this, coordinate, button, isHandled ) ?? false;

	[MethodImpl( MethodImplOptions.AggressiveInlining )]
	public void MouseReleased(
		int button
	) =>
		Handler?.OnMouseReleased( this, button );

	[MethodImpl( MethodImplOptions.AggressiveInlining )]
	public void MouseMoved(
		Coordinate coordinate
	) =>
		Handler?.OnMouseMoved( this, coordinate );

	[MethodImpl( MethodImplOptions.AggressiveInlining )]
	public bool MouseMove(
		Coordinate coordinate,
		bool isHandled
	) =>
		  Handler?.OnMouseMove( this, coordinate, isHandled ) ?? false;

	[MethodImpl( MethodImplOptions.AggressiveInlining )]
	public void MouseEntered(
	) =>
		Handler?.OnMouseEntered( this );

	[MethodImpl( MethodImplOptions.AggressiveInlining )]
	public void MouseExited(
	) =>
		Handler?.OnMouseExited( this );
}
